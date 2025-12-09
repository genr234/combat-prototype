using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    private Rigidbody _rigidbody;
    public float speed = 5f;
    public CameraController cameraController;
    
    [Header("Buff Modifiers")]
    [HideInInspector] public float speedMultiplier = 1f;
    
    [Header("Dash")]
    public float dashSpeed = 10f;
    public float dashDuration = 0.1f;
    public float dashCooldown = 4f;
    private float _dashCooldownTimer = 0f;
    private bool _isDashing = false;
    
    public float DashCooldownProgress => _dashCooldownTimer / dashCooldown;
    public bool IsDashing => _isDashing;
    public bool CanDash => _dashCooldownTimer <= 0 && !_isDashing;
    
    private void Start() 
    {
        _rigidbody = GetComponent<Rigidbody>();
        Physics.gravity = new Vector3(0, -20f, 0);
        
        // Setup dash input callback
        InputSystem.actions.FindAction("Player/Dash").performed += OnDashPerformed;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from dash input to prevent memory leaks
        InputSystem.actions.FindAction("Player/Dash").performed -= OnDashPerformed;
    }
    
    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        // Execute dash if it's available
        if (_dashCooldownTimer <= 0 && !_isDashing)
        {
            StartCoroutine(PerformDash());
        }
    }

    
    private void FixedUpdate()
    {
        // Update dash cooldown
        if (_dashCooldownTimer > 0)
            _dashCooldownTimer -= Time.fixedDeltaTime;
        
        var inputVector = InputSystem.actions.FindAction("Player/Move").ReadValue<Vector2>();
        var movement = new Vector3(inputVector.x, 0, inputVector.y) * (speed * speedMultiplier * Time.fixedDeltaTime);
        var rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
        movement = rotation * movement;
        _rigidbody.MovePosition(_rigidbody.position + movement);
        if (movement != Vector3.zero)
        {
            var targetRotation = Quaternion.LookRotation(movement);
            _rigidbody.rotation = Quaternion.Slerp(_rigidbody.rotation, targetRotation, 0.2f);
        }
        cameraController.zoomedOut = InputSystem.actions.FindAction("Player/Zoom Out").IsPressed();
    }
    
    private IEnumerator PerformDash()
    {
        _isDashing = true;
        _dashCooldownTimer = dashCooldown;
        
        // Get current forward direction (direction player is facing)
        Vector3 dashDirection = _rigidbody.rotation * Vector3.forward;
        
        // Alternative: dash in movement direction if available
        var inputVector = InputSystem.actions.FindAction("Player/Move").ReadValue<Vector2>();
        if (inputVector != Vector2.zero)
        {
            var movementDirection = new Vector3(inputVector.x, 0, inputVector.y);
            var rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
            dashDirection = rotation * movementDirection.normalized;
        }
        
        float elapsedTime = 0f;
        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.fixedDeltaTime;
            _rigidbody.linearVelocity = dashDirection * dashSpeed;
            yield return new WaitForFixedUpdate();
        }
        
        _isDashing = false;
    }
}
