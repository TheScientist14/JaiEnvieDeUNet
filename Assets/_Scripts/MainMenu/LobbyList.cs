using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyList : MonoBehaviour
{
    [SerializeField] private Button btn;
    [SerializeField] private LobbyButton prefab;
    [SerializeField] private GameObject parentMenu;
    
    private void Start()
    {
        LobbyManager.instance.init.AddListener(RefreshUI);
    }

    public void ButtonSelected(LobbyButton lobbyButton)
    {
        btn.onClick.RemoveAllListeners();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        btn.onClick.AddListener(() => LobbyManager.instance.JoinLobby(lobbyButton.GetLobbyId()));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }
    
    private void CreateListOfLobbiesInMenu(QueryResponse lobbies)
    {
        if (lobbies == null)
        {
            return;
        }
        
        for(int i = 0; i < parentMenu.transform.childCount; i++)
        {
            Destroy(parentMenu.transform.GetChild(i).gameObject);
        }

        foreach(Lobby lobby in lobbies.Results)
        {
            LobbyButton lbyBtn = Instantiate(prefab, parentMenu.transform);
            lbyBtn.InitButton(lobby.Id, lobby.Name, lobby.Players.Count + "/" + lobby.MaxPlayers, this);
        }
    }

    public async void RefreshUI()
    {
        Debug.Log("Refresh");
        CreateListOfLobbiesInMenu(await LobbyManager.instance.GetAllLobbies());
    }
}
