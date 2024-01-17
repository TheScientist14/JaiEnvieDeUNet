using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerLookAt : NetworkBehaviour
{
    [SerializeField] private Transform camera;

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
        transform.LookAt(-camera.up);
    }
}
