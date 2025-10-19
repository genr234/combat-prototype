using UnityEngine;
using UnityEngine.InputSystem;

namespace DefaultNamespace
{
    public class GunHandler : MonoBehaviour
    {
        public float Cooldown;
        public int Damage;
        public float Range;
        
        private bool onCooldown;
        private void Start()
        {
            InputSystem.actions.Enable();
            InputSystem.actions.FindAction("Player/Attack").performed += ctx =>
            {
                Debug.Log("Attack performed");
                if (onCooldown) return;
                Shoot();
                onCooldown = true;
                Invoke(nameof(ResetCooldown), Cooldown);
            };
        }
        
        private void FixedUpdate()
        {
            
        }

        private void Shoot()
        {
            print("shot");
            Vector3 origin = transform.position + transform.forward * 1f;
            Debug.DrawRay(origin, -transform.forward * Range, Color.red, 2f); 
            if (Physics.SphereCast(origin, 0.5f, -transform.forward, out var hit, Range))
            {
                Debug.Log("Hit: " + hit.collider.gameObject.name);
                var enemy = hit.transform.GetComponentInParent<EnemyManager>();
                if (enemy != null)
                {
                    Debug.Log("hit " + enemy.name);
                    enemy.TakeDamage(Damage);
                }
            }
            else
            {
                Debug.Log("No hit detected");
            }
        }

        private void ResetCooldown()
        {
            Debug.Log("Cooldown reset");
            onCooldown = false;
        }
        
        private void OnDrawGizmos()
        {
            Vector3 origin = transform.position + transform.forward * 1f;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin - transform.forward * Range);
        }
    }
}