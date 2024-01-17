using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NaughtyAttributes;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameConstants;
using Player = Unity.Services.Lobbies.Models.Player;
using PlayerM = Unity.Services.Matchmaker.Models.Player;
using DataObject = Unity.Services.Lobbies.Models.DataObject;

public class LobbyManager : Singleton<LobbyManager>
{
	[SerializeField] private float heartBeatFrequency = 15f;

	private string _playerName;
	private Gamemodes _gamemode = 0;
	private Lobby _lobby;
	private bool _IsOwnerOfLobby = false;
	private bool _IsWaitingForTicket = false;
	private IServerEvents _serverEvents;
	private ILobbyEvents _lobbyEvents;
	private MultiplayEventCallbacks _multiplayEventCallbacks = new MultiplayEventCallbacks();
	private LobbyEventCallbacks _lobbyEventCallbacks = new LobbyEventCallbacks();

	public UnityEvent lobbyCreated;
	public UnityEvent lobbyJoined;
	public UnityEvent kickedEvent;
	public UnityEvent refreshUI;
	public UnityEvent init;

	public string PlayerName
	{
		get => _playerName;
		set => _playerName = value;
	}

	public Lobby Lobby
	{
		get => _lobby;
	}


	private async void OnDestroy()
	{
		if(_lobby != null)
		{
			await LeaveLobby();
		}
	}

	public void ChangeGamemode(Int32 dropdown)
	{
		_gamemode = (Gamemodes)dropdown;
	}

	public async void Init()
	{
		var options = new InitializationOptions();
		options.SetProfile(_playerName);

		if(UnityServices.State != ServicesInitializationState.Initialized)
			await UnityServices.InitializeAsync(options);
		if(!AuthenticationService.Instance.IsSignedIn)
			await AuthenticationService.Instance.SignInAnonymouslyAsync();

		Debug.Log(AuthenticationService.Instance.PlayerId);

		_lobbyEventCallbacks.LobbyChanged += OnLobbyChanged;
		_lobbyEventCallbacks.KickedFromLobby += OnKickedFromLobby;
		_lobbyEventCallbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;

		init.Invoke();
	}

	private void OnLobbyEventConnectionStateChanged(LobbyEventConnectionState obj)
	{
		Debug.Log("StateChange");
	}

	private void OnKickedFromLobby()
	{
		Debug.Log("Kicked");
		_lobby = null;
		kickedEvent.Invoke();
	}
	private async void OnLobbyChanged(ILobbyChanges lobbyChanges)
	{
		Debug.Log("Lobby changed");
		lobbyChanges.ApplyToLobby(_lobby);

		if (!_IsOwnerOfLobby)
		{
			JoinServer();
		}
		
		refreshUI.Invoke();
	}

