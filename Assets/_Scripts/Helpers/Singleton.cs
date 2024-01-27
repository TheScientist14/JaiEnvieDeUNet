using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour
	where T : MonoBehaviour
{
	protected virtual void Awake()
	{
		if(_instance != null && _instance.gameObject != gameObject)
		{
			Destroy(gameObject);
			return;
		}

		_instance = GetComponent<T>();
		DontDestroyOnLoad(_instance);
	}

	public static T instance
	{
		get
		{
			if(_instance == null)
				_instance = new GameObject(typeof(T).Name + " " + nameof(Singleton<T>)).AddComponent<T>();

			return _instance;
		}
		private set
		{
			_instance = value;
		}
	}
	private static T _instance;

	protected virtual void OnDestroy()
	{
		if(_instance.gameObject == gameObject)
			_instance = null;
	}
}
