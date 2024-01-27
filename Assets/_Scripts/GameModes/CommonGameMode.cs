using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class CommonGameMode : NetworkSingleton<CommonGameMode>
{
	protected NetworkList<FixedString32Bytes> m_Players = new NetworkList<FixedString32Bytes>();
	protected NetworkVariable<int> m_NbConnectedPlayers = new NetworkVariable<int>();

	public UnityEvent AllPlayersConnected;

	public void Awake()
	{
		if(AllPlayersConnected == null)
			AllPlayersConnected = new UnityEvent();

		m_NbConnectedPlayers.OnValueChanged += CheckForAllPlayersConnected;
		AllPlayersConnected.AddListener(OnAllPlayersConnected);

		m_NbConnectedPlayers.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
	}

	private void CheckForAllPlayersConnected(int iPrevVal, int iCurVal)
	{
		if(iPrevVal != m_Players.Count && iCurVal == m_Players.Count)
			AllPlayersConnected.Invoke();
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		if(IsServer)
		{

			foreach (var player in NetworkInit.s_PayloadAllocation.MatchProperties.Players)
			{
				m_Players.Add(new FixedString32Bytes(
					player.CustomData.GetAs<Dictionary<string, string>>()
						.GetValueOrDefault("Name", "UnknownPlayer")
					));
			}
				
			//m_Players.SetDirty(true);
		}
	}

	public void OnPlayerConnected()
	{
		m_NbConnectedPlayers.Value += 1;
	}

	public List<FixedString32Bytes> GetPlayers()
	{
		List<FixedString32Bytes> players = new List<FixedString32Bytes>();
		
		foreach (var player in m_Players)
		{
			players.Add(player.Value);
		}
		
		return players;
	}

	protected virtual void OnAllPlayersConnected()
	{
		Debug.Log("All players Connected");
	}
}
