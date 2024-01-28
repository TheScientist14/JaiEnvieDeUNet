using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class CaptureTheFlagGamemode : PVPGameMode
{
	protected NetworkList<int> m_TeamPoints;

	[SerializeField] private int m_PointObjective = 100;

	public new static CaptureTheFlagGamemode Instance()
	{
		return instance as CaptureTheFlagGamemode;
	}

	public override void Awake()
	{
		base.Awake();

		m_TeamPoints = new NetworkList<int>();
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		if(IsServer)
		{
			m_TeamPoints.Add(0);
			m_TeamPoints.Add(0);
		}

		Debug.Log("CaptureTheFlag spawned");
	}

	protected override void OnAllPlayersConnected()
	{
		base.OnAllPlayersConnected();
		_DispatchPlayers(2);
	}

	public int GetTeamPoints(int iTeamIdx)
	{
		if(iTeamIdx >= m_TeamPoints.Count || iTeamIdx < 0)
			return 0;

		return m_TeamPoints[iTeamIdx];
	}

	public void AddTeamPoints(int iTeamIdx, int iNbPoints = 1)
	{
		m_TeamPoints[iTeamIdx] += iNbPoints;
		if(m_TeamPoints[iTeamIdx] >= m_PointObjective)
			OnVictory.Invoke(iTeamIdx);
	}
}
