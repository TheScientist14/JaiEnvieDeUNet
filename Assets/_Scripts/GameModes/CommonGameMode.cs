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
	protected NetworkVariable<NativeList<FixedString32Bytes>> m_Players = new NetworkVariable<NativeList<FixedString32Bytes>>();
	protected NetworkVariable<int> m_NbConnectedPlayers = new NetworkVariable<int>();

	public UnityEvent AllPlayersConnected;

	public void Awake()
	{
		if(AllPlayersConnected == null)
			AllPlayersConnected = new UnityEvent();

		m_NbConnectedPlayers.OnValueChanged += CheckForAllPlayersConnected;

		m_NbConnectedPlayers.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
	}

	private void CheckForAllPlayersConnected(int iPrevVal, int iCurVal)
	{
		if(iPrevVal != m_Players.Value.Length && iCurVal == m_Players.Value.Length)
			AllPlayersConnected.Invoke();
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		if(IsServer)
		{
			m_Players.Value.CopyFromNBC(
				NetworkInit.s_PayloadAllocation.MatchProperties.Players.ConvertAll(
					p => new FixedString32Bytes(p.CustomData.GetAs<Dictionary<string, string>>().GetValueOrDefault("Name", "UnknownPlayer"))
				).ToArray()
			);
			m_Players.SetDirty(true);
		}
	}

	public void OnPlayerConnected()
	{
		m_NbConnectedPlayers.Value += 1;
	}

	public List<FixedString32Bytes> GetPlayers()
	{
		return m_Players.Value.ToList();
	}
}
