using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace _Scripts.MapObjects
{
    [RequireComponent(typeof(BoxCollider), typeof(NetworkObject), typeof(NetworkTransform))]
    public class SpawnPointBehaviour : NetworkBehaviour
    {
        private NetworkVariable<bool> _canSpawn;

        public bool CanSpawn => _canSpawn.Value;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerBehaviour>())
            {
                _canSpawn.Value = false;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<PlayerBehaviour>())
            {
                _canSpawn.Value = true;
            }
        }
    }
}