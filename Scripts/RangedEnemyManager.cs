using Unity.Behavior;
using UnityEngine;

public class RangedEnemyManager : MonoBehaviour
{
    [Header("Stats")]
    public int health = 30;
    public int damage = 15;
    public float speed = 3f;
    
    [Header("Combat Settings")]
    public GameObject projectilePrefab;
    public float attackRange = 15f;
    public float minDistance = 8f; // Keep distance from player
    public float projectileSpeed = 20f;
    public float fireCooldown = 2f;
    
    [Header("References")]
    public GameObject target;
    public Transform shootPoint;
    
    private void Start()
    {
        // Find player if not assigned
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerManager>()?.gameObject;
            }
            target = player;
        }
        
        // Create shoot point if not assigned
        if (shootPoint == null)
        {
            var shootPointObj = new GameObject("ShootPoint");
            shootPointObj.transform.SetParent(transform);
            shootPointObj.transform.localPosition = new Vector3(0, 1.5f, 0.5f);
            shootPoint = shootPointObj.transform;
        }
        
        // Create default projectile if not assigned
        if (projectilePrefab == null)
        {
            CreateDefaultProjectile();
        }
        
        ApplyStats();
    }
    
    private void CreateDefaultProjectile()
    {
        // Create a simple sphere projectile
        projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectilePrefab.name = "EnemyProjectile";
        projectilePrefab.transform.localScale = Vector3.one * 0.3f;
        
        // Add rigidbody
        var rb = projectilePrefab.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Add projectile script
        var projectileScript = projectilePrefab.AddComponent<EnemyProjectile>();
        projectileScript.speed = projectileSpeed;
        projectileScript.damage = damage;
        projectileScript.projectileColor = Color.red;
        
        // Make it a prefab by deactivating it
        projectilePrefab.SetActive(false);
        DontDestroyOnLoad(projectilePrefab);
    }
    
    public void ApplyStats()
    {
        var behaviorAgent = GetComponent<BehaviorGraphAgent>();
        if (behaviorAgent != null && behaviorAgent.BlackboardReference != null)
        {
            behaviorAgent.BlackboardReference.SetVariableValue("Speed", speed);
            behaviorAgent.BlackboardReference.SetVariableValue("Damage", damage);
            behaviorAgent.BlackboardReference.SetVariableValue("Target", target);
            behaviorAgent.BlackboardReference.SetVariableValue("Projectile", projectilePrefab);
            behaviorAgent.BlackboardReference.SetVariableValue("ProjectileSpeed", projectileSpeed);
            behaviorAgent.BlackboardReference.SetVariableValue("FireCooldown", fireCooldown);
            behaviorAgent.BlackboardReference.SetVariableValue("AttackRange", attackRange);
            behaviorAgent.BlackboardReference.SetVariableValue("MinDistance", minDistance);
        }
    }
    
    public void TakeDamage(int inflictedDamage)
    {
        health -= inflictedDamage;
        
        // Trigger visual effects
        var visualEffects = GetComponent<EnemyVisualEffects>();
        if (visualEffects != null)
        {
            visualEffects.PlayDamageEffect();
        }
        
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
    
    // Helper method to check if in attack range
    public bool IsInAttackRange()
            {
        if (target == null) return false;
        float distance = Vector3.Distance(transform.position, target.transform.position);
        return distance <= attackRange && distance >= minDistance;
    }
    
    // Helper to check if too close
    public bool IsTooClose()
    {
        if (target == null) return false;
        float distance = Vector3.Distance(transform.position, target.transform.position);
        return distance < minDistance;
    }
}

