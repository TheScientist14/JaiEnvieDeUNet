using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : NetworkBehaviour
{
	NetworkVariable<sbyte> m_Health;
	UnityEvent OnDeath;

	private void Awake()
	{
		if(OnDeath == null)
			OnDeath = new UnityEvent();
	}

	// Start is called before the first frame update
	void Start()
	{
		m_Health.Value = 100;
	}

	public void TakeDamage(sbyte iDamage)
	{
		m_Health.Value -= iDamage;
	}
}
