using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NaughtyAttributes;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
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
	
	public UnityEvent lobbyCreated;

	public string PlayerName
	{
		get => _playerName;
		set => _playerName = value;
	}

	public Lobby Lobby => _lobby;

	public void ChangeGamemode(Int32 dropdown)
	{
		_gamemode = (Gamemodes)dropdown;
	}

	private async void Start()
	{
		await UnityServices.InitializeAsync();
		await AuthenticationService.Instance.SignInAnonymouslyAsync();
		GetAndGenerateAllLobbies();
	}

	public void ButtonSelected(LobbyButton lobbyButton)
	{
		btn.onClick.RemoveAllListeners();
		btn.onClick.AddListener(() => JoinLobby(lobbyButton.GetLobbyId()));
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
				await LobbyService.Instance.JoinLobbyByCodeAsync(joinCode, joinLobbyByCodeOptions);
			}
			else
			{
				JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions
				{
					Player = GetPlayer()
				};
				await LobbyService.Instance.JoinLobbyByIdAsync(joinCode,joinLobbyByIdOptions);
			}
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
		for (int i = 0; i < parentMenu.transform.childCount; i++)
		{
			Destroy(parentMenu.transform.GetChild(i).gameObject);
		}
		
		foreach(Lobby lobby in lobbies.Results)
		{
			LobbyButton lbyBtn = Instantiate(prefab, parentMenu.transform);
			lbyBtn.InitButton(lobby.Id,lobby.Name, lobby.Players.Count + "/" + lobby.MaxPlayers);
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
			_lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
			StartCoroutine(LobbyHeartBeat());
			lobbyCreated.Invoke();
		}
		catch(Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
		
		
	}

	private IEnumerator LobbyHeartBeat()
	{
		while (_lobby != null)
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
