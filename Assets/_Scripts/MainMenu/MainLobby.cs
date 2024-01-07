using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class MainLobby : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject parentUIObject;
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private GameObject[] UIToDeactivate;
    
    [SerializeField] private TMP_Text lobbyName;
    private LobbyManager _lobbyManager;
    
    void Start()
    {
        _lobbyManager = LobbyManager.instance;
        
        _lobbyManager.lobbyCreated.AddListener(InitLobbyUI);
    }

    private void InitLobbyUI()
    {
        foreach (var UIObject in UIToDeactivate)
        {
            UIObject.SetActive(false);
        }
        lobbyUI.SetActive(true);
        lobbyName.text = _lobbyManager.Lobby.Name;

        var playerNameText = Instantiate(playerPrefab, parentUIObject.transform).GetComponentInChildren<TMP_Text>();
        playerNameText.text = _lobbyManager.PlayerName;
    }
    
}
