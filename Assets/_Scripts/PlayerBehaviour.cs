using Cinemachine;
using UnityEngine;
using Unity.Netcode;
using Unity.XR.OpenVR;


[RequireComponent(typeof(Rigidbody))]
public class PlayerBehaviour : NetworkBehaviour
{
	[SerializeField] private Vector2 camSens = new(100, 100);
	[SerializeField] private float playerSpeed = 2.0f;
	[SerializeField] private float jumpHeight = 1.0f;

	private Rigidbody _rb;
	private InputManager _inputManager;
	private Transform _camTransform;
	private CinemachineVirtualCamera _virtualCamera;

	private bool _hasJumped = false;
	private Collider _ground = null;

	private float _backupDrag;

	private void Start()
	{
		_rb = GetComponent<Rigidbody>();
		_inputManager = InputManager.instance;
		_camTransform = GetComponentInChildren<Camera>().transform;
		_virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
		
		if (!IsOwner)
		{
			_camTransform.gameObject.SetActive(false);
			_virtualCamera.gameObject.SetActive(false);
			Destroy(this);
			return;
		}
		

		_virtualCamera.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.m_MaxSpeed = camSens.x * 0.01f;
		_virtualCamera.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.m_MaxSpeed = camSens.y * 0.01f;
	}

	void Update()
	{
		if(IsGrounded() && _inputManager.PlayerJumped())
			_hasJumped = true;
	}

	void FixedUpdate()
	{
		if(!IsGrounded())
		{
			_backupDrag = _rb.drag;
			_rb.drag = 0;
			return;
		}
		_rb.drag = _backupDrag;

		Vector3 forward = _camTransform.forward;
		forward.y = 0;
		Vector3 move = forward.normalized * _inputManager.GetPlayerMovement().y + _camTransform.right * _inputManager.GetPlayerMovement().x;
		_rb.AddForce(move * playerSpeed, ForceMode.Acceleration);

		if(_hasJumped)
		{
			_hasJumped = false;
			_rb.AddForce(Vector3.up * jumpHeight, ForceMode.Acceleration);
		}
	}

	private bool IsGrounded()
	{
		return _ground != null;
	}

	void OnCollisionEnter(Collision iCollision)
	{
		if(Vector3.Dot(iCollision.GetContact(0).normal, Vector3.up) > 0.8f)
			_ground = iCollision.collider;
	}

	void OnCollisionExit(Collision iCollision)
	{
		if(iCollision.collider == _ground)
			_ground = null;
	}
}