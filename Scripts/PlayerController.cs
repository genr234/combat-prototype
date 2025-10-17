using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    private Rigidbody _rigidbody;
    public float speed = 5f;
    public CameraController cameraController;

    public int health = 100;
    

    private void Start() 
    {
        _rigidbody = GetComponent<Rigidbody>();
        Physics.gravity = new Vector3(0, -20f, 0);
    }

    private void FixedUpdate()
    {
        var inputVector = InputSystem.actions.FindAction("Player/Move").ReadValue<Vector2>();
        var movement = new Vector3(inputVector.x, 0, inputVector.y) * (speed * Time.fixedDeltaTime);
        var rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
        movement = rotation * movement;
        _rigidbody.MovePosition(_rigidbody.position + movement);
        cameraController.zoomedOut = InputSystem.actions.FindAction("Player/Zoom Out").IsPressed();
    }
    
}
