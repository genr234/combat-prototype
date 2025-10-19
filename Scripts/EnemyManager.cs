using Unity.Behavior;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public int health = 50;
    public int damage;
    public float speed = 5f;
    
    private void Start()
    {
        this.GetComponent<BehaviorGraphAgent>().BlackboardReference.SetVariableValue("Speed", speed);
        this.GetComponent<BehaviorGraphAgent>().BlackboardReference.SetVariableValue("Damage", damage);
        
    }

    private void FixedUpdate()
    {
        
    }
    
    public void TakeDamage(int inflictedDamage)
    {
        health -= inflictedDamage;
        if (health <= 0)
        {
            Destroy(this.gameObject);
        }
    }
    
}
