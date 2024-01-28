using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class CommonGameMode : NetworkSingleton<CommonGameMode>
{
	protected NetworkList<FixedString32Bytes> m_PlayerNames;
	protected NetworkList<ulong> m_PlayerIds;

	protected NetworkVariable<int> m_PlayerCount = new NetworkVariable<int>();
	protected NetworkVariable<int> m_NbConnectedPlayers = new NetworkVariable<int>();

	public UnityEvent AllPlayersConnected;

	public virtual void Awake()
	{
		if(AllPlayersConnected == null)
			AllPlayersConnected = new UnityEvent();

		m_PlayerNames = new NetworkList<FixedString32Bytes>();
		m_PlayerIds = new NetworkList<ulong>();

		m_NbConnectedPlayers.OnValueChanged += CheckForAllPlayersConnected;
		AllPlayersConnected.AddListener(OnAllPlayersConnected);
	}

	private void CheckForAllPlayersConnected(int iPrevVal, int iCurVal)
	{
		if(iPrevVal != m_PlayerCount.Value && iCurVal == m_PlayerCount.Value)
			AllPlayersConnected.Invoke();
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		if(IsServer)
		{
			m_PlayerCount.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;

			m_NbConnectedPlayers.Value = 0;
			// no need ? It seems to be called for every players that is already connected
			/*foreach(ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
				_OnPlayerConnected(clientId);*/

			NetworkManager.Singleton.OnClientConnectedCallback += _OnPlayerConnected;
		}
	}

	void _OnPlayerConnected(ulong iPlayerId)
	{
		ClientRpcParams clientRpcParams = new ClientRpcParams
		{
			Send = new ClientRpcSendParams
			{
				TargetClientIds = new ulong[] { iPlayerId }
			}
		};

		_RegisterClientRpc(iPlayerId, clientRpcParams);
	}

	[ClientRpc]
	void _RegisterClientRpc(ulong iClientId, ClientRpcParams iClientRpcParams)
	{
		Debug.Log($"Registering client to server {NetworkManager.Singleton.LocalClient.ClientId} || {iClientId}");
		if(!IsClient || NetworkManager.Singleton.LocalClient.ClientId != iClientId)
			return;

		Debug.Log("Registering client to server but different");
		_ReceiveClientInfoServerRpc(iClientId, new FixedString32Bytes(LobbyManager.instance.PlayerName));
	}

	[ServerRpc(RequireOwnership = false)]
	void _ReceiveClientInfoServerRpc(ulong iPlayerId, FixedString32Bytes iPlayerName)
	{
		m_PlayerIds.Add(iPlayerId);
		m_PlayerNames.Add(iPlayerName);
		m_NbConnectedPlayers.Value += 1;
	}

	public List<FixedString32Bytes> GetPlayerNames()
	{
		List<FixedString32Bytes> players = new List<FixedString32Bytes>();
		foreach(FixedString32Bytes playerName in m_PlayerNames)
			players.Add(playerName.Value);

		return players;
	}

	public List<ulong> GetPlayerIds()
	{
		List<ulong> players = new List<ulong>();
		foreach(ulong playerId in m_PlayerIds)
			players.Add(playerId);

		return players;
	}

	protected virtual void OnAllPlayersConnected()
	{
		Debug.Log("All players Connected");
	}
}
