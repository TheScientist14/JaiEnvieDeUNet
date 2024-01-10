using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;

public class Gamemode : NetworkBehaviour
{
	[SerializeField] private Transform m_TestTransform;

	// Start is called before the first frame update
	async void Start()
	{
		if(AuthenticationService.Instance.IsSignedIn) // players
			return;

		// works only for server
		var payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();

		NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(MultiplayService.Instance.ServerConfig.IpAddress, MultiplayService.Instance.ServerConfig.Port);

		NetworkManager.Singleton.StartServer();
	}

	// Update is called once per frame
	void Update()
	{
		if(IsServer)
			m_TestTransform.Rotate(Vector3.right * Time.deltaTime * 5);
	}
}
