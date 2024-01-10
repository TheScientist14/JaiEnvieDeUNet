using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NaughtyAttributes;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyManager : Singleton<LobbyManager>
{
	[SerializeField] private Button btn;
	[SerializeField] private LobbyButton prefab;
	[SerializeField] private GameObject parentMenu;
	[SerializeField] private float heartBeatFrequency = 15f;

	private string _playerName;
	private Gamemodes _gamemode = 0;
	private Lobby _lobby;
	private bool _IsOwnerOfLobbyQuoi = false;
	private ILobbyEvents m_LobbyEvents;
	private LobbyEventCallbacks callbacks = new LobbyEventCallbacks();


	public UnityEvent lobbyCreated;
	public UnityEvent lobbyJoined;
	public UnityEvent kickedEvent;
	public UnityEvent refreshUI;

	public string PlayerName
	{
		get => _playerName;
		set => _playerName = value;
	}

	public Lobby Lobby
	{
		get => _lobby;
		set => _lobby = value;
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

		GetAndGenerateAllLobbies();
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
		refreshUI.Invoke();
	}

	public void ButtonSelected(LobbyButton lobbyButton)
	{
		btn.onClick.RemoveAllListeners();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		btn.onClick.AddListener(() => JoinLobby(lobbyButton.GetLobbyId()));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
	}

	private async Task JoinLobby(String joinCode, bool isLobbyCode = false)
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

	[Button("Refresh List")]
	private async void GetAndGenerateAllLobbies()
	{
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

			QueryResponse lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);

			CreateListOfLobbiesInMenu(lobbies);
		}
		catch(LobbyServiceException e)
		{
			Debug.Log(e);
		}
	}

	private void CreateListOfLobbiesInMenu(QueryResponse lobbies)
	{
		for(int i = 0; i < parentMenu.transform.childCount; i++)
		{
			Destroy(parentMenu.transform.GetChild(i).gameObject);
		}

		foreach(Lobby lobby in lobbies.Results)
		{
			LobbyButton lbyBtn = Instantiate(prefab, parentMenu.transform);
			lbyBtn.InitButton(lobby.Id, lobby.Name, lobby.Players.Count + "/" + lobby.MaxPlayers);
		}
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
				_IsOwnerOfLobbyQuoi = true;
				StartCoroutine(LobbyHeartBeat());

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
		if(_IsOwnerOfLobbyQuoi)
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
