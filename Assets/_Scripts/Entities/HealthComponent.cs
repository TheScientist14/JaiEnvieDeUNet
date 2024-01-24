using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : NetworkBehaviour
{
	private NetworkVariable<sbyte> m_Health;

	public UnityEvent OnDeath;

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
		if(iDamage <= 0 || m_Health.Value <= 0)
			return;

		m_Health.Value -= (sbyte)Mathf.Min(iDamage, m_Health.Value);

		if(m_Health.Value <= 0)
			OnDeath.Invoke();
	}

	public sbyte GetHealth()
	{
		return m_Health.Value;
	}
}
