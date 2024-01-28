using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : NetworkBehaviour
{
	public sbyte MaxHealth = 100;

	private NetworkVariable<sbyte> m_Health = new NetworkVariable<sbyte>();

	public UnityEvent OnDeath;

	public UnityEvent<int> OnDamaged;

	public UnityEvent<int> OnHealed;

	private void Awake()
	{
		if(OnDeath == null)
			OnDeath = new UnityEvent();
		if(OnDamaged == null)
			OnDamaged = new UnityEvent<int>();
        if (OnHealed == null)
            OnHealed = new UnityEvent<int>();

        m_Health.OnValueChanged += _CheckForDeath;
	}

	private void _CheckForDeath(sbyte iPrevVal, sbyte iCurVal)
	{
		if (iPrevVal > 0 && iCurVal <= 0)
		{
			if (IsServer)
			{
				PVEGameMode.Instance().AddToEnemyDeathCounter();
			}
			OnDeath.Invoke();
		}
	}

	// Start is called before the first frame update
	void Start()
	{
		if (IsServer)
		{
			m_Health.Value = MaxHealth;
		}
	}

	public void TakeDamage(sbyte iDamage)
	{
		if(!IsServer)
		{
			TakeDamageServerRPC(iDamage);
			return;
		}

		if(iDamage <= 0 || m_Health.Value <= 0)
			return;

		m_Health.Value -= (sbyte)Mathf.Min(iDamage, m_Health.Value);
		OnDamaged.Invoke(iDamage);
	}

	public void Heal(sbyte iHeal)
	{
		if(!IsServer) 
		{
			HealServerRPC(iHeal);
			return;
		}

		if (iHeal <= 0 || m_Health.Value >= MaxHealth)
		{
			return;
		}

		m_Health.Value = (sbyte)Mathf.Min((int)m_Health.Value + (int) iHeal, MaxHealth);
		OnHealed.Invoke(iHeal);


    }

	[ServerRpc]
	private void TakeDamageServerRPC(sbyte iDamage)
	{
		TakeDamage(iDamage);
	}

	[ServerRpc]
	private void HealServerRPC(sbyte iHeal)
	{
		Heal(iHeal);
	}


	public sbyte GetHealth()
	{
		return m_Health.Value;
	}

	public void OnValueChanged(NetworkVariable<sbyte>.OnValueChangedDelegate iListener)
	{
		m_Health.OnValueChanged += iListener;
	}
}
