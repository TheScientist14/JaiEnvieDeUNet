using Unity.Netcode;
using UnityEngine;

public class PlayerIgnoreGameObjectInRender : NetworkBehaviour
{
    private void Start()
    {
        if (!IsOwner)
        {
            gameObject.layer = 0;
            return;
        }
        gameObject.layer = 8;
    }
}
