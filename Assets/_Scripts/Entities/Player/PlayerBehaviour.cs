using Cinemachine;
using UnityEngine;
using Unity.Netcode;
using Unity.XR.OpenVR;
using System.Collections.Generic;
using Unity.FPS.Game;


[RequireComponent(typeof(Rigidbody))]
public class PlayerBehaviour : NetworkBehaviour
{
	[SerializeField] private Vector2 camSens = new(100, 100);
	[SerializeField] private float playerSpeed = 2.0f;
	[SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private float airControl = 0;

    [Header("Weapons")]
	[SerializeField] private List<WeaponController> weapons = new List<WeaponController>();
	[SerializeField] private Transform weaponSocket;
	[SerializeField] private Transform aimingWeaponSocket;
	[SerializeField] private float aimingAnimationSpeed;
	private WeaponController[] weaponSlots = new WeaponController[4];
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



	private void Start()
	{
		_rb = GetComponent<Rigidbody>();
		_inputManager = InputManager.instance;
		_camTransform = GetComponentInChildren<Camera>().transform;
		_virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();

		DefaultFoV = _virtualCamera.m_Lens.FieldOfView;
		defaultWeaponPosition = weaponSocket.localPosition;
		if(!IsOwner)
		{
			_camTransform.gameObject.SetActive(false);
			_virtualCamera.gameObject.SetActive(false);
			Destroy(this);
			return;
		}

		_virtualCamera.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.m_MaxSpeed = camSens.x * 0.01f;
		_virtualCamera.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.m_MaxSpeed = camSens.y * 0.01f;

		foreach(var weapon in weapons) 
		{
			AddWeapon(weapon);
		}
		
		SwitchWeapon(true);
	}

	private void Update()
	{
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
			!_inputManager.PlayerCancelFire());

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
				WeaponController weaponInstance = Instantiate(weaponPrefab, weaponSocket);
				weaponInstance.transform.localPosition = Vector3.zero;
				weaponInstance.transform.localRotation = Quaternion.identity;

				weaponInstance.Owner = gameObject;
				weaponInstance.SourcePrefab = weaponPrefab.gameObject;
				weaponInstance.ShowWeapon(false);
				weaponInstance.GetComponent<NetworkObject>().Spawn();

				weaponSlots[i] = weaponInstance;

				return true;
			}
		}
        if (weaponSlots[activeWeaponIndex] == null)
        {
            SwitchWeapon(true);
        }

        return false;
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
}