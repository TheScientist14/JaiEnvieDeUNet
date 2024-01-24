using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;


public class InputManager : Singleton<InputManager>
{
	private Inputs _inputs;

	protected override void Awake()
	{
		base.Awake();

		_inputs = new Inputs();
		Cursor.visible = false;
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

	public bool PlayerAimDownSight()
	{
		return _inputs.Player.Zoom.triggered;
	}

	public bool PlayerReload()
	{
		return _inputs.Player.Reload.triggered;
	}

	public float PlayerSwitchWeapon()
	{
		return _inputs.Player.SwitchWeapon.ReadValue<float>();
	}
}