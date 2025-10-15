using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    public Camera playerCamera;
    private void Start()
    {
        
    }

    private void FixedUpdate()
    {
        var newPosition = new Vector3(transform.position.x, 4, (transform.position.z - 7));
        var oldPosition = playerCamera.transform.position;
        playerCamera.transform.position = Vector3.Lerp(oldPosition, newPosition, 0.2f);
    }
}
