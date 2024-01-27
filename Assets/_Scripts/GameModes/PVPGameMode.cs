using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

static class ShuffleExtension
{
	public static void Shuffle<T>(this INativeList<T> list)
		where T : unmanaged
	{
		int n = list.Length;
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
	protected NetworkVariable<NativeList<int>> m_ShuffledPlayerIndices = new NetworkVariable<NativeList<int>>();
	protected NetworkVariable<int> m_NbTeam = new NetworkVariable<int>();

	protected List<List<int>> m_Teams = new List<List<int>>();

	public static PVPGameMode Instance()
	{
		return instance as PVPGameMode;
	}

	public void Start()
	{
		m_ShuffledPlayerIndices.OnValueChanged += _DispatchPlayersAction;
		m_NbTeam.OnValueChanged += _DispatchPlayersAction;
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		_DispatchPlayers();
	}

	// Server only
	protected virtual void _DispatchPlayers(int iNbTeams = 2)
	{
		if(!IsServer)
			return;

		NativeList<int> playersIdx = new NativeList<int>();
		playersIdx.InsertRangeWithBeginEnd(0, m_Players.Count);
		playersIdx.Shuffle();
		m_ShuffledPlayerIndices.Value = playersIdx;
	}

	protected void _DispatchPlayersAction<T>(T _, T __)
	{
		NativeList<int> shuffledIndices = m_ShuffledPlayerIndices.Value;

		Debug.Log("Shuffled player indices: " + shuffledIndices);

		m_Teams.Clear();
		for(int i = 0; i < m_NbTeam.Value; i++)
			m_Teams.Add(new List<int>());

		int teamIdx = 0;
		foreach(int playerIdx in shuffledIndices)
		{
			m_Teams[teamIdx].Add(playerIdx);

			teamIdx++;
			if(teamIdx >= m_NbTeam.Value)
				teamIdx = 0;
		}
	}

	protected List<List<int>> GetTeams()
	{
		return m_Teams;
	}
}
