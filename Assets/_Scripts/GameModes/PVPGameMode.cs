using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

static class ShuffleExtension
{
	public static void Shuffle<T>(this IList<T> list)
		where T : unmanaged
	{
		int n = list.Count;
		while(n > 1)
		{
			n--;
			int k = Random.Range(0, n + 1);
			T value = list[k];
			list[k] = list[n];
			list[n] = value;
		}
	}
}

public class PVPGameMode : CommonGameMode
{
	protected NetworkList<ulong> m_ShuffledPlayerIds;
	protected NetworkVariable<int> m_NbTeam;

	protected List<List<ulong>> m_Teams = new List<List<ulong>>();
	protected Dictionary<ulong, int> m_PlayerToTeam = new Dictionary<ulong, int>();

	public UnityEvent<int> OnVictory;

	public static PVPGameMode Instance()
	{
		return instance as PVPGameMode;
	}

	public override void Awake()
	{
		base.Awake();

		if(OnVictory == null)
			OnVictory = new UnityEvent<int>();
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		m_NbTeam.OnValueChanged += _DispatchPlayersAction;
	}

	// Server only
	protected virtual void _DispatchPlayers(int iNbTeams = 2)
	{
		if(!IsServer)
			return;

		List<ulong> playersIds = GetPlayerIds();
		playersIds.Shuffle();

		m_NbTeam.Value = iNbTeams;
	}

	protected void _DispatchPlayersAction<T>(T _, T __)
	{
		Debug.Log("Shuffled player indices: " + m_ShuffledPlayerIds);

		m_Teams.Clear();
		m_PlayerToTeam.Clear();
		for(int i = 0; i < m_NbTeam.Value; i++)
			m_Teams.Add(new List<ulong>());

		int teamIdx = 0;
		foreach(ulong playerIdx in m_ShuffledPlayerIds)
		{
			m_Teams[teamIdx].Add(playerIdx);
			m_PlayerToTeam[playerIdx] = teamIdx;

			teamIdx++;
			if(teamIdx >= m_NbTeam.Value)
				teamIdx = 0;
		}
	}

	public List<List<ulong>> GetTeams()
	{
		return m_Teams;
	}

	public int GetPlayerTeam(ulong iPlayerId)
	{
		return m_PlayerToTeam.GetValueOrDefault(iPlayerId, -1);
	}
}
