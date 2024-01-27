using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ZoneBehaviour : NetworkBehaviour
{
	// negative values means neutral zone
	protected NetworkVariable<int> m_CurTeam = new NetworkVariable<int>();
	protected NetworkVariable<bool> m_DoOwnZone = new NetworkVariable<bool>();

	// server only
	// map of team index to player count
	protected Dictionary<int, int> m_PlayersInZone = new Dictionary<int, int>();

	[SerializeField] private float m_CaptureDuration = 2;
	[SerializeField] private float m_PointFrequency = 1;
	[SerializeField] private int m_NbGainedPoints = 1;

	public void Awake()
	{
		m_CurTeam.OnValueChanged += _TryTakeControl;
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		if(IsServer)
		{
			m_CurTeam.Value = -1;

			StartCoroutine(_GainPoints());
		}
	}

	IEnumerator _GainPoints()
	{
		var delay = new WaitForSeconds(m_PointFrequency);
		CaptureTheFlagGamemode gamemode = CaptureTheFlagGamemode.Instance();
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

		PlayerBehaviour player = iCollider.gameObject.GetComponent<PlayerBehaviour>();
		if(player == null)
			return;

		int playerTeam = PVPGameMode.Instance().GetPlayerTeam(player.OwnerClientId);

		int nbTeamPlayerInZone = m_PlayersInZone.GetValueOrDefault(playerTeam, 0);
		m_PlayersInZone[playerTeam] = nbTeamPlayerInZone + 1;

		_UpdateTeam();
	}

	private void OnTriggerExit(Collider iCollider)
	{
		if(!IsServer)
			return;

		PlayerBehaviour player = iCollider.gameObject.GetComponent<PlayerBehaviour>();
		if(player == null)
			return;

		int playerTeam = PVPGameMode.Instance().GetPlayerTeam(player.OwnerClientId);

		int nbTeamPlayerInZone = m_PlayersInZone.GetValueOrDefault(playerTeam, 0);

		if(nbTeamPlayerInZone <= 1)
			m_PlayersInZone.Remove(playerTeam);
		else
			m_PlayersInZone[playerTeam] = nbTeamPlayerInZone + 1;

		_UpdateTeam();
	}

	private void _UpdateTeam()
	{
		if(m_PlayersInZone.Count == 1)
			m_CurTeam.Value = m_PlayersInZone.Keys.GetEnumerator().Current;
		else
			m_CurTeam.Value = -1;
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
