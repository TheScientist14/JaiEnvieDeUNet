using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button),typeof(RawImage))]
public class LobbyButton : MonoBehaviour
{
   [SerializeField] private TextMeshProUGUI lobbyNameText;
   [SerializeField] private TextMeshProUGUI playerCountText;
   private LobbyList _lobbyList;
   private string _lobbyId; 
   
   private Button _btn;
   
   private void Start()
   {
      TryGetComponent(out _btn);
      _btn.onClick.AddListener(Selected);
   }

   public void InitButton(string lobbyId, string lobbyName, string player, LobbyList lobbyList)
   {
      _lobbyId = lobbyId;
      lobbyNameText.text = lobbyName;
      playerCountText.text = player;
      _lobbyList = lobbyList;
   }

   private void Selected()
   {
      _lobbyList.ButtonSelected(this);
   }

   public string GetLobbyId()
   {
      return _lobbyId;
   }
}
