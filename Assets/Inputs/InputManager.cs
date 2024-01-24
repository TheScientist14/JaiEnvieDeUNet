using System;
using UnityEngine;


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
}