	private async void JoinServer()
	{
		if(_lobby.Data.ContainsKey(k_ServerIp))
		{
			Debug.Log("Joining Server");
			string ip = _lobby.Data[k_ServerIp].Value;
			ushort port = ushort.Parse(_lobby.Data[k_ServerPort].Value);
            
			await LeaveLobby();
            
			// init NGO client side
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, port);
            
			Debug.Log("Connected to " + ip + ":" + port);
            
			SceneManager.LoadScene("PVE");
		}
	}

	public async Task JoinLobby(String joinCode, bool isLobbyCode = false)
	{
		try
		{
			if(isLobbyCode)
			{
				JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
				{
					Player = GetPlayer()
				};
				_lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(joinCode, joinLobbyByCodeOptions);
				lobbyJoined.Invoke();
			}
			else
			{
				JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
				{
					Player = GetPlayer()
				};
				_lobby = await LobbyService.Instance.JoinLobbyByIdAsync(joinCode, joinLobbyByIdOptions);
				lobbyJoined.Invoke();
			}

			SubToLobbyEvents();

		}
		catch(LobbyServiceException e)
		{
			Debug.Log(e);
		}
	}


	public async Task<QueryResponse> GetAllLobbies()
	{
		QueryResponse lobbies = null;
		try
		{
			QueryLobbiesOptions options = new QueryLobbiesOptions();
			options.Count = 25;

			// Filter for open lobbies only
			options.Filters = new List<QueryFilter>()
			{
				new QueryFilter(
					field: QueryFilter.FieldOptions.AvailableSlots,
					op: QueryFilter.OpOptions.GT,
					value: "0")
			};

			// Order by newest lobbies first
			options.Order = new List<QueryOrder>()
			{
				new QueryOrder(
					asc: false,
					field: QueryOrder.FieldOptions.Created)
			};

			lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);
		}
		catch(LobbyServiceException e)
		{
			Debug.Log(e);
		}

		return lobbies;
	}



	[Button("CreateLobby")]
	public async void CreateLobby()
	{
		string lobbyName = _playerName + "'s Lobby";
		int maxPlayers = 4;
		CreateLobbyOptions options = new CreateLobbyOptions();
		options.IsPrivate = false;
		options.Player = GetPlayer();
		options.Data = new Dictionary<string, DataObject>
		{
			{"Gamemode", new DataObject(DataObject.VisibilityOptions.Public, _gamemode.ToString())}
		};

		try
		{
			if(_lobby == null)
			{
				_lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
				_IsOwnerOfLobby = true;

				StartCoroutine(LobbyHeartBeat());
				SubToLobbyEvents();

				lobbyCreated.Invoke();
			}
		}
		catch(Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	public async void StartGame()
	{
		if(!_IsOwnerOfLobby || _IsWaitingForTicket)
			return;

		_IsWaitingForTicket = true;

		List<PlayerM> players = new List<PlayerM>();
		foreach(Player player in _lobby.Players)
			players.Add(new PlayerM(player.Id));

		// Set options for matchmaking
		var options = new CreateTicketOptions(
		  "DefaultQ", // The name of the queue defined in the previous step, 
		  new Dictionary<string, object>());

		// Create ticket
		var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);

		// Print the created ticket id
		Debug.Log(ticketResponse.Id);


		UpdateLobbyOptions updateOptions = new UpdateLobbyOptions();

		updateOptions.IsLocked = true;

		Debug.Log("UpdatingLobby");
		await LobbyService.Instance.UpdateLobbyAsync(Lobby.Id, updateOptions);
		Debug.Log("Updated lobby");

		WaitForTicket(ticketResponse.Id);
	}

	private async void WaitForTicket(string prmTicketId)
	{
		Debug.Log("Wait");

		MultiplayAssignment assignment = null;
		bool gotAssignment = false;
		do
		{
			Debug.Log("Waiting");
			await Task.Delay(TimeSpan.FromSeconds(1.1f));

			// Poll ticket
			TicketStatusResponse ticketStatus = await MatchmakerService.Instance.GetTicketAsync(prmTicketId);

			if(ticketStatus == null)
				continue;

			//Convert to platform assignment data (IOneOf conversion)
			if(ticketStatus.Value is MultiplayAssignment value)
			{
				assignment = value;
			}

			if(assignment == null)
				continue;

			switch(assignment.Status)
			{
				case MultiplayAssignment.StatusOptions.Found:
					gotAssignment = true;
					break;
				case MultiplayAssignment.StatusOptions.InProgress:
					//...
					break;
				case MultiplayAssignment.StatusOptions.Failed:
					gotAssignment = true;
					Debug.LogError("Failed to get ticket status. Error: " + assignment.Message);
					break;
				case MultiplayAssignment.StatusOptions.Timeout:
					gotAssignment = true;
					Debug.LogError("Failed to get ticket status. Ticket timed out.");
					break;
				default:
					throw new InvalidOperationException();
			}

		} while(!gotAssignment);

		Debug.Log("Updating Lobby");
		
		UpdateLobbyOptions updateOptions = new UpdateLobbyOptions();

		updateOptions.Data = new Dictionary<string, DataObject>()
		{
			{
				k_ServerIp, new DataObject(DataObject.VisibilityOptions.Member, assignment.Ip)
			},
			{
				k_ServerPort, new DataObject(DataObject.VisibilityOptions.Member, assignment.Port.ToString())
			}
		};

		await LobbyService.Instance.UpdateLobbyAsync(Lobby.Id, updateOptions);

		Debug.Log("Server Found");
		
		JoinServer();
	}

	private async void SubToLobbyEvents()
	{
		try
		{
			_lobbyEvents = await Lobbies.Instance.SubscribeToLobbyEventsAsync(_lobby.Id, _lobbyEventCallbacks);
		}
		catch(LobbyServiceException ex)
		{
			switch(ex.Reason)
			{
				case LobbyExceptionReason.AlreadySubscribedToLobby:
					Debug.LogWarning($"Already subscribed to lobby[{LobbyManager.instance.Lobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}");
					break;
				case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy:
					Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}");
					throw;
				case LobbyExceptionReason.LobbyEventServiceConnectionError:
					Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}");
					throw;
				default:
					throw;
			}
		}
	}

	private async Task UnSubToLobbyEvents()
	{
		try
		{
			Debug.Log("Unsubing from Lobby events");
			await _lobbyEvents.UnsubscribeAsync();
		}
		catch(Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	public async Task LeaveLobby()
	{
		Debug.Log("LeavingLobby");

		if (_lobby == null)
		{
			return;
		}
        
		await UnSubToLobbyEvents();

		if(_IsOwnerOfLobby)
		{
			foreach(var player in _lobby.Players)
			{
				if(player.Id != AuthenticationService.Instance.PlayerId)
				{
					try
					{
						await LobbyService.Instance.RemovePlayerAsync(_lobby.Id, player.Id);
						Debug.Log($"Kicked {player.Id}");
					}
					catch { }
				}
			}
			
			Debug.Log("Kicked all players");
			

			try
			{
				await LobbyService.Instance.DeleteLobbyAsync(_lobby.Id);
				Debug.Log("Deleted Lobby");
			}
			catch(Exception e)
			{
				Console.WriteLine(e);
				throw;
			}

		}
		else
		{
			try
			{
				await LobbyService.Instance.RemovePlayerAsync(_lobby.Id, AuthenticationService.Instance.PlayerId);
				Debug.Log("Removed self from lobby");
			}
			catch
			{
				Debug.LogWarning("Could not remove player from lobby");
			}
		}

		//Calls Kicked Event for host and kickes clients

		_lobby = null;
		kickedEvent.Invoke();
	}

	private IEnumerator LobbyHeartBeat()
	{
		while(_lobby != null)
		{
			LobbyService.Instance.SendHeartbeatPingAsync(_lobby.Id);

			yield return new WaitForSeconds(heartBeatFrequency);
		}
	}

	private Player GetPlayer()
	{
		return new Player(
			id: AuthenticationService.Instance.PlayerId,
			data: new Dictionary<string, PlayerDataObject>
			{
				{ "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, _playerName) }
			}
		);
	}
}
