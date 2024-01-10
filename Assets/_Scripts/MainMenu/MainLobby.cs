using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.Serialization;

public class MainLobby : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject parentUIObject;
    [SerializeField] private GameObject lobbyUI;
    [FormerlySerializedAs("UIToDeactivate")] [SerializeField] private GameObject[] uiToDeactivate;
    [SerializeField] private TMP_Text lobbyName;

    private ILobbyEvents m_LobbyEvents;
    
    void Start()
    {
        LobbyManager.instance.lobbyCreated.AddListener(ShowUI);
        LobbyManager.instance.lobbyJoined.AddListener(ShowUI);
    }

    private async void ShowUI()
    {
        foreach (var UIObject in uiToDeactivate)
        {
            UIObject.SetActive(false);
        }

        lobbyUI.SetActive(true);
        lobbyName.text = LobbyManager.instance.Lobby.Name;
        
        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += OnLobbyChanged;
        
        try {
            m_LobbyEvents = await Lobbies.Instance.SubscribeToLobbyEventsAsync(LobbyManager.instance.Lobby.Id, callbacks);
        }
        catch (LobbyServiceException ex)
        {
            switch (ex.Reason) {
                case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{LobbyManager.instance.Lobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
                case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
                case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
                default: throw;
            }
        }
        
        RefreshUI();
    }
    
    private void OnLobbyChanged(ILobbyChanges lobbyChanges)
    {
        lobbyChanges.ApplyToLobby(LobbyManager.instance.Lobby);
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
    
}
