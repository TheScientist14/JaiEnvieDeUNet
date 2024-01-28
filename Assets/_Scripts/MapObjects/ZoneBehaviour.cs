using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ZoneBehaviour : NetworkBehaviour
{
	//------------------------------------------------------------------------
	// Basically, everything is done server side here.
	// NetworkVariables are used so that clients are aware of the state
	// of the zone, to add some VFX, SFX, etc...
	//------------------------------------------------------------------------

	// negative values means neutral zone
	protected NetworkVariable<int> m_CurTeam = new NetworkVariable<int>();
	protected NetworkVariable<bool> m_DoOwnZone = new NetworkVariable<bool>();

	// server only
	// map of team index to player count
	protected Dictionary<int, int> m_PlayersInZone = new Dictionary<int, int>();
	protected HashSet<HealthComponent> m_PlayerHealthComponentsInZone = new HashSet<HealthComponent>();

	[SerializeField] private float m_CaptureDuration = 2;
	[SerializeField] private float m_PointFrequency = 1;
	[SerializeField] private int m_NbGainedPoints = 1;

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		if(IsServer)
		{
			m_CurTeam.OnValueChanged += _TryTakeControl;
			m_CurTeam.Value = -1;

			StartCoroutine(_GainPoints());
		}
	}

	IEnumerator _GainPoints()
	{
		var delay = new WaitForSeconds(m_PointFrequency);
		CaptureTheFlagGamemode gamemode = null;
		do
		{
			gamemode = CaptureTheFlagGamemode.Instance();
			yield return new WaitForEndOfFrame();
		}
		while(gamemode == null);

		while(true)
		{
			if(m_CurTeam.Value >= 0 && m_DoOwnZone.Value)
				gamemode.AddTeamPoints(m_CurTeam.Value, m_NbGainedPoints * m_PlayersInZone.GetValueOrDefault(m_CurTeam.Value, 0));

			yield return delay;
		}
	}

	private void OnTriggerEnter(Collider iCollider)
	{
		if(!IsServer)
			return;

		// tracking only players
		PlayerBehaviour player = iCollider.gameObject.GetComponent<PlayerBehaviour>();
		if(player == null)
			return;

		HealthComponent health = player.GetComponent<HealthComponent>();
		if(health == null)
			return;

		m_PlayerHealthComponentsInZone.Add(health);
		health.OnDeath.AddListener(_RemoveDeadPlayers);

		int playerTeam = PVPGameMode.Instance().GetPlayerTeam(health.OwnerClientId);
		_IncrPlayerbNbForTeam(playerTeam);
	}

	private void OnTriggerExit(Collider iCollider)
	{
		if(!IsServer)
			return;

		// tracking only players
		PlayerBehaviour player = iCollider.gameObject.GetComponent<PlayerBehaviour>();
		if(player == null)
			return;

		HealthComponent health = player.GetComponent<HealthComponent>();
		if(health == null)
			return;

		m_PlayerHealthComponentsInZone.Remove(health);
		health.OnDeath.RemoveListener(_RemoveDeadPlayers);

		int playerTeam = PVPGameMode.Instance().GetPlayerTeam(health.OwnerClientId);
		_DecrPlayerNbForTeam(playerTeam);
	}

	private void _IncrPlayerbNbForTeam(int iTeamIdx)
	{
		if(iTeamIdx < 0)
			return;

		int nbTeamPlayerInZone = m_PlayersInZone.GetValueOrDefault(iTeamIdx, 0);
		m_PlayersInZone[iTeamIdx] = nbTeamPlayerInZone + 1;

		_UpdateTeam();
	}

	private void _DecrPlayerNbForTeam(int iTeamIdx)
	{
		if(iTeamIdx < 0)
			return;

		int nbTeamPlayerInZone = m_PlayersInZone.GetValueOrDefault(iTeamIdx, 0);

		if(nbTeamPlayerInZone <= 1)
			m_PlayersInZone.Remove(iTeamIdx);
		else
			m_PlayersInZone[iTeamIdx] = nbTeamPlayerInZone + 1;

		_UpdateTeam();
	}

	private void _UpdateTeam()
	{
		if(m_PlayersInZone.Count == 1)
			m_CurTeam.Value = m_PlayersInZone.Keys.GetEnumerator().Current;
		else
			m_CurTeam.Value = -1;
	}

	private void _RemoveDeadPlayers()
	{
		m_PlayerHealthComponentsInZone.RemoveWhere(health =>
			{
				if(health.GetHealth() > 0)
					return false;

				health.OnDeath.RemoveListener(_RemoveDeadPlayers);

				int playerTeam = PVPGameMode.Instance().GetPlayerTeam(health.OwnerClientId);
				_DecrPlayerNbForTeam(playerTeam);

				return true;
			}
		);
	}

	private void _TryTakeControl(int iPrevValue, int iCurVal)
	{
		if(iPrevValue == iCurVal)
			return;

		StopCoroutine(_TakeControl());
		m_DoOwnZone.Value = false;
		if(iCurVal >= 0)
			StartCoroutine(_TakeControl());
	}

	private IEnumerator _TakeControl()
	{
		yield return new WaitForSeconds(m_CaptureDuration);
		m_DoOwnZone.Value = true;
	}
}
