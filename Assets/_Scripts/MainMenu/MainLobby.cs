using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class MainLobby : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject parentUIObject;
    [SerializeField] private GameObject lobbyUI;
    [FormerlySerializedAs("UIToDeactivate")] [SerializeField] private GameObject[] uiToDeactivate;
    [SerializeField] private TMP_Text lobbyName;

    
    void Start()
    {
        LobbyManager.instance.lobbyCreated.AddListener(ShowUI);
        LobbyManager.instance.lobbyJoined.AddListener(ShowUI);
        LobbyManager.instance.refreshUI.AddListener(RefreshUI);
        LobbyManager.instance.kickedEvent.AddListener(ReturnToLobbyList);
    }

    private void ShowUI()
    {
        foreach (var UIObject in uiToDeactivate)
        {
            UIObject.SetActive(false);
        }

        lobbyUI.SetActive(true);
        lobbyName.text = LobbyManager.instance.Lobby.Name;
        
        RefreshUI();
    }

    void RefreshUI()
    {
        foreach (Transform transformGo in parentUIObject.transform)
        {
            Destroy(transformGo.gameObject);
        }
        
        foreach (var player in LobbyManager.instance.Lobby.Players)
        {
            var playerNameText = Instantiate(playerPrefab, parentUIObject.transform).GetComponentInChildren<TMP_Text>();
            playerNameText.text = player.Data["Name"].Value;
        }
    }

    private void ReturnToLobbyList()
    {
        lobbyUI.SetActive(false);
        uiToDeactivate[1].gameObject.SetActive(true);
    }
    
}
