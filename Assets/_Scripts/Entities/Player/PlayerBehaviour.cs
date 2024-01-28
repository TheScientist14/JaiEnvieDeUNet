using Cinemachine;
using UnityEngine;
using Unity.Netcode;
using Unity.XR.OpenVR;
using System.Collections.Generic;
using Unity.FPS.Game;
using NaughtyAttributes;
using System.Collections;


[RequireComponent(typeof(Rigidbody))]
public class PlayerBehaviour : NetworkBehaviour
{
	[SerializeField] private Vector2 camSens = new(100, 100);
	[SerializeField] private float playerSpeed = 2.0f;
	[SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private float airControl = 0;
	[SerializeField] private float timeToRevive = 5.0f;
	private float reviveTimeLeft = 0.0f;
	[SerializeField] private float penaltyMultiplierOutsideReviveBox = 0.5f;
	[SerializeField] CapsuleCollider normalCapsuleCollider;
	[SerializeField] BoxCollider reviveBoxCollider;

    [Header("Weapons")]
	[SerializeField] private List<WeaponController> weapons = new List<WeaponController>();
	[SerializeField] private Transform weaponSocket;
	[SerializeField] private Transform aimingWeaponSocket;
	[SerializeField] private float aimingAnimationSpeed;
	[SerializeField] private WeaponController[] weaponSlots;
	private int activeWeaponIndex = 0;
	private bool isAiming = false;
	private Vector3 weaponMainLocalPosition;
	private Vector3 defaultWeaponPosition;
	private float DefaultFoV;

	private Rigidbody _rb;
	private InputManager _inputManager;
	private Transform _camTransform;
	private CinemachineVirtualCamera _virtualCamera;

	private bool _hasJumped = false;
	private Collider _ground = null;

	private HealthComponent _health;
    private bool _isDead = false;

    public bool IsDead { get => _isDead; set => _isDead = value; }

    private void Start()
	{
		_health = GetComponent<HealthComponent>();

		_health.OnDeath.AddListener(OnDie);
		_health.OnDamaged.AddListener(OnDamaged);

		_rb = GetComponent<Rigidbody>();
		_inputManager = InputManager.instance;
		_camTransform = GetComponentInChildren<Camera>().transform;
		_virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
		var canvas = GetComponentInChildren<Canvas>();

		DefaultFoV = _virtualCamera.m_Lens.FieldOfView;
		defaultWeaponPosition = weaponSocket.localPosition;
		if(!IsOwner && !IsServer)
		{
			_camTransform.gameObject.SetActive(false);
			_virtualCamera.gameObject.SetActive(false);
			canvas.gameObject.SetActive(false);
			
			Destroy(this);
			return;
			
		}
        UnlockCamera();

        //InitWeaponsServerRPC();

        foreach (var weaponController in weaponSlots)
		{
			weaponController.Owner = gameObject;
			weaponController.ShowWeapon(false);
		}
		
		weaponSlots[activeWeaponIndex].ShowWeapon(true);
		
		Debug.Log("PLayerBehaviour Done Start ");
		//SwitchWeapon(true);
	}

	[ServerRpc(RequireOwnership = true)]
	private void InitWeaponsServerRPC()
	{
		foreach(var weapon in weapons) 
		{
			AddWeapon(weapon);
		}
	}

	private void Update()
	{
        if (_isDead)
        {
            return;
        }
		if(IsGrounded() && _inputManager.PlayerJumped())
			_hasJumped = true;

		//Shooting
		WeaponController activeWeapon = weaponSlots[activeWeaponIndex];

		if (activeWeapon == null)
			return;

		if (activeWeapon.IsReloading)
			return;

		if (!activeWeapon.AutomaticReload && _inputManager.PlayerReload() && activeWeapon.CurrentAmmoRatio < 1.0f)
		{
			isAiming = false;
			activeWeapon.StartReloadAnimation();
			return;
		}
		isAiming = _inputManager.PlayerHoldAimDownSight();

		activeWeapon.HandleShootInputs(
			_inputManager.PlayerFired(),
			_inputManager.PlayerHoldDownFire(),
			_inputManager.PlayerCancelFire());

		if (!isAiming && !activeWeapon.IsCharging)
		{
			float switchWeaponInput = Mathf.Clamp(_inputManager.PlayerSwitchWeapon(), -1.0f, 1.0f);
			if (switchWeaponInput != 0.0f) 
			{
				SwitchWeapon(switchWeaponInput < 0.0f);
			}
		}
	}

	private void FixedUpdate()
	{
		if (_isDead)
		{
			return;
		}
		Vector3 forward = _camTransform.forward;
		forward.y = 0;
		Vector3 move = forward.normalized * _inputManager.GetPlayerMovement().y + _camTransform.right * _inputManager.GetPlayerMovement().x;

		if(!IsGrounded())
			move *= airControl;

		_rb.AddForce(move * playerSpeed, ForceMode.Acceleration);

		if(_hasJumped)
		{
			_hasJumped = false;
			_rb.AddForce(Vector3.up * jumpHeight, ForceMode.Acceleration);
		}
	}

    private void LateUpdate()
    {
        UpdateWeaponAiming();

		weaponSocket.localPosition = weaponMainLocalPosition;
    }

    private bool AddWeapon(WeaponController weaponPrefab)
	{
		if (HasWeapon(weaponPrefab) != null)
		{
			return false;
		}

		for (int i = 0; i < weaponSlots.Length; i++)
		{
			if (weaponSlots[i] == null)
			{
				AddWeaponSpawn(weaponPrefab, i);
				return true;
			}
		}
        if (weaponSlots[activeWeaponIndex] == null)
        {
            SwitchWeapon(true);
        }

        return false;
    }
    
	private void AddWeaponSpawn(WeaponController weaponPrefab, int i)
	{
        WeaponController weaponInstance = Instantiate(weaponPrefab, weaponSocket);
        weaponInstance.transform.localPosition = Vector3.zero;
        weaponInstance.transform.localRotation = Quaternion.identity;

        weaponInstance.Owner = gameObject;
        weaponInstance.SourcePrefab = weaponPrefab.gameObject;
        weaponInstance.ShowWeapon(false);
        weaponInstance.GetComponent<NetworkObject>().Spawn();

        weaponSlots[i] = weaponInstance;
    }

	private WeaponController HasWeapon(WeaponController weaponPrefab)
	{
		for (int i = 0; i < weaponSlots.Length; i++)
		{
			var weapon = weaponSlots[i];
			if (weapon != null && weapon.SourcePrefab == weaponPrefab.gameObject)
			{
				return weapon;
			}
		}
		return null;
	}

	private void SwitchWeapon(bool ascendingOrder)
	{
		int newWeaponIndex = -1;
		int closestSlotDistance = weaponSlots.Length;
		for (int i = 0; i < weaponSlots.Length; i++)
		{
			if (i != activeWeaponIndex && weaponSlots[i] != null)
			{

				int distanceToActiveIndex = 0;
				if (ascendingOrder)
				{
                    distanceToActiveIndex = i - activeWeaponIndex;
				}
				else
				{
					distanceToActiveIndex = -1 * (i - activeWeaponIndex);
				}
				if (distanceToActiveIndex < 0)
				{
					distanceToActiveIndex = weaponSlots.Length + distanceToActiveIndex;
				}

				if (distanceToActiveIndex < closestSlotDistance)
				{
					closestSlotDistance = distanceToActiveIndex;
					newWeaponIndex = i;
				}
			}
		}
		SwitchToWeaponIndex(newWeaponIndex);
	}

	private void SwitchToWeaponIndex(int newWeaponIndex)
	{
		if (newWeaponIndex != activeWeaponIndex && newWeaponIndex >= 0)
		{
			weaponMainLocalPosition = weaponSocket.localPosition;
			weaponSlots[activeWeaponIndex].ShowWeapon(false);
			weaponSlots[newWeaponIndex].ShowWeapon(true);
			activeWeaponIndex = newWeaponIndex;
		}
	}

	private void UpdateWeaponAiming()
	{
		WeaponController activeWeapon = weaponSlots[activeWeaponIndex];
        if (isAiming && activeWeapon)
        {
			weaponMainLocalPosition = Vector3.Lerp(
				weaponMainLocalPosition,
				aimingWeaponSocket.localPosition + activeWeapon.AimOffset,
				aimingAnimationSpeed * Time.deltaTime);

			_virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(
				_virtualCamera.m_Lens.FieldOfView, 
				activeWeapon.AimZoomRatio * DefaultFoV,
				aimingAnimationSpeed * Time.deltaTime);
		}
		else
		{
			weaponMainLocalPosition = Vector3.Lerp(
				weaponMainLocalPosition,
				defaultWeaponPosition,
				aimingAnimationSpeed * Time.deltaTime);
			_virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(
				_virtualCamera.m_Lens.FieldOfView,
				DefaultFoV,
				aimingAnimationSpeed * Time.deltaTime);
		}
    }

	private bool IsGrounded()
	{
		return _ground != null;
	}

	private void OnCollisionEnter(Collision iCollision)
	{
		if(Vector3.Dot(iCollision.GetContact(0).normal, Vector3.up) > 0.8f)
			_ground = iCollision.collider;
	}

	private void OnCollisionExit(Collision iCollision)
	{
		if(iCollision.collider == _ground)
			_ground = null;
	}

	private void OnDamaged(int damage)
	{
		//TODO do smthg?
	}

	private void OnDie()
	{
		_isDead = true;
		LockCamera();
		normalCapsuleCollider.enabled = false;
		reviveBoxCollider.enabled = true;
		transform.Rotate(transform.right, 90.0f);
    }

    private void OnTriggerEnter(Collider other)
    {
		if (IsDead && other.TryGetComponent(out PlayerBehaviour playerBehaviour))
		{
			StopCoroutine(LeaveReviveBox());
			StartCoroutine(Revive());
		}
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsDead && other.TryGetComponent(out PlayerBehaviour playerBehaviour))
        {
            StopCoroutine(Revive());
            StartCoroutine(LeaveReviveBox());
        }
    }

    private IEnumerator Revive()
	{
		while (reviveTimeLeft < timeToRevive)
		{
			reviveTimeLeft += Time.deltaTime;
			yield return null;
		}
		IsDead = false;
		transform.rotation = Quaternion.identity;
        normalCapsuleCollider.enabled = true;
        reviveBoxCollider.enabled = false;
        UnlockCamera();
		
	}

	private IEnumerator LeaveReviveBox()
	{
		while(reviveTimeLeft > 0.0f) 
		{
			reviveTimeLeft -= Time.deltaTime * penaltyMultiplierOutsideReviveBox;
			yield return null;
		}
		reviveTimeLeft = 0.0f;
	}

    private void LockCamera()
	{
        _virtualCamera.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.m_MaxSpeed = 0.0f;
        _virtualCamera.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.m_MaxSpeed = 0.0f;
    }

    private void UnlockCamera()
	{
        _virtualCamera.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.m_MaxSpeed = camSens.x * 0.01f;
        _virtualCamera.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.m_MaxSpeed = camSens.y * 0.01f;

    }
}