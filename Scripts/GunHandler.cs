using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GunHandler : MonoBehaviour
{
    [Header("Weapon Configuration")]
    public WeaponConfig currentWeapon;
        
    [Header("Legacy Settings (used if no WeaponConfig)")]
    public float Cooldown = 0.5f;
    public int Damage = 10;
    public float Range = 20f;
        
    [Header("References")]
    public Texture2D cursorTexture;
    public Texture2D lockedCursorTexture;
    public Transform muzzlePoint;
        
    [Header("Components")]
    private TargetingSystem targetingSystem;
    private ShootingEffects shootingEffects;
    private PlayerBuffSystem buffSystem;
        
    [Header("State")]
    private bool onCooldown;
    private int currentAmmo;
    private int totalAmmo;
    private bool isReloading;
    private bool isFiring;
    private bool hasNotifiedHUD = false;
    private bool inputEnabled = true; // Track if input is enabled
    
    // Input actions references
    private InputAction attackAction;
    private InputAction reloadAction;
        
    // Public properties for UI access
    public int CurrentAmmo => currentAmmo;
    public int TotalAmmo => totalAmmo;
    public bool IsReloading => isReloading;
        
    [Header("UI Feedback")]
    public GameObject lockOnReticlePrefab;
    private GameObject lockOnReticleInstance;
    private LockOnReticle lockOnReticleController;
    private SpriteRenderer reticleRenderer;
    private PlayerHudController hudController;

    private void Start()
    {
        InitializeComponents();
        InitializeInput();
        InitializeAmmo();
        SetupCursor();
    }

    private void InitializeComponents()
    {
        // Get or add TargetingSystem
        targetingSystem = GetComponent<TargetingSystem>();
        if (targetingSystem == null)
        {
            targetingSystem = gameObject.AddComponent<TargetingSystem>();
        }

        // Get or add ShootingEffects
        shootingEffects = GetComponent<ShootingEffects>();
        if (shootingEffects == null)
        {
            shootingEffects = gameObject.AddComponent<ShootingEffects>();
        }

        // Find PlayerBuffSystem
        var player = transform.root.gameObject;
        buffSystem = player.GetComponent<PlayerBuffSystem>();

        // Find HUD controller
        hudController = FindFirstObjectByType<PlayerHudController>();

        // Setup muzzle point
        if (muzzlePoint == null)
        {
            muzzlePoint = transform;
        }
        shootingEffects.muzzlePoint = muzzlePoint;

        // Create lock-on reticle if prefab is assigned
        CreateLockOnReticle();
    }

    private void CreateLockOnReticle()
    {
        // If prefab is assigned, instantiate it
        if (lockOnReticlePrefab != null)
        {
            lockOnReticleInstance = Instantiate(lockOnReticlePrefab);
            lockOnReticleController = lockOnReticleInstance.GetComponent<LockOnReticle>();
            if (lockOnReticleController != null)
            {
                lockOnReticleController.targetingSystem = targetingSystem;
                Debug.Log("[GunHandler] Lock-on reticle created from prefab");
            }
        }
        else
        {
            // Create a procedural reticle if no prefab is assigned
            CreateProceduralReticle();
        }
    }

    private void CreateProceduralReticle()
    {
        // Find the UI root or create one
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[GunHandler] No Canvas found in scene, creating procedural reticle without UI");
            return;
        }

        // Create a new GameObject for the reticle
        lockOnReticleInstance = new GameObject("LockOnReticle");
        lockOnReticleInstance.transform.SetParent(canvas.transform, false);

        // Add RectTransform
        var rectTransform = lockOnReticleInstance.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);
        rectTransform.anchoredPosition = Vector2.zero;

        // Add Image component
        var image = lockOnReticleInstance.AddComponent<Image>();
        image.color = Color.red;

        // Add LockOnReticle controller
        lockOnReticleController = lockOnReticleInstance.AddComponent<LockOnReticle>();
        lockOnReticleController.targetingSystem = targetingSystem;

        Debug.Log("[GunHandler] Procedural lock-on reticle created");
    }

    private void InitializeInput()
    {
        InputSystem.actions.Enable();
            
        attackAction = InputSystem.actions.FindAction("Player/Attack");
        if (attackAction != null)
        {
            attackAction.performed += OnAttackPerformed;
            attackAction.canceled += OnAttackCanceled;
        }

        reloadAction = InputSystem.actions.FindAction("Player/Reload");
        if (reloadAction != null)
        {
            reloadAction.performed += ctx => StartReload();
        }
    }

    private void InitializeAmmo()
    {
        if (currentWeapon != null)
        {
            currentAmmo = currentWeapon.magazineSize;
            totalAmmo = currentWeapon.maxAmmo;
            Debug.Log($"[GunHandler] Initialized ammo: {currentAmmo}/{totalAmmo} for {currentWeapon.weaponName}");
                
            // Notify HUD about initial weapon
            if (hudController != null)
            {
                Debug.Log("[GunHandler] HUD controller found in InitializeAmmo, notifying immediately");
                hudController.OnWeaponChanged();
                hasNotifiedHUD = true;
            }
            else
            {
                Debug.LogWarning("[GunHandler] HUD controller not found yet, will try again in Update()");
            }
        }
        else
        {
            Debug.LogWarning("[GunHandler] No weapon config assigned! Please assign a WeaponConfig to currentWeapon.");
        }
    }

    public void SetupCursor()
    {
        if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, new Vector2(cursorTexture.width / 2f, cursorTexture.height / 2f), CursorMode.Auto);
        }
    }

    /// <summary>
    /// Disable shooting input (called when UI is shown)
    /// </summary>
    public void DisableInput()
    {
        inputEnabled = false;
        isFiring = false; // Stop any ongoing firing
        Debug.Log("[GunHandler] Input disabled");
    }

    /// <summary>
    /// Re-enable shooting input (called when UI is hidden)
    /// </summary>
    public void EnableInput()
    {
        inputEnabled = true;
        Debug.Log("[GunHandler] Input enabled");
    }

    private void Update()
        {
            // Try to find HUD controller if not found yet (might not be initialized during InitializeComponents)
            if (!hasNotifiedHUD && hudController == null)
            {
                hudController = FindFirstObjectByType<PlayerHudController>();
                if (hudController != null && currentWeapon != null)
                {
                    Debug.Log("[GunHandler] Found HUD controller in Update, notifying about weapon");
                    hudController.OnWeaponChanged();
                    hasNotifiedHUD = true;
                }
            }


            UpdateCursor();
            UpdateAutoFire();
        }

    private void UpdateCursor()
    {
        if (targetingSystem == null) return;

        // Change cursor based on lock-on state
        if (targetingSystem.IsLockedOn && lockedCursorTexture != null)
        {
            Cursor.SetCursor(lockedCursorTexture, new Vector2(lockedCursorTexture.width / 2f, lockedCursorTexture.height / 2f), CursorMode.Auto);
        }
        else if (cursorTexture != null)
        {
            Cursor.SetCursor(cursorTexture, new Vector2(cursorTexture.width / 2f, cursorTexture.height / 2f), CursorMode.Auto);
        }
    }

    private void UpdateAutoFire()
    {
        if (currentWeapon == null) return;
        if (currentWeapon.fireMode != FireMode.Auto) return;
        if (!isFiring || onCooldown || isReloading) return;

        Shoot();
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        if (!inputEnabled) return; // Block input if disabled
        isFiring = true;

        if (onCooldown || isReloading) return;

        if (currentWeapon != null)
        {
            switch (currentWeapon.fireMode)
            {
                case FireMode.Single:
                    Shoot();
                    break;
                case FireMode.Burst:
                    StartCoroutine(BurstFire());
                    break;
                case FireMode.Auto:
                    // Handled in Update
                    Shoot();
                    break;
            }
        }
        else
        {
            Shoot();
        }
    }

    private void OnAttackCanceled(InputAction.CallbackContext ctx)
    {
        if (!inputEnabled) return; // Block input if disabled
        isFiring = false;
    }

    private IEnumerator BurstFire()
    {
        if (currentWeapon == null) yield break;

        var shotsToFire = currentWeapon.burstCount;
            
        for (var i = 0; i < shotsToFire; i++)
        {
            if (currentAmmo <= 0 && !currentWeapon.infiniteAmmo) break;
                
            Shoot();
                
            if (i < shotsToFire - 1)
            {
                yield return new WaitForSeconds(currentWeapon.burstFireRate);
            }
        }
    }

    private void Shoot()
    {
        // Check ammo
        if (currentWeapon != null && !currentWeapon.infiniteAmmo)
        {
            if (currentAmmo <= 0)
            {
                // Play empty clip sound
                if (currentWeapon.emptyClipSound != null)
                {
                    AudioSource.PlayClipAtPoint(currentWeapon.emptyClipSound, transform.position);
                }
                StartReload();
                return;
            }
            currentAmmo--;
        }

        // Get weapon stats
        var range = currentWeapon != null ? currentWeapon.range : Range;
        var projectileRadius = currentWeapon != null ? currentWeapon.projectileRadius : 0.5f;
        var spread = currentWeapon != null ? currentWeapon.GetSpread(targetingSystem.LockOnStrength) : 0f;

        // Get shoot direction with spread
        var shootDirection = targetingSystem.GetShootDirection();
        shootDirection = targetingSystem.ApplySpread(shootDirection, spread);

        var origin = muzzlePoint != null ? muzzlePoint.position : transform.position + shootDirection * 0.5f;

        Debug.DrawRay(origin, shootDirection * range, Color.red, 1f);

        // Perform raycast
        var didHit = false;
        var hitPoint = origin + shootDirection * range;
        var hitNormal = -shootDirection;
        var hitEnemies = new List<EnemyManager>();

        // Handle penetration
        var maxPenetration = currentWeapon != null ? currentWeapon.penetrationCount + 1 : 1;
        // Add buff system penetration bonus
        if (buffSystem != null)
        {
            maxPenetration += buffSystem.PenetrationBonus;
        }
        var hits = Physics.SphereCastAll(origin, projectileRadius, shootDirection, range);
            
        // Sort by distance
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        var enemiesHit = 0;
        foreach (var hit in hits)
        {
            if (enemiesHit >= maxPenetration) break;

            var enemy = hit.transform.GetComponentInParent<EnemyManager>();
            if (!enemy || hitEnemies.Contains(enemy)) continue;
            hitEnemies.Add(enemy);
            hitPoint = hit.point;
            hitNormal = hit.normal;
            didHit = true;
            enemiesHit++;

            // Calculate damage
            int damage;
            bool isCritical;
            if (currentWeapon)
            {
                damage = currentWeapon.CalculateDamage(out isCritical);
            }
            else
            {
                damage = Damage;
                isCritical = false;
            }

            // Apply buff system modifiers
            if (buffSystem != null)
            {
                damage = buffSystem.CalculateDamage(damage, out var buffCrit);
                isCritical = isCritical || buffCrit;
            }

            // Apply damage
            enemy.TakeDamage(damage);
            
            // Process life steal
            if (buffSystem != null)
            {
                buffSystem.ProcessLifeSteal(damage);
            }

            // Create floating damage number
            if (shootingEffects)
            {
                shootingEffects.CreateFloatingDamageNumber(hit.point + Vector3.up, damage, isCritical);
            }

            // Apply knockback
            if (currentWeapon && currentWeapon.knockbackForce > 0)
            {
                var enemyEffects = enemy.GetComponent<EnemyVisualEffects>();
                if (enemyEffects)
                {
                    enemyEffects.PlayKnockback(shootDirection, currentWeapon.knockbackForce);
                }
            }

            // Hit stop effect
            if (currentWeapon && currentWeapon.hitStopDuration > 0)
            {
                shootingEffects.DoHitStop(currentWeapon.hitStopDuration);
            }

            Debug.Log($"Hit {enemy.name} for {damage} damage{(isCritical ? " (CRITICAL!)" : "")}");
        }

        // Handle explosion (from weapon or buff)
        var explosionRadius = currentWeapon != null ? currentWeapon.explosionRadius : 0f;
        if (buffSystem != null)
        {
            explosionRadius += buffSystem.ExplosionRadiusBonus;
        }
        if (explosionRadius > 0 && didHit)
        {
            HandleExplosion(hitPoint, explosionRadius);
        }

        // Play visual effects
        if (shootingEffects && currentWeapon)
        {
            shootingEffects.PlayAllShootEffects(currentWeapon, hitPoint, hitNormal, didHit);
        }

        // Start cooldown
        var cooldown = currentWeapon ? currentWeapon.cooldown : Cooldown;
        // Apply fire rate buff (higher multiplier = faster fire rate = shorter cooldown)
        if (buffSystem != null && buffSystem.FireRateMultiplier > 0)
        {
            cooldown /= buffSystem.FireRateMultiplier;
        }
        onCooldown = true;
        Invoke(nameof(ResetCooldown), cooldown);
    }

    private void HandleExplosion(Vector3 center, float radius)
    {
        var explosionDamage = currentWeapon != null ? currentWeapon.explosionDamage : 20f;
        
        // Create explosion visual effect
        if (shootingEffects != null)
        {
            shootingEffects.CreateExplosionEffect(center, radius, currentWeapon);
        }

        // Find and damage enemies in explosion radius
        var enemiesInRadius = targetingSystem.GetEnemiesInRadius(center, radius);
            
        foreach (var enemy in enemiesInRadius)
        {
            var distance = Vector3.Distance(center, enemy.transform.position);
            var damageMultiplier = 1f - (distance / radius);
            var finalExplosionDamage = Mathf.RoundToInt(explosionDamage * damageMultiplier);

            if (finalExplosionDamage <= 0) continue;
            enemy.TakeDamage(finalExplosionDamage);
            if (shootingEffects != null)
            {
                shootingEffects.CreateFloatingDamageNumber(enemy.transform.position + Vector3.up, finalExplosionDamage, false);
            }

            // Knockback from explosion center
            var knockbackDir = (enemy.transform.position - center).normalized;
            var enemyEffects = enemy.GetComponent<EnemyVisualEffects>();
            if (enemyEffects)
            {
                var knockbackForce = currentWeapon != null ? currentWeapon.knockbackForce * 2f : 10f;
                enemyEffects.PlayKnockback(knockbackDir, knockbackForce);
            }
        }
    }

    private void StartReload()
    {
        if (isReloading) return;
        if (!currentWeapon) return;
        if (currentWeapon.infiniteAmmo) return;
        if (currentAmmo >= currentWeapon.magazineSize) return;
        if (totalAmmo <= 0) return;

        StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;

        if (currentWeapon.reloadSound)
        {
            AudioSource.PlayClipAtPoint(currentWeapon.reloadSound, transform.position);
        }

        // Show reload UI
        if (hudController)
        {
            hudController.ShowReloadIndicator();
        }

        // Animate reload progress
        var elapsed = 0f;
        var reloadTime = currentWeapon.reloadTime;
            
        while (elapsed < reloadTime)
        {
            elapsed += Time.deltaTime;
            var progress = elapsed / reloadTime;
                
            if (hudController != null)
            {
                hudController.UpdateReloadProgress(progress);
            }
                
            yield return null;
        }

        // Complete reload
        var ammoNeeded = currentWeapon.magazineSize - currentAmmo;
        var ammoToAdd = Mathf.Min(ammoNeeded, totalAmmo);
            
        currentAmmo += ammoToAdd;
        totalAmmo -= ammoToAdd;

        // Hide reload UI
        if (hudController != null)
        {
            hudController.HideReloadIndicator();
        }

        isReloading = false;
        Debug.Log($"Reload complete! Ammo: {currentAmmo}/{currentWeapon.magazineSize}");
    }

    private void ResetCooldown()
    {
        onCooldown = false;
    }

    /// <summary>
    /// Switch to a different weapon
    /// </summary>
    public void SwitchWeapon(WeaponConfig newWeapon)
    {
        currentWeapon = newWeapon;
        InitializeAmmo();
            
        // Notify HUD controller about weapon change
        if (hudController != null)
        {
            hudController.OnWeaponChanged();
        }
            
        Debug.Log($"Switched to {newWeapon.weaponName}");
    }

    private void OnDrawGizmos()
    {
        if (targetingSystem == null) return;

        var shootDirection = targetingSystem.GetShootDirection();
        var range = currentWeapon != null ? currentWeapon.range : Range;
        var origin = muzzlePoint != null ? muzzlePoint.position : transform.position + shootDirection * 0.5f;

        // Draw shoot direction
        Gizmos.color = targetingSystem.IsLockedOn ? Color.green : Color.red;
        Gizmos.DrawLine(origin, origin + shootDirection * range);

        // Draw spread cone
        if (currentWeapon != null && currentWeapon.maxSpread > 0)
        {
            var spread = currentWeapon.GetSpread(targetingSystem.LockOnStrength);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                
            var leftRot = Quaternion.AngleAxis(-spread, Vector3.up);
            var rightRot = Quaternion.AngleAxis(spread, Vector3.up);
                
            Gizmos.DrawLine(origin, origin + leftRot * shootDirection * range);
            Gizmos.DrawLine(origin, origin + rightRot * shootDirection * range);
        }
    }

    private void OnDestroy()
    {
        // Cleanup input bindings
        var attackAction = InputSystem.actions?.FindAction("Player/Attack");
        if (attackAction != null)
        {
            attackAction.performed -= OnAttackPerformed;
            attackAction.canceled -= OnAttackCanceled;
        }
    }
}