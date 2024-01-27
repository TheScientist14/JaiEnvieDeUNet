using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkSingleton<T> : NetworkBehaviour
	where T : NetworkBehaviour
{
	private static T _instance;
	public static T instance
	{
		get
		{
			return _instance;
		}
		private set
		{
			_instance = value;
		}
	}

	public override void OnNetworkSpawn()
	{
		if(_instance != null && _instance.gameObject != gameObject)
		{
			Debug.LogWarning("A new " + typeof(T).Name + " has been spawned when one was already active");
			Destroy(gameObject);
		}
		else
		{
			_instance = GetComponent<T>();
			DontDestroyOnLoad(_instance);
		}

		base.OnNetworkSpawn();
	}

	public override void OnNetworkDespawn()
	{
		if(_instance.gameObject == gameObject)
			_instance = null;

		base.OnNetworkDespawn();
	}
}
