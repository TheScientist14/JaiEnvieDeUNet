using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;

public class NetworkInit : NetworkBehaviour
{
	[SerializeField] private NetworkManager m_NetworkManagerPrefab;

	// Start is called before the first frame update
	async void Start()
	{
		if(UnityServices.State != ServicesInitializationState.Uninitialized)
		{
			Debug.Log("Init client");
			NetworkManager.Singleton.StartClient();

			if(LobbyManager.instance.IsLobbyHost())
			{
				Debug.Log("Loading scene");
				LoadASceneServerRPC(LobbyManager.instance.GetGamemode().ToString());
			}

			return;
		}

		Instantiate(m_NetworkManagerPrefab);

		await UnityServices.InitializeAsync();
		Debug.Log("Unity services initialized");

		// works only for server
		var payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();

		if(payloadAllocation == null)
			Debug.LogError("No allocation");

		NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
			MultiplayService.Instance.ServerConfig.IpAddress, MultiplayService.Instance.ServerConfig.Port, "0.0.0.0");
		NetworkManager.Singleton.StartServer();
		NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(UnityEngine.SceneManagement.LoadSceneMode.Additive);
		Debug.Log("NGO initialized");

		Application.targetFrameRate = 60;

		await MultiplayService.Instance.ReadyServerForPlayersAsync();
		Debug.Log("Server is ready for players");
	}

	[ServerRpc(RequireOwnership = false)]
	public void LoadASceneServerRPC(string iSceneName)
	{
		NetworkManager.Singleton.SceneManager.LoadScene(iSceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
	}
}
