using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody _rigidbody;
    public float speed = 5f;
    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        var inputVector = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();
        var movement = new Vector3(inputVector.x, 0, inputVector.y) * (speed * Time.fixedDeltaTime);
        var rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
        movement = rotation * movement;
        _rigidbody.MovePosition(_rigidbody.position + movement);
    }
    
}
