using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace _Scripts.MapObjects
{
    [RequireComponent(typeof(BoxCollider))]
    public class SpawnPointBehaviour : MonoBehaviour
    {
        private bool _canSpawn;

        public bool CanSpawn => _canSpawn;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerBehaviour>())
            {
                _canSpawn = false;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<PlayerBehaviour>())
            {
                _canSpawn = true;
            }
        }
    }
}