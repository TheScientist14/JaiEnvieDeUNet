using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;

public class Gamemode : NetworkBehaviour
{
	[SerializeField] private Transform m_TestTransform;

	// Start is called before the first frame update
	async void Start()
	{
		if(UnityServices.State != ServicesInitializationState.Uninitialized)
		{
			Debug.Log("Init client");
			NetworkManager.Singleton.StartClient();
			return;
		}

		await UnityServices.InitializeAsync();
		Debug.Log("Unity services initialized");

		// works only for server
		var payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();

		if(payloadAllocation == null)
		{
			Debug.LogError("No allocation");
		}

		NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
			MultiplayService.Instance.ServerConfig.IpAddress, MultiplayService.Instance.ServerConfig.Port, "0.0.0.0");
		NetworkManager.Singleton.StartServer();
		Debug.Log("NGO initialized");

		await MultiplayService.Instance.ReadyServerForPlayersAsync();
		Debug.Log("Server is ready for players");
	}

	// Update is called once per frame
	void Update()
	{
		if(!IsServer)
			return;

		m_TestTransform.Rotate(Vector3.right * Time.deltaTime * 5);
	}
}
