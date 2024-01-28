using System.Collections;
using System.Collections.Generic;
using _Scripts.MapObjects;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using _Scripts.Helpers;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[RequireComponent(typeof(BoxCollider))]
public class SpawnPointManager : NetworkSingleton<SpawnPointManager>
{
    [SerializeField] private SpawnPointBehaviour spawnPointBehaviourPrefab;
    [SerializeField] private List<BoxCollider> noSpawnBoxColliders;
    
    private BoxCollider _spawnArea;
    private List<SpawnPointBehaviour> _spawnPoints = new ();

    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        TryGetComponent(out _spawnArea);

        _spawnArea.isTrigger = true;
        
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += SingletonOnOnClientConnectedCallback;
        }
    }

    private void SingletonOnOnClientConnectedCallback(ulong obj)
    {
        SpawnPointBehaviour spawnPoint = CreateSpawnPointAndAddToList();

        ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[]{obj}
                    }
                };
        
        MovePlayerClientRpc(spawnPoint.transform.position, clientRpcParams);
    }

    [ClientRpc]
    private void MovePlayerClientRpc(Vector3 position, ClientRpcParams clientRpcParams = default)
    {
        NetworkManager.Singleton.ConnectedClients[clientRpcParams.Send.TargetClientIds[0]].PlayerObject.gameObject.transform.position = position;
    }
    

    private SpawnPointBehaviour CreateSpawnPointAndAddToList()
    {
        SpawnPointBehaviour spawnPoint = Instantiate(spawnPointBehaviourPrefab, GetRandomPositionInBox(), quaternion.identity);
        _spawnPoints.Add(spawnPoint);

        return spawnPoint;
    }

    private Vector3 GetRandomPositionInBox()
    {
        var bounds = _spawnArea.bounds;
        Vector3 randomCoord = bounds.center + new Vector3(bounds.extents.x * Random.Range(-1 , 1), bounds.center.y, bounds.extents.z * Random.Range(-1 , 1));


        foreach (BoxCollider noSpawnZone in noSpawnBoxColliders)
        {
            if (noSpawnZone.bounds.Contains(randomCoord))
            {
                return GetRandomPositionInBox();
            }
        }
        return randomCoord;
    }
    
    
    private Vector3 RandomAvailableRespawnPoint()
    {
        _spawnPoints.Shuffle();
        foreach (var spawnPoint in _spawnPoints)
        {
            if (spawnPoint.CanSpawn)
            {
               return spawnPoint.transform.position;
            }
        }

        return CreateSpawnPointAndAddToList().transform.position;

    }
    
    /// <summary>
    /// This method when used will tp a game object to a spawn point.
    ///
    /// WARNING : Use server side ONLY, clients do not have data 
    /// </summary>
    /// <param name="prmGameObject"></param>
    public void MoveGameObjectToSpawnPoint(GameObject prmGameObject)
    {
        prmGameObject.transform.position = RandomAvailableRespawnPoint();
    }

    
    /// <summary>
    /// This method is a ServerRPC to move a player to a spawn point.
    ///
    /// Given ID should be client id key of the network manager connected client list 
    /// </summary>
    /// <param name="playerID"></param>
    
    [ServerRpc]
    public void MovePlayerToSpawnPointServerRPC(ulong playerID)
    {
        GameObject player = NetworkManager.Singleton.ConnectedClients[playerID].PlayerObject.gameObject;
        
        //need to change to a client rpc
        
        MoveGameObjectToSpawnPoint(player);
        
    }
}
