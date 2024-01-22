using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class DoorBehaviour : MonoBehaviour
{
    [SerializeField] private Transform positionToTp;
    [SerializeField] private bool isChangeScene = false;
    [SerializeField] [EnableIf("isChangeScene")] private Scene sceneToChange;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (!isChangeScene)
                    TpPlayer(other.gameObject);
        }
    }
    
    private void TpPlayer(GameObject player)
    {
        player.transform.position = positionToTp.position;
        player.transform.rotation = positionToTp.rotation;
    }

    private void ChangeScene()
    {
        //detect if all players are next to the door, and then change scene for all of them ?
        // or have one player enter and on that trigger change scene for all players 
    }
    
}
