using UnityEngine;
using UnityEngine.UIElements;

public class PlayerHudController : MonoBehaviour
{
    [Header("References")]
    public UIDocument uiDocument;
    public PlayerManager playerManager;
    public GunHandler gunHandler;
    public PlayerController playerController;

    [Header("Settings")]
    public int maxHealth = 100;
    public float lowHealthThreshold = 0.3f;
    public float criticalHealthThreshold = 0.15f;

    // UI Elements
    private VisualElement root;
    private VisualElement healthBarFill;
    private Label healthText;
    private Label weaponName;
    private Label ammoCurrent;
    private Label ammoTotal;
    private VisualElement reloadContainer;
    private VisualElement reloadBarFill;
    private VisualElement dashCooldownBar;
    private Label dashLabel;

    // State
    private bool isReloading;
    private float reloadProgress;

    private void Start()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogError("PlayerHudController: UIDocument not found!");
            return;
        }

        // Find player and gun handler if not assigned
        if (playerManager == null)
        {
            playerManager = FindFirstObjectByType<PlayerManager>();
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (gunHandler == null)
        {
            gunHandler = FindFirstObjectByType<GunHandler>();
            
            if (gunHandler == null)
            {
                
                // Try to find it by name first
                var gunHandlerObject = GameObject.Find("GunHandler");
                
                // If not found by name, try to find it on the Player
                if (gunHandlerObject == null)
                {
                    var playerObj = GameObject.FindWithTag("Player");
                    if (playerObj != null)
                    {
                        gunHandler = playerObj.GetComponentInChildren<GunHandler>(true);
                    }
                }
                else
                {
                    gunHandler = gunHandlerObject.GetComponent<GunHandler>();
                }
                
                if (gunHandler == null)
                {
                    var allGunHandlers = FindObjectsByType<GunHandler>(FindObjectsSortMode.InstanceID);
                    if (allGunHandlers.Length > 0)
                    {
                        gunHandler = allGunHandlers[0];
                    }
                }
            }
        }

        InitializeUI();
        if (root != null)
        {
            root.schedule.Execute(() => 
            {
                if (gunHandler != null)
                {
                    UpdateAmmoUI();
                }
            }).StartingIn(100); 
        }
    }

    private void InitializeUI()
    {
        root = uiDocument.rootVisualElement;

        // Health elements
        healthBarFill = root.Q<VisualElement>("health-bar-fill");
        healthText = root.Q<Label>("health-text");

        // Ammo elements
        weaponName = root.Q<Label>("weapon-name");
        ammoCurrent = root.Q<Label>("ammo-current");
        ammoTotal = root.Q<Label>("ammo-total");

        // Reload elements
        reloadContainer = root.Q<VisualElement>("reload-container");
        reloadBarFill = root.Q<VisualElement>("reload-bar-fill");
        
        // Dash elements
        dashCooldownBar = root.Q<VisualElement>("dash-cooldown-bar");
        dashLabel = root.Q<Label>("dash-label");
        
        // Initial update
        UpdateHealthUI();
        UpdateAmmoUI();
        HideReloadIndicator();
    }

    private void Update()
    {
        if (root == null) return;

        // Keep trying to find GunHandler if not found yet
        if (!gunHandler)
        {
            gunHandler = FindFirstObjectByType<GunHandler>();
            if (!gunHandler)
            {
                // Try with all GunHandlers in scene
                var allGunHandlers = FindObjectsByType<GunHandler>(FindObjectsSortMode.InstanceID);
                if (allGunHandlers.Length > 0)
                {
                    gunHandler = allGunHandlers[0];
                }
            }
        }

        UpdateHealthUI();
        UpdateAmmoUI();
        UpdateDashUI();
    }

    private void UpdateHealthUI()
    {
        if (!playerManager || healthBarFill == null) return;

        var currentHealth = playerManager.Health;
        var healthPercent = (float)currentHealth / maxHealth;

        // Update bar width
        healthBarFill.style.width = new Length(healthPercent * 100f, LengthUnit.Percent);

        // Update text
        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }

        // Update bar color based on health level
        healthBarFill.RemoveFromClassList("low");
        healthBarFill.RemoveFromClassList("critical");

        if (healthPercent <= criticalHealthThreshold)
        {
            healthBarFill.AddToClassList("critical");
        }
        else if (healthPercent <= lowHealthThreshold)
        {
            healthBarFill.AddToClassList("low");
        }
    }

    private void UpdateAmmoUI()
    {
        if (!gunHandler)
        {
            return;
        }

        var weapon = gunHandler.currentWeapon;
        
        if (weapon)
        {
            // Update weapon name
            if (weaponName != null)
            {
                var displayName = weapon.weaponName.ToUpper();
                if (weaponName.text != displayName)
                {
                    weaponName.text = displayName;
                }
            }

            if (weapon.infiniteAmmo)
            {
                if (ammoCurrent != null) ammoCurrent.text = "∞";
                if (ammoTotal != null) ammoTotal.text = "";
            }
            else
            {
                var current = GetCurrentAmmo();
                var total = GetTotalAmmo();


                if (ammoCurrent != null)
                {
                    var currentText = current.ToString();
                    if (ammoCurrent.text != currentText)
                    {
                        ammoCurrent.text = currentText;
                    }
                    
                    ammoCurrent.RemoveFromClassList("ammo-warning");
                    ammoCurrent.RemoveFromClassList("ammo-empty");
                    
                    if (current == 0 && gunHandler.IsReloading == false)
                    {
                        ammoCurrent.AddToClassList("ammo-empty");
                    }
                    else if (current <= weapon.magazineSize * 0.25f)
                    {
                        ammoCurrent.AddToClassList("ammo-warning");
                    }
                }

                if (ammoTotal == null) return;
                var totalText = total.ToString();
                if (ammoTotal.text != totalText)
                {
                    ammoTotal.text = totalText;
                }
            }
        }
        else
        {
            // Legacy mode - no weapon config
            if (weaponName != null) weaponName.text = "WEAPON";
            if (ammoCurrent != null) ammoCurrent.text = "∞";
            if (ammoTotal != null) ammoTotal.text = "";
        }
    }

    /// <summary>
    /// Show reload progress indicator
    /// </summary>
    public void ShowReloadIndicator()
    {
        reloadContainer?.AddToClassList("visible");
    }

    /// <summary>
    /// Hide reload indicator
    /// </summary>
    public void HideReloadIndicator()
    {
        if (reloadContainer != null)
        {
            reloadContainer.RemoveFromClassList("visible");
        }
        if (reloadBarFill != null)
        {
            reloadBarFill.style.width = new Length(0, LengthUnit.Percent);
        }
    }

    /// <summary>
    /// Update reload progress (0-1)
    /// </summary>
    public void UpdateReloadProgress(float progress)
    {
        if (reloadBarFill != null)
        {
            reloadBarFill.style.width = new Length(progress * 100f, LengthUnit.Percent);
        }
    }
    
    private int GetCurrentAmmo()
    {
        return gunHandler ? gunHandler.CurrentAmmo : 0;
    }

    private int GetTotalAmmo()
    {
        return gunHandler ? gunHandler.TotalAmmo : 0;
    }

    /// <summary>
    /// Flash the health bar when taking damage
    /// </summary>
    public void FlashHealthBar()
    {
        if (healthBarFill == null) return;
        
        // Add flash effect via class toggle
        healthBarFill.AddToClassList("flash");
        
        // Remove after delay
        healthBarFill.schedule.Execute(() => 
        {
            healthBarFill.RemoveFromClassList("flash");
        }).ExecuteLater(150);
    }

    /// <summary>
    /// Called when weapon is switched - force UI update immediately
    /// </summary>
    public void OnWeaponChanged()
    {
        UpdateAmmoUI();
    }
    
    private void UpdateDashUI()
    {
        if (!playerController) return;

        // Update dash cooldown bar
        if (dashCooldownBar != null)
        {
            float cooldownProgress = 1f - playerController.DashCooldownProgress;
            dashCooldownBar.style.width = new Length(cooldownProgress * 100f, LengthUnit.Percent);
            
            // Add/remove class based on cooldown state
            dashCooldownBar.RemoveFromClassList("dash-ready");
            if (playerController.CanDash)
            {
                dashCooldownBar.AddToClassList("dash-ready");
            }
        }
        
        // Update dash label
        if (dashLabel != null)
        {
            if (playerController.IsDashing)
            {
                dashLabel.text = "DASH!";
            }
            else if (playerController.CanDash)
            {
                dashLabel.text = "DASH";
            }
            else
            {
                float cooldownPercent = (1f - playerController.DashCooldownProgress) * 100f;
                dashLabel.text = $"DASH {cooldownPercent:F0}%";
            }
        }
    }
}
