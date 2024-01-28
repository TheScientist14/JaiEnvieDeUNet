using System.Collections;
using System.Collections.Generic;
using _Scripts.Helpers;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;


public class PVPGameMode : CommonGameMode
{
	protected NetworkList<ulong> m_ShuffledPlayerIds;
	protected NetworkVariable<int> m_NbTeam = new NetworkVariable<int>();

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

		m_ShuffledPlayerIds = new NetworkList<ulong>();
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
		foreach(ulong playersId in playersIds)
			m_ShuffledPlayerIds.Add(playersId);

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
