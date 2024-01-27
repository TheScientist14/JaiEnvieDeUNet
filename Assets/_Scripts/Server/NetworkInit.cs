using System;
using NaughtyAttributes;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkInit : NetworkBehaviour
{
	[SerializeField] private NetworkManager m_NetworkManagerPrefab;

	private Gamemodes _gameMode;

	public static MatchmakingResults s_PayloadAllocation; // available only on server

	// Start is called before the first frame update
	async void Start()
	{
		if(UnityServices.State != ServicesInitializationState.Uninitialized)
		{
			Debug.Log("Init client ");


			bool success = NetworkManager.Singleton.StartClient();
			Debug.Log("Started client : " + success);



			NetworkManager.Singleton.OnTransportFailure += () => { Debug.Log("Transport Failure"); };
			NetworkManager.Singleton.OnClientDisconnectCallback += b => { Debug.Log($"Client Disconnected : Was host : {b}"); };
			//NetworkManager.Singleton.OnClientStarted += OnClientStarted;
			NetworkManager.Singleton.OnClientConnectedCallback += obj =>
			{
				OnClientStarted();
			};

			return;
		}

		Instantiate(m_NetworkManagerPrefab);

		await UnityServices.InitializeAsync();
		Debug.Log("Unity services initialized");

		// works only for server
		s_PayloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();

		_gameMode = GetGameModeFromQueueName(s_PayloadAllocation.QueueName);

		if(s_PayloadAllocation == null)
			Debug.LogError("No allocation");

		NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
			MultiplayService.Instance.ServerConfig.IpAddress, MultiplayService.Instance.ServerConfig.Port, "0.0.0.0");
		NetworkManager.Singleton.StartServer();
		NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
		Debug.Log("NGO initialized");

		Application.targetFrameRate = 60;

		await MultiplayService.Instance.ReadyServerForPlayersAsync();
		Debug.Log("Server is ready for players");
	}

	[Button("Start")]
	public void OnClientStarted()
	{
		Debug.Log("Client started");

		if(LobbyManager.instance.IsLobbyHost())
		{
			Debug.Log("Loading scene");
			NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
			LoadGameModeSceneServerRPC();
		}
	}

	private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
	{
		// Both client and server receive these notifications
		switch(sceneEvent.SceneEventType)
		{
			// Handle server to client Load Notifications
			case SceneEventType.Load:
			{
				Debug.Log("SceneLoad");
				// This event provides you with the associated AsyncOperation
				// AsyncOperation.progress can be used to determine scene loading progression
				var asyncOperation = sceneEvent.AsyncOperation;
				// Since the server "initiates" the event we can simply just check if we are the server here
				if(IsServer)
				{
					// Handle server side load event related tasks here
				}
				else
				{
					// Handle client side load event related tasks here
				}
				break;
			}
			// Handle server to client unload notifications
			case SceneEventType.Unload:
			{
				Debug.Log("SceneUnload");
				// You can use the same pattern above under SceneEventType.Load here
				break;
			}
			// Handle client to server LoadComplete notifications
			case SceneEventType.LoadComplete:
			{
				Debug.Log("SceneLoadComplete");
				// This will let you know when a load is completed
				// Server Side: receives thisn'tification for both itself and all clients
				if(IsServer)
				{
					if(sceneEvent.ClientId == NetworkManager.LocalClientId)
					{
						// Handle server side LoadComplete related tasks here
					}
					else
					{
						// Handle client LoadComplete **server-side** notifications here
					}
				}
				else // Clients generate this notification locally
				{
					// Handle client side LoadComplete related tasks here
				}

				// So you can use sceneEvent.ClientId to also track when clients are finished loading a scene
				break;
			}
			// Handle Client to Server Unload Complete Notification(s)
			case SceneEventType.UnloadComplete:
			{
				Debug.Log("SceneUnloadComplete");
				// This will let you know when an unload is completed
				// You can follow the same pattern above as SceneEventType.LoadComplete here

				// Server Side: receives this notification for both itself and all clients
				// Client Side: receives this notification for itself

				// So you can use sceneEvent.ClientId to also track when clients are finished unloading a scene
				break;
			}
			// Handle Server to Client Load Complete (all clients finished loading notification)
			case SceneEventType.LoadEventCompleted:
			{
				Debug.Log("SceneLoadedEventComplete");
				// This will let you know when all clients have finished loading a scene
				// Received on both server and clients
				foreach(var clientId in sceneEvent.ClientsThatCompleted)
				{
					// Example of parsing through the clients that completed list
					if(IsServer)
					{
						// Handle any server-side tasks here
					}
					else
					{
						// Handle any client-side tasks here
					}
				}
				break;
			}
			// Handle Server to Client unload Complete (all clients finished unloading notification)
			case SceneEventType.UnloadEventCompleted:
			{
				Debug.Log("SceneUnloadEventCompleted");
				// This will let you know when all clients have finished unloading a scene
				// Received on both server and clients
				foreach(var clientId in sceneEvent.ClientsThatCompleted)
				{
					// Example of parsing through the clients that completed list
					if(IsServer)
					{
						// Handle any server-side tasks here
					}
					else
					{
						// Handle any client-side tasks here
					}
				}
				break;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void LoadGameModeSceneServerRPC(ServerRpcParams serverRpcParams = default)
	{
		NetworkManager.SceneManager.LoadScene(_gameMode.ToString(), LoadSceneMode.Additive);
	}

	public static Gamemodes GetGameModeFromQueueName(string queueName)
	{
		switch(queueName)
		{
			default:
				return Gamemodes.PVE;
			case "TDMQueue":
				return Gamemodes.TeamDeathmatch;
			case "KotHQueue":
				return Gamemodes.KingOfTheHill;
			case "FfAQueue":
				return Gamemodes.FFA;
		}
	}
}
