using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerLookAt : NetworkBehaviour
{
    [SerializeField] private Transform cameraTransform;

    private void Start()
    {
        if (!IsOwner)
        {
            gameObject.layer = 0;
            return;
        }
        gameObject.layer = 8;
    }

    private void FixedUpdate()
    {
        transform.LookAt(-cameraTransform.up);
    }
}
