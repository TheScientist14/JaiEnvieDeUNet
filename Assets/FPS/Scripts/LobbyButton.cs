using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button),typeof(RawImage))]
public class LobbyButton : MonoBehaviour
{
   [SerializeField] private TextMeshProUGUI lobbyNameText;
   [SerializeField] private TextMeshProUGUI playerCountText;
   private string _lobbyId; 
   
   private Button _btn;
   
   private void Start()
   {
      TryGetComponent(out _btn);
      _btn.onClick.AddListener(Selected);
   }

   public void InitButton(string lobbyId, string lobbyName, string player)
   {
      _lobbyId = lobbyId;
      lobbyNameText.text = lobbyName;
      playerCountText.text = player;
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
