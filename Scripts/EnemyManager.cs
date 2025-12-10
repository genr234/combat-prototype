using Unity.Behavior;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class EnemyManager : MonoBehaviour
{
    public int health = 50;
    public int damage;
    public float speed = 5f;
    public GameObject target;
    
    private void Start()
    {
        // Defer blackboard initialization to next frame to ensure BehaviorGraphAgent is ready
        Invoke(nameof(ApplyStats), 0.01f);
    }
    
    public void ApplyStats()
    {
        var behaviorAgent = this.GetComponent<BehaviorGraphAgent>();
        Debug.Log($"[EnemyManager] ApplyStats called for {gameObject.name}, BehaviorGraphAgent: {(behaviorAgent != null ? "FOUND" : "NOT FOUND")}");
        
        if (behaviorAgent != null)
        {
            Debug.Log($"[EnemyManager] BehaviorGraphAgent found, BlackboardReference: {(behaviorAgent.BlackboardReference != null ? "FOUND" : "NULL")}");
            
            if (behaviorAgent.BlackboardReference != null)
            {
                try
                {
                    behaviorAgent.BlackboardReference.SetVariableValue("Speed", speed);
                    Debug.Log($"[EnemyManager] Set Speed to {speed}");
                    
                    behaviorAgent.BlackboardReference.SetVariableValue("Damage", damage);
                    Debug.Log($"[EnemyManager] Set Damage to {damage}");
                    
                    behaviorAgent.BlackboardReference.SetVariableValue("Target", target);
                    Debug.Log($"[EnemyManager] Set Target to {target}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[EnemyManager] Error setting blackboard variables: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[EnemyManager] BlackboardReference is null for {gameObject.name}. BehaviorGraphAgent may not be initialized yet.");
            }
        }
        else
        {
            Debug.LogWarning($"[EnemyManager] No BehaviorGraphAgent found on {gameObject.name}");
        }
    }
    
    [Preserve]
    public void TakeDamage(int inflictedDamage)
    {
        Debug.Log($"[EnemyManager] {gameObject.name} TakeDamage called with {inflictedDamage} damage. Current health: {health}");
        
        health -= inflictedDamage;
        Debug.Log($"[EnemyManager] {gameObject.name} took {inflictedDamage} damage. Health now: {health}");
        
        // Trigger visual effects
        var visualEffects = GetComponent<EnemyVisualEffects>();
        if (visualEffects != null)
        {
            visualEffects.PlayDamageEffect();
        }
        
        // Check if enemy should die
        Debug.Log($"[EnemyManager] Checking death condition: health ({health}) <= 0? {health <= 0}");
        if (health <= 0)
        {
            Debug.Log($"[EnemyManager] Death condition TRUE, calling Die()");
            Die();
        }
        else
        {
            Debug.Log($"[EnemyManager] Death condition FALSE, enemy survives");
        }
    }
    
    [Preserve]
    public void Die()
    {
        Debug.Log($"[EnemyManager] Die() called for {gameObject.name}! Health: {health}");
        
        try
        {
            // Immediately disable the game object to stop all behavior
            gameObject.SetActive(false);
            Debug.Log($"[EnemyManager] Disabled GameObject immediately");
            
            // Disable behavior and movement before destroying
            var behaviorAgent = GetComponent<BehaviorGraphAgent>();
            if (behaviorAgent != null)
            {
                behaviorAgent.enabled = false;
                Debug.Log($"[EnemyManager] Disabled BehaviorGraphAgent");
            }
            
            var navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = false;
                Debug.Log($"[EnemyManager] Disabled NavMeshAgent");
            }
            
            // Destroy the game object
            Debug.Log($"[EnemyManager] Calling Destroy on {gameObject.name}");
            Destroy(this.gameObject);
            Debug.Log($"[EnemyManager] Destroy called successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[EnemyManager] Exception in Die(): {ex.Message}\n{ex.StackTrace}");
        }
    }
    
}
