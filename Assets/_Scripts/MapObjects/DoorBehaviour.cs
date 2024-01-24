using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class DoorBehaviour : MonoBehaviour
{
    [SerializeField] private Transform positionToTp;
    [SerializeField] private bool isChangeScene = false;
    [SerializeField] [EnableIf("isChangeScene")] private string sceneToChangeTo;
    
    private GameObject PlayerToTP;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (!isChangeScene)
            {
                PlayerToTP = other.gameObject;
                 TpPlayer();
            }
            else
            {
                LoadBossSceneServerRPC();
            }
        }
    }
    
    private void TpPlayer()
    {
        PlayerToTP.transform.position = positionToTp.position;
        PlayerToTP.transform.rotation = positionToTp.rotation;
    }

    [ServerRpc]
    private async void LoadBossSceneServerRPC()
    {
        
        var sceneEventProgressStatus = NetworkManager.Singleton.SceneManager.LoadScene(sceneToChangeTo, LoadSceneMode.Additive);

        
        if (sceneEventProgressStatus == SceneEventProgressStatus.InvalidSceneName) return;
        if (sceneEventProgressStatus == SceneEventProgressStatus.InternalNetcodeError) return;
        //if (sceneEventProgressStatus == SceneEventProgressStatus.SceneFailedVerification) return;
            
        while (sceneEventProgressStatus == SceneEventProgressStatus.SceneEventInProgress) await Task.Delay(10);
        
        TpPlayer();
    }
}
