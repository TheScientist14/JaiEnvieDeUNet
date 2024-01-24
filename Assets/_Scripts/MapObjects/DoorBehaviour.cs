using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class DoorBehaviour : NetworkBehaviour
{
    [SerializeField] private Transform positionToTp;
    [SerializeField] private bool isChangeScene = false;
    [SerializeField] [EnableIf("isChangeScene")] private string sceneToChangeTo;
    
    private GameObject PlayerToTP;
    private bool sceneLoaded = false;
    
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)return;
        
        if (other.gameObject.CompareTag("Player"))
        {
            if (!isChangeScene)
            {
                PlayerToTP = other.gameObject;
                 TpPlayer();
            }
            else if (!sceneLoaded)
            {
                PlayerToTP = other.gameObject;
                LoadBossScene();
            }
        }
    }
    
    private void TpPlayer()
    {
        PlayerToTP.transform.position = positionToTp.position;
        PlayerToTP.transform.rotation = positionToTp.rotation;
        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= (id, sceneName, mode) => TpPlayer();
    }
    
    private void LoadBossScene()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete += (id, sceneName, mode) => TpPlayer();
        NetworkManager.Singleton.SceneManager.LoadScene(sceneToChangeTo, LoadSceneMode.Additive);
        
    }
}
