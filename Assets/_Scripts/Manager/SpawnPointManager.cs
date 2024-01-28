using System.Collections;
using System.Collections.Generic;
using _Scripts.MapObjects;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using _Scripts.Helpers;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[RequireComponent(typeof(BoxCollider), typeof(NetworkObject))]
public class SpawnPointManager : NetworkBehaviour
{
    [SerializeField] private SpawnPointBehaviour spawnPointBehaviourPrefab;
    [SerializeField] private List<BoxCollider> noSpawnBoxColliders;
    
    private BoxCollider _spawnArea;
    private List<SpawnPointBehaviour> _spawnPoints;

    
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        TryGetComponent(out _spawnArea);
        
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += SingletonOnOnClientConnectedCallback;
        }
    }

    private void SingletonOnOnClientConnectedCallback(ulong obj)
    {

        SpawnPointBehaviour spawnPoint = CreateSpawnPointAndAddToList();

        NetworkManager.Singleton.ConnectedClients[obj].PlayerObject.gameObject.transform.position = spawnPoint.gameObject.transform.position;
    }

    private SpawnPointBehaviour CreateSpawnPointAndAddToList()
    {
        SpawnPointBehaviour spawnPoint = Instantiate(spawnPointBehaviourPrefab, GetRandomPositionInBox(), quaternion.identity);
        _spawnPoints.Add(spawnPoint);
        
        spawnPoint.GetComponent<NetworkObject>().Spawn();

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

    public Vector3 RandomAvailableRespawnPoint()
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
    
}
