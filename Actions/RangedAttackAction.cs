using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Action = Unity.Behavior.Action;

namespace Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Ranged Attack", story: "Shoots [Projectile] at [Target]", category: "Action", id: "a7d91c5f63204e2b8f34567890abcdef")]
    public class RangedAttackAction : Action
    {
        [FormerlySerializedAs("Target")] [SerializeReference] public BlackboardVariable<GameObject> target;
        [FormerlySerializedAs("Projectile")] [SerializeReference] public BlackboardVariable<GameObject> projectile;
        [FormerlySerializedAs("Damage")] [SerializeReference] public BlackboardVariable<int> damage;
        [FormerlySerializedAs("ProjectileSpeed")] [SerializeReference] public BlackboardVariable<float> projectileSpeed = new BlackboardVariable<float>(20f);
        [FormerlySerializedAs("FireCooldown")] [SerializeReference] public BlackboardVariable<float> fireCooldown = new BlackboardVariable<float>(2f);
        
        private float lastFireTime;
        private Transform shootPoint;

        protected override Status OnStart()
        {
            // Find or create shoot point
            if (shootPoint == null)
            {
                var shootPointObj = GameObject.Find("ShootPoint");
                if (shootPointObj != null && shootPointObj.transform.IsChildOf(GameObject.transform))
                {
                    shootPoint = shootPointObj.transform;
                }
                else
                {
                    // Create shoot point if it doesn't exist
                    var newShootPoint = new GameObject("ShootPoint");
                    newShootPoint.transform.SetParent(GameObject.transform);
                    newShootPoint.transform.localPosition = new Vector3(0, 1.5f, 0.5f); // Positioned in front and up
                    shootPoint = newShootPoint.transform;
                }
            }
            
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            // Check cooldown
            if (Time.time - lastFireTime < fireCooldown.Value)
            {
                return Status.Running;
            }
            
            // Validate target
            if (target.Value == null)
            {
                return Status.Failure;
            }
            
            // Check if projectile prefab is set
            if (projectile.Value == null)
            {
                Debug.LogWarning("Ranged Attack: No projectile prefab assigned!");
                return Status.Failure;
            }
            
            // Calculate direction to target
            var directionToTarget = (target.Value.transform.position - shootPoint.position).normalized;
            
            // Instantiate projectile
            var projectileInstance = Object.Instantiate(
                projectile.Value, 
                shootPoint.position, 
                Quaternion.LookRotation(directionToTarget)
            );
            
            // Setup projectile
            var projectileScript = projectileInstance.GetComponent<EnemyProjectile>();
            if (projectileScript != null)
            {
                projectileScript.damage = damage.Value;
                projectileScript.speed = projectileSpeed.Value;
                projectileScript.Launch(directionToTarget);
            }
            else
            {
                // If no EnemyProjectile script, just give it velocity
                var rb = projectileInstance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = directionToTarget * projectileSpeed.Value;
                }
            }
            
            lastFireTime = Time.time;
            
            return Status.Success;
        }

        protected override void OnEnd()
        {
        }
    }
}

