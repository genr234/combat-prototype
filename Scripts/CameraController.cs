using System;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    public Camera playerCamera;
    public bool zoomedOut = false;
    private void Start()
    {
        
    }

    private void FixedUpdate()
    {
        var yModifier = 4;
        var zModifier = 7;
            
        if (zoomedOut)
        {
            yModifier = 9;
            zModifier = 17;
        }
        var newPosition = new Vector3(transform.position.x, (transform.position.y + yModifier), (transform.position.z - zModifier));
        var oldPosition = playerCamera.transform.position;
        playerCamera.transform.position = Vector3.Lerp(oldPosition, newPosition, 0.2f);
    }
}
