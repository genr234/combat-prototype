using UnityEngine;
using UnityEngine.InputSystem;

namespace DefaultNamespace
{
    public class GunHandler : MonoBehaviour
    {
        public float Cooldown;
        public int Damage;
        public float Range;
        
        public Texture2D cursorTexture;
        
        private bool onCooldown;
        private void Start()
        {
            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
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
            
            // Get the direction towards the cursor
            Vector3 shootDirection = GetShootDirection();
            
            Vector3 origin = transform.position + shootDirection * 1f;
            Debug.DrawRay(origin, shootDirection * Range, Color.red, 2f); 
            
            if (Physics.SphereCast(origin, 0.5f, shootDirection, out var hit, Range))
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

        private Vector3 GetShootDirection()
        {
            // Get mouse position in screen space
            Vector2 mousePos = Mouse.current.position.ReadValue();
            
            // Create a ray from the camera through the mouse position
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            
            // Create a plane at the gun's height
            Plane groundPlane = new Plane(Vector3.up, transform.position);
            
            // Raycast to find where the mouse points in the world
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                Vector3 direction = (worldPoint - transform.position).normalized;
                return direction;
            }
            
            // Fallback to forward direction if raycast fails
            return transform.forward;
        }

        private void ResetCooldown()
        {
            Debug.Log("Cooldown reset");
            onCooldown = false;
        }
        
        private void OnDrawGizmos()
        {
            Vector3 shootDirection = GetShootDirection();
            Vector3 origin = transform.position + shootDirection * 1f;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, origin + shootDirection * Range);
        }
    }
}