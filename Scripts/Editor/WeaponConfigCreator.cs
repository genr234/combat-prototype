using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class WeaponConfigCreator : EditorWindow
    {
        [MenuItem("Tools/Create Default Weapons")]
        public static void CreateDefaultWeapons()
        {
            CreateBlasterStandard();
            CreatePrecisionRifle();
            CreateShotgun();
        
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        
            Debug.Log("Created 3 default weapon configurations in Assets/Weapons/");
        }

        private static void CreateBlasterStandard()
        {
            var weapon = ScriptableObject.CreateInstance<WeaponConfig>();
        
            // Base Stats
            weapon.weaponName = "Blaster Standard";
            weapon.damage = 15;
            weapon.cooldown = 0.3f;
            weapon.range = 25f;
            weapon.projectileRadius = 0.4f;
        
            // Fire Behavior
            weapon.fireMode = FireMode.Single;
            weapon.burstCount = 1;
            weapon.burstFireRate = 0.1f;
            weapon.maxSpread = 8f;
            weapon.spreadReductionWhileLocked = 0.7f;
        
            // Ammo
            weapon.magazineSize = 12;
            weapon.maxAmmo = 120;
            weapon.reloadTime = 1.5f;
            weapon.infiniteAmmo = true;
        
            // Special Properties
            weapon.penetrationCount = 0;
            weapon.hasBounce = false;
            weapon.explosionRadius = 0f;
        
            // Visual
            weapon.weaponColor = new Color(1f, 0.8f, 0.2f); // Yellow/Orange
            weapon.muzzleFlashColor = new Color(1f, 0.9f, 0.3f);
        
            // Feedback
            weapon.screenShakeForce = 0.08f;
            weapon.screenShakeDuration = 0.08f;
            weapon.recoilForce = 0.05f;
            weapon.recoilDuration = 0.1f;
        
            // Fun Parameters
            weapon.projectileSpeed = 60f;
            weapon.criticalChance = 0.1f;
            weapon.criticalMultiplier = 2f;
            weapon.knockbackForce = 3f;
            weapon.hitStopDuration = 0f;
        
            EnsureDirectoryExists("Assets/Weapons");
            AssetDatabase.CreateAsset(weapon, "Assets/Weapons/BlasterStandard.asset");
        }

        private static void CreatePrecisionRifle()
        {
            var weapon = ScriptableObject.CreateInstance<WeaponConfig>();
        
            // Base Stats
            weapon.weaponName = "Precision Rifle";
            weapon.damage = 12;
            weapon.cooldown = 0.15f;
            weapon.range = 35f;
            weapon.projectileRadius = 0.2f;
        
            // Fire Behavior
            weapon.fireMode = FireMode.Auto;
            weapon.burstCount = 1;
            weapon.burstFireRate = 0.1f;
            weapon.maxSpread = 3f;
            weapon.spreadReductionWhileLocked = 0.9f;
        
            // Ammo
            weapon.magazineSize = 30;
            weapon.maxAmmo = 300;
            weapon.reloadTime = 2f;
            weapon.infiniteAmmo = true;
        
            // Special Properties - Penetration!
            weapon.penetrationCount = 2;
            weapon.hasBounce = false;
            weapon.explosionRadius = 0f;
        
            // Visual
            weapon.weaponColor = new Color(0.2f, 0.8f, 1f); // Cyan
            weapon.muzzleFlashColor = new Color(0.4f, 0.9f, 1f);
        
            // Feedback
            weapon.screenShakeForce = 0.04f;
            weapon.screenShakeDuration = 0.05f;
            weapon.recoilForce = 0.03f;
            weapon.recoilDuration = 0.08f;
        
            // Fun Parameters
            weapon.projectileSpeed = 100f;
            weapon.criticalChance = 0.15f;
            weapon.criticalMultiplier = 2.5f;
            weapon.knockbackForce = 2f;
            weapon.hitStopDuration = 0f;
        
            EnsureDirectoryExists("Assets/Weapons");
            AssetDatabase.CreateAsset(weapon, "Assets/Weapons/PrecisionRifle.asset");
        }

        private static void CreateShotgun()
        {
            var weapon = ScriptableObject.CreateInstance<WeaponConfig>();
        
            // Base Stats
            weapon.weaponName = "Devastator Shotgun";
            weapon.damage = 35;
            weapon.cooldown = 0.8f;
            weapon.range = 12f;
            weapon.projectileRadius = 0.8f;
        
            // Fire Behavior
            weapon.fireMode = FireMode.Single;
            weapon.burstCount = 1;
            weapon.burstFireRate = 0.1f;
            weapon.maxSpread = 20f;
            weapon.spreadReductionWhileLocked = 0.4f;
        
            // Ammo
            weapon.magazineSize = 6;
            weapon.maxAmmo = 60;
            weapon.reloadTime = 2.5f;
            weapon.infiniteAmmo = true;
        
            // Special Properties - Explosion!
            weapon.penetrationCount = 0;
            weapon.hasBounce = false;
            weapon.explosionRadius = 3f;
            weapon.explosionDamage = 20f;
        
            // Visual
            weapon.weaponColor = new Color(1f, 0.3f, 0.1f); // Orange/Red
            weapon.muzzleFlashColor = new Color(1f, 0.5f, 0.2f);
        
            // Feedback - Heavy!
            weapon.screenShakeForce = 0.2f;
            weapon.screenShakeDuration = 0.15f;
            weapon.recoilForce = 0.15f;
            weapon.recoilDuration = 0.2f;
        
            // Fun Parameters
            weapon.projectileSpeed = 40f;
            weapon.criticalChance = 0.05f;
            weapon.criticalMultiplier = 1.5f;
            weapon.knockbackForce = 8f;
            weapon.hitStopDuration = 0.03f; // Brief hit stop for impact!
        
            EnsureDirectoryExists("Assets/Weapons");
            AssetDatabase.CreateAsset(weapon, "Assets/Weapons/DevastatorShotgun.asset");
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var folders = path.Split('/');
                var currentPath = folders[0];
            
                for (var i = 1; i < folders.Length; i++)
                {
                    var newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }
        }
    }
}

