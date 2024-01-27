using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CaptureTheFlagGamemode : PVPGameMode
{
	protected NetworkList<int> m_TeamPoints = new NetworkList<int>();

	public new static CaptureTheFlagGamemode Instance()
	{
		return instance as CaptureTheFlagGamemode;
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		m_TeamPoints.Add(0);
		m_TeamPoints.Add(0);
	}

	protected override void OnAllPlayersConnected()
	{
		_DispatchPlayers(2);
	}

	public int GetTeamPoints(int iTeamIdx)
	{
		return m_TeamPoints[iTeamIdx];
	}

	public void AddTeamPoints(int iTeamIdx, int iNbPoints = 1)
	{
		m_TeamPoints[iTeamIdx] += iNbPoints;
	}
}
