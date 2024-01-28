using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;


public class InputManager : Singleton<InputManager>
{
	private Inputs _inputs;
	private bool _isHoldingShoot = false;
	private bool _isHoldingAimDownSight = false;

	public bool IsHoldingShoot
	{
		get => _isHoldingShoot; set => _isHoldingShoot = value;
	}
	public bool IsHoldingAimDownSight
	{
		get => _isHoldingAimDownSight; set => _isHoldingAimDownSight = value;
	}

	protected override void Awake()
	{
		base.Awake();

		_inputs = new Inputs();
		Cursor.visible = false;
	}

	private void Start()
	{
		_inputs.Player.Fire.performed += (InputAction) =>
		{
			_isHoldingShoot = true;
		};

		_inputs.Player.Fire.canceled += (InputAction) =>
		{
			_isHoldingShoot = false;
		};

		_inputs.Player.Zoom.performed += (InputAction) =>
		{
			_isHoldingAimDownSight = true;
		};

		_inputs.Player.Zoom.canceled += (InputAction) =>
		{
			_isHoldingAimDownSight = false;
		};
	}

	private void OnEnable()
	{
		_inputs.Enable();
	}

	private void OnDisable()
	{
		_inputs.Disable();
	}

	public Vector2 GetPlayerMovement()
	{
		return _inputs.Player.Move.ReadValue<Vector2>();
	}

	public bool PlayerJumped()
	{
		return _inputs.Player.Jump.triggered;
	}

	public bool PlayerUsed()
	{
		return _inputs.Player.Use.triggered;
	}

	public bool PlayerFired()
	{
		return _inputs.Player.Fire.triggered;
	}

	public bool PlayerHoldDownFire()
	{
		return _inputs.Player.Fire.IsInProgress();
	}

	public bool PlayerCancelFire()
	{
		return _inputs.Player.Fire.WasReleasedThisFrame();
	}

	public bool PlayerHoldAimDownSight()
	{
		return _inputs.Player.Zoom.IsInProgress();
	}

	public bool PlayerCancelAimDownSight()
	{
		return _inputs.Player.Zoom.WasReleasedThisFrame();
	}

	public bool PlayerReload()
	{
		return _inputs.Player.Reload.triggered;
	}

	public float PlayerSwitchWeapon()
	{
		return _inputs.Player.SwitchWeapon.ReadValue<float>();
	}

	public bool PlayerToggleShowScore()
	{
		return _inputs.Player.ShowScore.triggered;
	}
}