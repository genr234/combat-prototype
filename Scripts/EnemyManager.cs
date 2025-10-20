using Unity.Behavior;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public int health = 50;
    public int damage;
    public float speed = 5f;
    public GameObject target;
    
    private void Start()
    {
        ApplyStats();
    }

    private void FixedUpdate()
    {
        
    }
    
    public void ApplyStats()
    {
        var behaviorAgent = this.GetComponent<BehaviorGraphAgent>();
        if (behaviorAgent != null && behaviorAgent.BlackboardReference != null)
        {
            behaviorAgent.BlackboardReference.SetVariableValue("Speed", speed);
            behaviorAgent.BlackboardReference.SetVariableValue("Damage", damage);
            behaviorAgent.BlackboardReference.SetVariableValue("Target", target);
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
            Destroy(this.gameObject);
        }
    }
    
}
