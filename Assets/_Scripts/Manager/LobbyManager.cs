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
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Player = Unity.Services.Lobbies.Models.Player;
using PlayerM = Unity.Services.Matchmaker.Models.Player;

public class LobbyManager : Singleton<LobbyManager>
{
	[SerializeField] private float heartBeatFrequency = 15f;

	private string _playerName;
	private Gamemodes _gamemode = 0;
	private Lobby _lobby;
	private bool _IsOwnerOfLobby = false;
	private ILobbyEvents m_LobbyEvents;
	private LobbyEventCallbacks callbacks = new LobbyEventCallbacks();

	private static string ticketIdKey = "ticketId";

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


	private void OnDestroy()
	{
		if(_lobby != null)
		{
			LeaveLobby();
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

		callbacks.LobbyChanged += OnLobbyChanged;
		callbacks.KickedFromLobby += OnKickedFromLobby;
		callbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;
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
	private void OnLobbyChanged(ILobbyChanges lobbyChanges)
	{
		lobbyChanges.ApplyToLobby(_lobby);

		if(_lobby.IsLocked)
		{
			WaitForTicket();
			return;
		}

		refreshUI.Invoke();
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

			SubToEvents();

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
				
				LobbyHeartBeat();
				SubToEvents();

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
		if(!_IsOwnerOfLobby)
			return;

		List<PlayerM> players = new List<PlayerM>();
		foreach(Player player in _lobby.Players)
			players.Add(new PlayerM(player.Id, player.Data));

		// Set options for matchmaking
		var options = new CreateTicketOptions(
		  "Default", // The name of the queue defined in the previous step, 
		  new Dictionary<string, object>());

		// Create ticket
		var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);

		// Print the created ticket id
		Debug.Log(ticketResponse.Id);


		UpdateLobbyOptions updateOptions = new UpdateLobbyOptions
		{
			IsLocked = true,
			Data = { { ticketIdKey, new DataObject(DataObject.VisibilityOptions.Member, ticketResponse.Id) } }
		};
		await LobbyService.Instance.UpdateLobbyAsync(Lobby.Id, updateOptions);
	}

	private async void WaitForTicket()
	{
		if(!Lobby.Data.ContainsKey(ticketIdKey))
		{
			Debug.LogWarning("No ticket id in lobby data");
			return;
		}

		string ticketId = Lobby.Data[ticketIdKey].Value;

		MultiplayAssignment assignment = null;
		bool gotAssignment = false;
		do
		{
			await Task.Delay(TimeSpan.FromSeconds(1f));

			// Poll ticket
			TicketStatusResponse ticketStatus = await MatchmakerService.Instance.GetTicketAsync(ticketId);

			if(ticketStatus == null)
				continue;

			//Convert to platform assignment data (IOneOf conversion)
			if(ticketStatus.Value is MultiplayAssignment)
			{
				assignment = ticketStatus.Value as MultiplayAssignment;
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

		LeaveLobby();

		// init NGO client side

		NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(assignment.Ip, (ushort)assignment.Port);
		NetworkManager.Singleton.StartClient();

		SceneManager.SetActiveScene(SceneManager.GetSceneByName("PVE"));
	}

	private async void SubToEvents()
	{
		try
		{
			m_LobbyEvents = await Lobbies.Instance.SubscribeToLobbyEventsAsync(_lobby.Id, callbacks);
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

	public async void LeaveLobby()
	{
		if(_IsOwnerOfLobby)
		{
			foreach(var player in _lobby.Players)
			{
				if(player.Id != AuthenticationService.Instance.PlayerId)
				{
					await LobbyService.Instance.RemovePlayerAsync(_lobby.Id, player.Id);
				}
			}

			await LobbyService.Instance.DeleteLobbyAsync(_lobby.Id);
		}
		else
		{
			await LobbyService.Instance.RemovePlayerAsync(_lobby.Id, AuthenticationService.Instance.PlayerId);
		}

		//Call Kicked Event for host and kickes clients

		_lobby = null;
	}

	private async void LobbyHeartBeat()
	{
		while(_lobby != null)
		{
			await LobbyService.Instance.SendHeartbeatPingAsync(_lobby.Id);

			await Task.Delay(TimeSpan.FromSeconds(heartBeatFrequency));
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
