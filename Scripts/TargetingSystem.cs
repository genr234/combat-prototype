using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TargetingSystem : MonoBehaviour
{
    [Header("Lock-On Settings")]
    public float lockOnRange = 15f;
    public float lockOnFOV = 45f;
    public float lockOnSpeed = 10f;
    public LayerMask enemyLayer = -1;

    [Header("Targeting State")]
    [SerializeField] private Transform currentTarget;
    [SerializeField] private float lockOnStrength;
    [SerializeField] private bool isLockedOn;

    [Header("Visual Feedback")]
    public GameObject lockOnIndicatorPrefab;
    private GameObject lockOnIndicatorInstance;
    private LineRenderer lockOnLine;

    // Properties
    public Transform CurrentTarget => currentTarget;
    public bool IsLockedOn => isLockedOn;
    public float LockOnStrength => lockOnStrength;

    private List<EnemyManager> enemiesInRange = new List<EnemyManager>();
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        CreateLockOnIndicator();
    }

    private void Update()
    {
        UpdateTargeting();
        UpdateLockOnIndicator();
    }

    private void CreateLockOnIndicator()
    {
        // Create a simple lock-on indicator using LineRenderer
        lockOnIndicatorInstance = new GameObject("LockOnIndicator");
        lockOnIndicatorInstance.transform.SetParent(transform);
            
        lockOnLine = lockOnIndicatorInstance.AddComponent<LineRenderer>();
        lockOnLine.startWidth = 0.05f;
        lockOnLine.endWidth = 0.02f;
        lockOnLine.positionCount = 2;
        lockOnLine.material = new Material(Shader.Find("Sprites/Default"));
        lockOnLine.startColor = Color.red;
        lockOnLine.endColor = Color.yellow;
        lockOnLine.enabled = false;
    }

    private void UpdateTargeting()
    {
        // Find all enemies in range
        var colliders = Physics.OverlapSphere(transform.position, lockOnRange, enemyLayer);
        enemiesInRange.Clear();

        foreach (var col in colliders)
        {
            var enemy = col.GetComponentInParent<EnemyManager>();
            if (enemy && !enemiesInRange.Contains(enemy))
            {
                enemiesInRange.Add(enemy);
            }
        }

        // Find best target
        Transform bestTarget = null;
        var bestScore = float.MinValue;

        foreach (var enemy in enemiesInRange)
        {
            if (!enemy) continue;

            var dirToEnemy = (enemy.transform.position - transform.position).normalized;
            var aimDirection = GetAimDirection();
                
            var angle = Vector3.Angle(aimDirection, dirToEnemy);
            var distance = Vector3.Distance(transform.position, enemy.transform.position);

            // Only consider enemies within FOV
            if (angle > lockOnFOV) continue;

            // Score based on angle (lower is better) and distance (closer is better)
            var angleScore = 1f - (angle / lockOnFOV);
            var distanceScore = 1f - (distance / lockOnRange);
            var score = angleScore * 0.7f + distanceScore * 0.3f;

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = enemy.transform;
            }
        }

        // Update current target
        if (bestTarget)
        {
            currentTarget = bestTarget;
            isLockedOn = true;

            // Calculate lock-on strength based on how centered the target is
            var dirToTarget = (currentTarget.position - transform.position).normalized;
            var aimDir = GetAimDirection();
            var angle = Vector3.Angle(aimDir, dirToTarget);
            lockOnStrength = Mathf.Clamp01(1f - (angle / lockOnFOV));
        }
        else
        {
            currentTarget = null;
            isLockedOn = false;
            lockOnStrength = 0f;
        }
    }

    private void UpdateLockOnIndicator()
    {
        if (!lockOnLine) return;

        if (isLockedOn && currentTarget)
        {
            lockOnLine.enabled = true;
            lockOnLine.SetPosition(0, transform.position + Vector3.up * 0.5f);
            lockOnLine.SetPosition(1, currentTarget.position + Vector3.up * 0.5f);

            // Color based on lock strength
            var lineColor = Color.Lerp(Color.red, Color.green, lockOnStrength);
            lockOnLine.startColor = lineColor;
            lockOnLine.endColor = lineColor * 0.7f;
                
            // Width based on lock strength
            lockOnLine.startWidth = Mathf.Lerp(0.02f, 0.08f, lockOnStrength);
            lockOnLine.endWidth = Mathf.Lerp(0.01f, 0.04f, lockOnStrength);
        }
        else
        {
            lockOnLine.enabled = false;
        }
    }

    /// <summary>
    /// Get the direction the player is aiming (towards mouse cursor)
    /// </summary>
    public Vector3 GetAimDirection()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return transform.forward;
            
        Vector3 mousePos = Mouse.current.position.ReadValue();
        var ray = mainCamera.ScreenPointToRay(mousePos);
        var groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out var distance))
        {
            var worldPoint = ray.GetPoint(distance);
            return (worldPoint - transform.position).normalized;
        }

        return transform.forward;
    }

    /// <summary>
    /// Get the optimal shoot direction (towards locked target or aim direction)
    /// </summary>
    public Vector3 GetShootDirection()
    {
        if (isLockedOn && currentTarget != null)
        {
            var aimDir = GetAimDirection();
            var targetDir = (currentTarget.position - transform.position).normalized;
                
            // Blend between aim direction and target direction based on lock strength
            return Vector3.Lerp(aimDir, targetDir, lockOnStrength * 0.8f).normalized;
        }

        return GetAimDirection();
    }

    /// <summary>
    /// Apply spread to a direction
    /// </summary>
    public Vector3 ApplySpread(Vector3 direction, float spreadAngle)
    {
        if (spreadAngle <= 0) return direction;

        var actualSpread = spreadAngle * (1f - lockOnStrength * 0.7f);
            
        var spreadRotation = Quaternion.Euler(
            Random.Range(-actualSpread, actualSpread),
            Random.Range(-actualSpread, actualSpread),
            0
        );

        return spreadRotation * direction;
    }

    /// <summary>
    /// Get all enemies within explosion radius
    /// </summary>
    public List<EnemyManager> GetEnemiesInRadius(Vector3 center, float radius)
    {
        var result = new List<EnemyManager>();
        var colliders = Physics.OverlapSphere(center, radius, enemyLayer);

        foreach (var col in colliders)
        {
            var enemy = col.GetComponentInParent<EnemyManager>();
            if (enemy != null && !result.Contains(enemy))
            {
                result.Add(enemy);
            }
        }

        return result;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw lock-on range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockOnRange);

        // Draw FOV cone
        Gizmos.color = Color.cyan;
        var forward = Application.isPlaying ? GetAimDirection() : transform.forward;
            
        var leftRayRotation = Quaternion.AngleAxis(-lockOnFOV, Vector3.up);
        var rightRayRotation = Quaternion.AngleAxis(lockOnFOV, Vector3.up);
        var leftRayDirection = leftRayRotation * forward;
        var rightRayDirection = rightRayRotation * forward;

        Gizmos.DrawRay(transform.position, leftRayDirection * lockOnRange);
        Gizmos.DrawRay(transform.position, rightRayDirection * lockOnRange);

        // Draw line to current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}