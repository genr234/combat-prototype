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
            InputSystem.actions.FindAction("Player/Attack").performed += ctx =>
            {
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
            print("shoot");
            RaycastHit hit;
            if (!Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, Range)) return;
            var enemy = hit.transform.GetComponent<EnemyManager>();
            if (enemy != null)
            {
                enemy.TakeDamage(Damage);
            }
        }

        private void ResetCooldown()
        {
            onCooldown = false;
        }
    }
}