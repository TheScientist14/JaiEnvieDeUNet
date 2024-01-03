using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button),typeof(RawImage))]
public class LobbyButton : MonoBehaviour
{
   [SerializeField] private TextMeshProUGUI lobbyName;
   [SerializeField] private TextMeshProUGUI playerCount;
   private string _lobbyId; 
   
   private Button _btn;
   
   private void Start()
   {
      TryGetComponent(out _btn);
      _btn.onClick.AddListener(Selected);
   }

   public void InitButton(string lobbyId, string player)
   {
      _lobbyId = lobbyId;
      lobbyName.text = lobbyId;
      playerCount.text = player;
   }

   private void Selected()
   {
      LobbyManager.instance.ButtonSelected(this);
   }

   public string GetLobbyId()
   {
      return _lobbyId;
   }
}
