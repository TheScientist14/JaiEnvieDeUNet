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
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LobbyManager : Singleton<LobbyManager>
{
	[SerializeField] private Button btn;
	[SerializeField] private LobbyButton prefab;
	[SerializeField] private GameObject parentMenu;
	private void Start()
	{
		Init();
	}

	private async void Init()
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
				await LobbyService.Instance.JoinLobbyByCodeAsync(joinCode);
			}
			else
			{
				await LobbyService.Instance.JoinLobbyByIdAsync(joinCode);
			}
		}
		catch(LobbyServiceException e)
		{
			Debug.Log(e);
		}
	}

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
		foreach(Lobby lobby in lobbies.Results)
		{
			LobbyButton lbyBtn = Instantiate(prefab, parentMenu.transform);
			lbyBtn.InitButton(lobby.Id, lobby.Players.Count + "/" + lobby.MaxPlayers);
		}
	}

	[Button("CreateLobby")]
	private async void CreateLobby()
	{
		string lobbyName = "new lobby";
		int maxPlayers = 4;
		CreateLobbyOptions options = new CreateLobbyOptions();
		options.IsPrivate = false;

		try
		{
			Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
		}
		catch(Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}
}
