using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlayerCamera : MonoBehaviour
{
    public Transform player;

    void Update()
    {
        transform.position = player.transform.position;
        transform.rotation = player.transform.localRotation;
    }

    private void FixedUpdate()
    {
        //transform.rotation = player.transform.localRotation;
    }
}
