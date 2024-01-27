using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonGameMode : NetworkSingleton<CommonGameMode>
{
	protected List<string> m_Players = new List<string>();

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		NetworkInit.s_PayloadAllocation.MatchProperties.Players.ConvertAll(p => p.CustomData.GetAs<Dictionary<string, string>>().GetValueOrDefault("Name", "UnknownPlayer"));
	}

	public List<string> GetPlayers()
	{
		return m_Players;
	}
}
