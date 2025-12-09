using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Auto-assigns buffs and shows non-interactive notifications
/// No UI clicking, no input conflicts, no cursor issues
/// </summary>
public class RewardSelectionManager : MonoBehaviour
{
    [Header("Configuration")]
    public RewardPool rewardPool;
    
    [Header("UI Document")]
    public UIDocument uiDocument;
    
    [Header("Auto-Setup")]
    [Tooltip("Automatically create default buffs if reward pool is empty")]
    public bool autoCreateDefaults = true;
    
    [Header("Notification Settings")]
    [Tooltip("How long to show the reward notification")]
    public float notificationDuration = 4f;
    
    [Tooltip("Number of rewards to grant per wave")]
    public int rewardsPerWave = 1;
    
    [Header("State")]
    [SerializeField] private int currentWave;
    
    // UI Elements
    private VisualElement root;
    private VisualElement notificationPanel;
    private VisualElement buffDisplayPanel; // Permanent buff display
    private VisualElement activeBuffsContainer;
    
    // References
    public PlayerBuffSystem playerBuffSystem;
    private GunHandler gunHandler;
    private EnemySwarmManager swarmManager;
    
    // Singleton for easy access
    public static RewardSelectionManager Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
        
        // Find or create UIDocument
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }
        }
        
        // Ensure UIDocument has proper settings
        if (uiDocument.panelSettings == null)
        {
            var panelSettings = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
            panelSettings.name = "RuntimePanelSettings";
            panelSettings.scaleMode = UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.sortingOrder = 100;
            uiDocument.panelSettings = panelSettings;
        }
    }
    
    private void Start()
    {
        
        swarmManager = FindFirstObjectByType<EnemySwarmManager>();
        
        // Subscribe to wave complete event
        if (swarmManager != null)
        {
            swarmManager.OnWaveComplete.AddListener(OnWaveCompleted);
            Debug.Log("[RewardSystem] ✓ Subscribed to EnemySwarmManager.OnWaveComplete");
        }
        else
        {
            Debug.LogError("[RewardSystem] ✗ No EnemySwarmManager found in scene!");
        }
        
        // Auto-create defaults if needed
        if (autoCreateDefaults && rewardPool == null)
        {
            Debug.Log("[RewardSystem] Auto-creating default reward pool...");
            CreateDefaultRewardPool();
            Debug.Log("[RewardSystem] ✓ Default reward pool created (using fallback buffs)");
        }
        else if (rewardPool != null)
        {
            Debug.Log($"[RewardSystem] ✓ Using configured RewardPool with {rewardPool.availableBuffs.Count} buffs");
        }
        else
        {
            Debug.Log("[RewardSystem] Will use fallback rewards on wave complete");
        }
        
        // Build UI
        BuildUI();
        
        Debug.Log("[RewardSystem] ✓ Initialization complete");
    }
    
    private void OnDestroy()
    {
        if (swarmManager != null)
        {
            swarmManager.OnWaveComplete.RemoveListener(OnWaveCompleted);
        }
    }

    /// <summary>
    /// Called when a wave is completed - auto-grants rewards
    /// </summary>
    private void OnWaveCompleted(int waveIndex)
    {
        currentWave = waveIndex + 1;
        Debug.Log($"[RewardSystem] Wave {currentWave} completed, granting rewards");
        GrantRewards();
    }
    
    /// <summary>
    /// Automatically grant rewards and show notification
    /// </summary>
    private void GrantRewards()
    {
        Debug.Log("[RewardSystem] GrantRewards called");
        
        // Get rewards to grant (uses rewardPool.numberOfChoices)
        var rewards = rewardPool != null ? rewardPool.GetRandomRewards() : GenerateFallbackRewards();
        
        Debug.Log($"[RewardSystem] Reward pool: {(rewardPool != null ? "YES" : "NO")}, Rewards count: {rewards?.Count ?? 0}");
        
        if (rewards == null || rewards.Count == 0)
        {
            Debug.LogWarning("[RewardSystem] No rewards generated!");
            return;
        }
        
        // Auto-apply all rewards
        var grantedRewards = new List<string>();
        var buffDetails = new List<BuffConfig>(); // Track buff configs for detailed display
        
        foreach (var reward in rewards)
        {
            Debug.Log($"[RewardSystem] Processing reward: isBuff={reward.isBuff}, buff={reward.buff?.buffName ?? "NULL"}, weapon={reward.weapon?.weaponName ?? "NULL"}");
            
            if (reward.isBuff && reward.buff != null)
            {
                if (playerBuffSystem != null)
                {
                    playerBuffSystem.AddBuff(reward.buff);
                    grantedRewards.Add(GetBuffDisplayName(reward.buff));
                    buffDetails.Add(reward.buff);
                    Debug.Log($"[RewardSystem] ✓ Auto-granted buff: {reward.buff.buffName}");
                }
                else
                {
                    Debug.LogError("[RewardSystem] PlayerBuffSystem is NULL!");
                }
            }
            else if (!reward.isBuff && reward.weapon != null)
            {
                if (gunHandler == null)
                {
                    var playerObj = GameObject.FindWithTag("Player");
                    foreach (Transform child in playerObj.transform)
                    {
                        foreach (Transform child2 in child.transform)
                        {
                            var gh = child2.GetComponent<GunHandler>();
                            if (gh != null)
                            {
                                gunHandler = gh;
                                Debug.Log("[RewardSystem] ✓ Found GunHandler on player");
                                break;
                            }
                        }
                        break;
                    }
                }
                gunHandler.SwitchWeapon(reward.weapon);
                grantedRewards.Add($"{reward.weapon.weaponName}");
                Debug.Log($"[RewardSystem] ✓ Auto-granted weapon: {reward.weapon.weaponName}");
            }
            else
            {
                Debug.LogWarning("[RewardSystem] Reward has no buff or weapon!");
            }
        }
        
        Debug.Log($"[RewardSystem] Total granted rewards: {grantedRewards.Count}");
        
        // Show notification and update buff display
        if (grantedRewards.Count > 0)
        {
            ShowNotification(grantedRewards);
            UpdateBuffDisplay(grantedRewards, buffDetails);
            Debug.Log("[RewardSystem] ✓ Notification and buff display updated");
        }
        else
        {
            Debug.LogWarning("[RewardSystem] No rewards were successfully granted!");
        }
    }
    
    /// <summary>
    /// Get a detailed display name for a buff with its stats
    /// </summary>
    private string GetBuffDisplayName(BuffConfig buff)
    {
        var baseName = buff.buffName;
        var detail = "";
        
        if (buff.percentageBonus > 0)
        {
            detail = $" +{(buff.percentageBonus * 100):F0}%";
        }
        else if (buff.flatBonus > 0)
        {
            detail = $" +{buff.flatBonus:F0}";
        }
        
        return baseName + detail;
    }
    
    /// <summary>
    /// Show a non-interactive notification of granted rewards
    /// </summary>
    private void ShowNotification(List<string> rewardNames)
    {
        if (notificationPanel == null)
        {
            Debug.LogWarning("[RewardSystem] Notification panel not found!");
            return;
        }
        
        // Update notification content
        var titleLabel = notificationPanel.Q<Label>("notification-title");
        var rewardList = notificationPanel.Q<VisualElement>("reward-list");
        
        if (titleLabel != null)
        {
            titleLabel.text = $"WAVE {currentWave} COMPLETE!";
        }
        
        if (rewardList != null)
        {
            rewardList.Clear();
            foreach (var rewardName in rewardNames)
            {
                var rewardLabel = new Label($"✓ {rewardName}");
                rewardLabel.AddToClassList("reward-item");
                rewardLabel.style.fontSize = 18;
                rewardLabel.style.color = new Color(0.4f, 1f, 0.4f);
                rewardLabel.style.marginBottom = 5;
                rewardLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                rewardList.Add(rewardLabel);
            }
        }
        
        // Show the notification
        notificationPanel.style.display = DisplayStyle.Flex;
        
        // Auto-hide after duration
        StartCoroutine(HideNotificationAfterDelay());
    }
    
    /// <summary>
    /// Hide notification after a delay
    /// </summary>
    private System.Collections.IEnumerator HideNotificationAfterDelay()
    {
        yield return new WaitForSeconds(notificationDuration);
        
        if (notificationPanel != null)
        {
            notificationPanel.style.display = DisplayStyle.None;
        }
    }
    
    /// <summary>
    /// Build the notification UI programmatically
    /// </summary>
    private void BuildUI()
    {
        // Create root
        root = new VisualElement();
        root.style.flexGrow = 1;
        root.style.width = new Length(100, LengthUnit.Percent);
        root.style.height = new Length(100, LengthUnit.Percent);
        root.style.position = Position.Absolute;
        root.style.left = 0;
        root.style.top = 0;
        root.pickingMode = PickingMode.Ignore; // Don't capture any input!
        
        // Create buff display panel (top-right corner)
        CreateBuffDisplayPanel();
        
        // Create notification container (for centering)
        var notificationContainer = new VisualElement();
        notificationContainer.style.width = new Length(100, LengthUnit.Percent);
        notificationContainer.style.height = new Length(100, LengthUnit.Percent);
        notificationContainer.style.position = Position.Absolute;
        notificationContainer.style.alignItems = Align.Center;
        notificationContainer.style.justifyContent = Justify.FlexStart;
        notificationContainer.style.paddingTop = 60; // Position from top
        notificationContainer.pickingMode = PickingMode.Ignore;
        root.Add(notificationContainer);
        
        // Create notification panel (top-center)
        notificationPanel = new VisualElement();
        notificationPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        notificationPanel.style.borderTopLeftRadius = 12;
        notificationPanel.style.borderTopRightRadius = 12;
        notificationPanel.style.borderBottomLeftRadius = 12;
        notificationPanel.style.borderBottomRightRadius = 12;
        notificationPanel.style.borderLeftColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        notificationPanel.style.borderRightColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        notificationPanel.style.borderTopColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        notificationPanel.style.borderBottomColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        notificationPanel.style.borderLeftWidth = 3;
        notificationPanel.style.borderRightWidth = 3;
        notificationPanel.style.borderTopWidth = 3;
        notificationPanel.style.borderBottomWidth = 3;
        notificationPanel.style.paddingTop = 20;
        notificationPanel.style.paddingBottom = 20;
        notificationPanel.style.paddingLeft = 30;
        notificationPanel.style.paddingRight = 30;
        notificationPanel.style.alignItems = Align.Center;
        notificationPanel.style.minWidth = 300;
        notificationPanel.style.maxWidth = 500;
        notificationPanel.pickingMode = PickingMode.Ignore; // Don't capture any input!
        notificationContainer.Add(notificationPanel);
        
        // Title
        var title = new Label("WAVE COMPLETE!");
        title.name = "notification-title";
        title.style.fontSize = 28;
        title.style.color = new Color(1f, 0.8f, 0.2f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 5;
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        notificationPanel.Add(title);
        
        // Subtitle
        var subtitle = new Label("Rewards Granted:");
        subtitle.style.fontSize = 16;
        subtitle.style.color = new Color(0.8f, 0.8f, 0.8f);
        subtitle.style.marginBottom = 15;
        subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
        notificationPanel.Add(subtitle);
        
        // Reward list container
        var rewardList = new VisualElement();
        rewardList.name = "reward-list";
        rewardList.style.alignItems = Align.FlexStart;
        rewardList.style.width = new Length(100, LengthUnit.Percent);
        notificationPanel.Add(rewardList);
        
        // Hide panel initially
        notificationPanel.style.display = DisplayStyle.None;
        
        // Set as UI Document root
        if (uiDocument != null)
        {
            uiDocument.rootVisualElement.Add(root);
            Debug.Log("[RewardSystem] Notification UI built successfully");
        }
        else
        {
            Debug.LogError("[RewardSystem] No UIDocument found!");
        }
    }
    
    /// <summary>
    /// Create the permanent buff display panel
    /// </summary>
    private void CreateBuffDisplayPanel()
    {
        buffDisplayPanel = new VisualElement();
        buffDisplayPanel.style.position = Position.Absolute;
        buffDisplayPanel.style.top = 10;
        buffDisplayPanel.style.right = 10;
        buffDisplayPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.85f);
        buffDisplayPanel.style.borderTopLeftRadius = 8;
        buffDisplayPanel.style.borderTopRightRadius = 8;
        buffDisplayPanel.style.borderBottomLeftRadius = 8;
        buffDisplayPanel.style.borderBottomRightRadius = 8;
        buffDisplayPanel.style.paddingTop = 12;
        buffDisplayPanel.style.paddingBottom = 12;
        buffDisplayPanel.style.paddingLeft = 15;
        buffDisplayPanel.style.paddingRight = 15;
        buffDisplayPanel.style.minWidth = 200;
        buffDisplayPanel.style.maxWidth = 300;
        buffDisplayPanel.pickingMode = PickingMode.Ignore;
        root.Add(buffDisplayPanel);
        
        // Title
        var title = new Label("ACTIVE BUFFS");
        title.style.fontSize = 14;
        title.style.color = new Color(1f, 0.8f, 0.2f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 8;
        title.style.unityTextAlign = TextAnchor.UpperCenter;
        buffDisplayPanel.Add(title);
        
        // Container for active buffs
        activeBuffsContainer = new VisualElement();
        activeBuffsContainer.style.alignItems = Align.FlexStart;
        activeBuffsContainer.style.width = new Length(100, LengthUnit.Percent);
        buffDisplayPanel.Add(activeBuffsContainer);
        
        // Initial empty message
        var emptyLabel = new Label("No buffs yet");
        emptyLabel.name = "empty-message";
        emptyLabel.style.fontSize = 12;
        emptyLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
        emptyLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
        activeBuffsContainer.Add(emptyLabel);
    }
    
    /// <summary>
    /// Update the buff display with current buffs
    /// </summary>
    private void UpdateBuffDisplay(List<string> newBuffNames, List<BuffConfig> buffConfigs = null)
    {
        if (activeBuffsContainer == null) return;
        
        // Remove empty message if exists
        var emptyMsg = activeBuffsContainer.Q<Label>("empty-message");
        if (emptyMsg != null)
        {
            activeBuffsContainer.Remove(emptyMsg);
        }
        
        // Add new buff entries with animation
        for (int i = 0; i < newBuffNames.Count; i++)
        {
            var buffName = newBuffNames[i];
            var buffConfig = buffConfigs != null && i < buffConfigs.Count ? buffConfigs[i] : null;
            
            var buffEntry = new VisualElement();
            buffEntry.style.flexDirection = FlexDirection.Row;
            buffEntry.style.alignItems = Align.Center;
            buffEntry.style.marginBottom = 6;
            buffEntry.style.paddingTop = 4;
            buffEntry.style.paddingBottom = 4;
            buffEntry.style.paddingLeft = 6;
            buffEntry.style.paddingRight = 6;
            buffEntry.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
            buffEntry.style.borderTopLeftRadius = 4;
            buffEntry.style.borderTopRightRadius = 4;
            buffEntry.style.borderBottomLeftRadius = 4;
            buffEntry.style.borderBottomRightRadius = 4;
            buffEntry.style.borderLeftWidth = 2;
            buffEntry.style.borderLeftColor = new Color(0.4f, 1f, 0.4f);
            
            // Buff icon emoji
            if (buffConfig != null)
            {
                var icon = new Label(GetBuffIcon(buffConfig.buffType));
                icon.style.fontSize = 16;
                icon.style.marginRight = 6;
                buffEntry.Add(icon);
            }
            
            // "NEW" indicator
            var newIndicator = new Label("NEW");
            newIndicator.style.fontSize = 10;
            newIndicator.style.color = new Color(1f, 1f, 0.3f);
            newIndicator.style.unityFontStyleAndWeight = FontStyle.Bold;
            newIndicator.style.marginRight = 6;
            newIndicator.style.backgroundColor = new Color(1f, 0.8f, 0f, 0.3f);
            newIndicator.style.paddingLeft = 4;
            newIndicator.style.paddingRight = 4;
            newIndicator.style.paddingTop = 2;
            newIndicator.style.paddingBottom = 2;
            newIndicator.style.borderTopLeftRadius = 3;
            newIndicator.style.borderTopRightRadius = 3;
            newIndicator.style.borderBottomLeftRadius = 3;
            newIndicator.style.borderBottomRightRadius = 3;
            buffEntry.Add(newIndicator);
            
            // Buff name
            var nameLabel = new Label(buffName);
            nameLabel.style.fontSize = 12;
            nameLabel.style.color = Color.white;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            buffEntry.Add(nameLabel);
            
            activeBuffsContainer.Add(buffEntry);
            
            StartCoroutine(RemoveNewIndicator(newIndicator, buffEntry));
        }
    }
    
    /// <summary>
    /// Get emoji icon for buff type
    /// </summary>
    private string GetBuffIcon(BuffType type)
    {
        return type switch
        {
            BuffType.DamageBoost => "⚔️",
            BuffType.SpeedBoost => "💨",
            BuffType.HealthBoost => "❤️",
            BuffType.MaxHealthBoost => "💖",
            BuffType.FireRateBoost => "🔥",
            BuffType.CriticalChanceBoost => "🎯",
            BuffType.CriticalDamageBoost => "💥",
            BuffType.LifeSteal => "🧛",
            BuffType.ExplosiveShots => "💣",
            BuffType.PenetrationShots => "➡️",
            BuffType.MultiShot => "🌟",
            _ => "✨"
        };
    }
    
    /// <summary>
    /// Remove the "NEW" indicator after a delay
    /// </summary>
    private System.Collections.IEnumerator RemoveNewIndicator(VisualElement indicator, VisualElement parent)
    {
        yield return new WaitForSeconds(3f);
        
        if (indicator != null && parent != null)
        {
            parent.Remove(indicator);
            // Update left border to normal color
            parent.style.borderLeftColor = new Color(0.3f, 0.3f, 0.4f);
        }
    }
    
    /// <summary>
    /// Create default reward pool with built-in buffs
    /// </summary>
    private void CreateDefaultRewardPool()
    {
        rewardPool = ScriptableObject.CreateInstance<RewardPool>();
        rewardPool.availableBuffs = new List<BuffConfig>();
        Debug.Log("[RewardSystem] Created default reward pool");
    }
    
    /// <summary>
    /// Generate fallback rewards if no reward pool
    /// </summary>
    private List<RewardChoice> GenerateFallbackRewards()
    {
        var choices = new List<RewardChoice>();
        
        // Create runtime buffs for fallback
        var damageBuff = ScriptableObject.CreateInstance<BuffConfig>();
        damageBuff.buffName = "Damage Up";
        damageBuff.description = "+15% damage";
        damageBuff.buffType = BuffType.DamageBoost;
        damageBuff.percentageBonus = 0.15f;
        damageBuff.rarity = BuffRarity.Common;
        choices.Add(new RewardChoice(damageBuff));
        
        var speedBuff = ScriptableObject.CreateInstance<BuffConfig>();
        speedBuff.buffName = "Speed Boost";
        speedBuff.description = "+10% movement speed";
        speedBuff.buffType = BuffType.SpeedBoost;
        speedBuff.percentageBonus = 0.10f;
        speedBuff.rarity = BuffRarity.Common;
        choices.Add(new RewardChoice(speedBuff));
        
        var fireRateBuff = ScriptableObject.CreateInstance<BuffConfig>();
        fireRateBuff.buffName = "Fire Rate Boost";
        fireRateBuff.description = "+20% fire rate";
        fireRateBuff.buffType = BuffType.FireRateBoost;
        fireRateBuff.percentageBonus = 0.20f;
        fireRateBuff.rarity = BuffRarity.Uncommon;
        choices.Add(new RewardChoice(fireRateBuff));
        
        Debug.Log($"[RewardSystem] Using fallback rewards with {choices.Count} buffs");
        return choices;
    }
}

