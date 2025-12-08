using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Manages the reward selection UI and game pause during selection
/// This is the main controller - attach to a GameObject in the scene
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
    
    [Header("State")]
    [SerializeField] private bool isSelectionActive;
    [SerializeField] private int currentWave;
    
    // UI Elements
    private VisualElement root;
    private VisualElement selectionPanel;
    private Label waveLabel;
    private VisualElement choicesContainer;
    
    // References
    private PlayerBuffSystem playerBuffSystem;
    private GunHandler gunHandler;
    private EnemySwarmManager swarmManager;
    
    // Current choices
    private List<RewardChoice> currentChoices = new List<RewardChoice>();
    
    // Cursor state storage
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;
    
    public bool IsSelectionActive => isSelectionActive;
    
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
            // Create runtime PanelSettings
            var panelSettings = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
            panelSettings.name = "RuntimePanelSettings";
            panelSettings.scaleMode = UnityEngine.UIElements.PanelScaleMode.ConstantPixelSize;
            panelSettings.sortingOrder = 100; // High sorting order to be on top
            uiDocument.panelSettings = panelSettings;
            Debug.Log("[RewardSelection] Created runtime PanelSettings");
        }
    }
    
    private void Start()
    {
        // Find references
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[RewardSelection] No player found with 'Player' tag!");
        }
        else
        {
            playerBuffSystem = player.GetComponent<PlayerBuffSystem>();
            if (playerBuffSystem == null)
            {
                playerBuffSystem = player.AddComponent<PlayerBuffSystem>();
                Debug.Log("[RewardSelection] Added PlayerBuffSystem to player");
            }
            gunHandler = player.GetComponentInChildren<GunHandler>();
            if (gunHandler == null)
            {
                Debug.LogWarning("[RewardSelection] No GunHandler found on player!");
            }
        }
        
        swarmManager = FindFirstObjectByType<EnemySwarmManager>();
        
        // Subscribe to wave complete event
        if (swarmManager != null)
        {
            swarmManager.OnWaveComplete.AddListener(OnWaveCompleted);
            Debug.Log("[RewardSelection] Subscribed to wave complete event");
        }
        else
        {
            Debug.LogWarning("[RewardSelection] No EnemySwarmManager found! Rewards won't show automatically.");
        }
        
        // Auto-create defaults if needed
        if (autoCreateDefaults && rewardPool == null)
        {
            Debug.Log("[RewardSelection] Creating default reward pool...");
            CreateDefaultRewardPool();
        }
        
        // Build UI
        BuildUI();
        HideSelection();
        
        Debug.Log("[RewardSelection] Initialization complete");
    }
    
    private void OnDestroy()
    {
        if (swarmManager != null)
        {
            swarmManager.OnWaveComplete.RemoveListener(OnWaveCompleted);
        }
    }

    private void Update()
    {
        // Test with new input system - check for R key from UI action or direct test
        var testAction = InputSystem.actions.FindAction("UI/Submit");
        if (testAction != null && testAction.WasPerformedThisFrame())
        {
            // Only trigger if UI is not active and player presses the submit action
            if (!isSelectionActive && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                // Alternative: Use a dedicated test key through actions
                // For now, we'll keep the direct input for testing
            }
        }
        
        // Direct test with R key for quick testing (can be removed in production)
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame && !isSelectionActive)
        {
            Debug.Log("[RewardSelection] Manual trigger via R key");
            ShowSelection();
        }
    }
    
    /// <summary>
    /// Called when a wave is completed
    /// </summary>
    private void OnWaveCompleted(int waveIndex)
    {
        currentWave = waveIndex + 1;
        Debug.Log($"[RewardSelection] Wave {currentWave} completed, showing rewards");
        ShowSelection();
    }
    
    /// <summary>
    /// Show the reward selection UI
    /// </summary>
    public void ShowSelection()
    {
        if (isSelectionActive) return;
        
        isSelectionActive = true;
        
        // Save current cursor state
        previousCursorLockMode = UnityEngine.Cursor.lockState;
        previousCursorVisible = UnityEngine.Cursor.visible;
        
        // PAUSE the game to prevent any input conflicts
        Time.timeScale = 0f;
        
        // Unlock cursor for UI interaction
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        
        // Disable shooting input while UI is active
        if (gunHandler != null)
        {
            gunHandler.DisableInput();
        }
        
        // Generate choices
        currentChoices = rewardPool != null ? rewardPool.GetRandomRewards() : GenerateFallbackChoices();
        
        // Update UI
        UpdateUI();
        
        // Show panel
        if (selectionPanel != null)
        {
            selectionPanel.style.display = DisplayStyle.Flex;
        }
        
        Debug.Log("[RewardSelection] Selection shown with " + currentChoices.Count + " choices (GAME PAUSED)");
    }
    
    /// <summary>
    /// Hide the reward selection UI
    /// </summary>
    public void HideSelection()
    {
        isSelectionActive = false;
        
        // Resume game
        Time.timeScale = 1f;
        
        // Re-enable shooting input
        if (gunHandler != null)
        {
            gunHandler.EnableInput();
            gunHandler.SetupCursor();
        }
        
        // Restore previous cursor state
        UnityEngine.Cursor.lockState = previousCursorLockMode;
        UnityEngine.Cursor.visible = previousCursorVisible;
        
        // Hide panel
        if (selectionPanel != null)
        {
            selectionPanel.style.display = DisplayStyle.None;
        }
        
        Debug.Log("[RewardSelection] Selection hidden, cursor state restored, GAME RESUMED");
    }
    
    /// <summary>
    /// Select a reward
    /// </summary>
    public void SelectReward(int index)
    {
        if (index < 0 || index >= currentChoices.Count) return;
        
        var choice = currentChoices[index];
        
        if (choice.isBuff)
        {
            // Apply buff
            if (playerBuffSystem != null)
            {
                playerBuffSystem.AddBuff(choice.buff);
                Debug.Log($"[RewardSelection] Applied buff: {choice.buff.buffName}");
            }
        }
        else
        {
            // Equip weapon
            if (gunHandler != null)
            {
                gunHandler.SwitchWeapon(choice.weapon);
                Debug.Log($"[RewardSelection] Equipped weapon: {choice.weapon.weaponName}");
            }
        }
        
        HideSelection();
    }
    
    /// <summary>
    /// Skip reward selection
    /// </summary>
    public void SkipSelection()
    {
        Debug.Log("[RewardSelection] Skipped selection");
        HideSelection();
    }
    
    /// <summary>
    /// Build the UI programmatically
    /// </summary>
    private void BuildUI()
    {
        // Create root
        root = new VisualElement();
        root.style.flexGrow = 1;
        root.style.width = new Length(100, LengthUnit.Percent);
        root.style.height = new Length(100, LengthUnit.Percent);
        root.style.justifyContent = Justify.Center;
        root.style.alignItems = Align.Center;
        root.style.overflow = Overflow.Hidden;
        
        // Create darkened background
        var backdrop = new VisualElement();
        backdrop.style.position = Position.Absolute;
        backdrop.style.left = 0;
        backdrop.style.right = 0;
        backdrop.style.top = 0;
        backdrop.style.bottom = 0;
        backdrop.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        backdrop.pickingMode = PickingMode.Position; // Capture all mouse events
        root.Add(backdrop);
        
        // Create selection panel
        selectionPanel = new VisualElement();
        selectionPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        selectionPanel.style.borderTopLeftRadius = 20;
        selectionPanel.style.borderTopRightRadius = 20;
        selectionPanel.style.borderBottomLeftRadius = 20;
        selectionPanel.style.borderBottomRightRadius = 20;
        selectionPanel.style.borderLeftColor = new Color(1f, 0.8f, 0.2f, 0.8f);
        selectionPanel.style.borderRightColor = new Color(1f, 0.8f, 0.2f, 0.8f);
        selectionPanel.style.borderTopColor = new Color(1f, 0.8f, 0.2f, 0.8f);
        selectionPanel.style.borderBottomColor = new Color(1f, 0.8f, 0.2f, 0.8f);
        selectionPanel.style.borderLeftWidth = 3;
        selectionPanel.style.borderRightWidth = 3;
        selectionPanel.style.borderTopWidth = 3;
        selectionPanel.style.borderBottomWidth = 3;
        selectionPanel.style.paddingTop = 30;
        selectionPanel.style.paddingBottom = 30;
        selectionPanel.style.paddingLeft = 40;
        selectionPanel.style.paddingRight = 40;
        selectionPanel.style.alignItems = Align.Center;
        // Responsive sizing
        selectionPanel.style.maxWidth = new Length(90, LengthUnit.Percent);
        selectionPanel.style.maxHeight = new Length(80, LengthUnit.Percent);
        selectionPanel.style.minWidth = 300;
        selectionPanel.style.overflow = Overflow.Visible;
        selectionPanel.pickingMode = PickingMode.Position; // Capture mouse events
        root.Add(selectionPanel);
        
        // Title
        var title = new Label("WAVE COMPLETE!");
        title.style.fontSize = 36;
        title.style.color = new Color(1f, 0.8f, 0.2f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 10;
        selectionPanel.Add(title);
        
        // Wave label
        waveLabel = new Label("Wave 1");
        waveLabel.style.fontSize = 24;
        waveLabel.style.color = Color.white;
        waveLabel.style.marginBottom = 20;
        selectionPanel.Add(waveLabel);
        
        // Subtitle
        var subtitle = new Label("Choose your reward:");
        subtitle.style.fontSize = 20;
        subtitle.style.color = new Color(0.8f, 0.8f, 0.8f);
        subtitle.style.marginBottom = 30;
        selectionPanel.Add(subtitle);
        
        // Choices container
        choicesContainer = new VisualElement();
        choicesContainer.style.flexDirection = FlexDirection.Row;
        choicesContainer.style.justifyContent = Justify.Center;
        choicesContainer.style.alignItems = Align.Stretch;
        choicesContainer.style.flexWrap = Wrap.Wrap; // Wrap on small screens
        choicesContainer.style.maxWidth = new Length(100, LengthUnit.Percent);
        selectionPanel.Add(choicesContainer);
        
        // Skip button
        var skipButton = new Button(SkipSelection);
        skipButton.text = "Skip";
        skipButton.style.marginTop = 30;
        skipButton.style.paddingTop = 10;
        skipButton.style.paddingBottom = 10;
        skipButton.style.paddingLeft = 30;
        skipButton.style.paddingRight = 30;
        skipButton.style.fontSize = 16;
        skipButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
        skipButton.style.color = Color.white;
        skipButton.style.borderTopLeftRadius = 8;
        skipButton.style.borderTopRightRadius = 8;
        skipButton.style.borderBottomLeftRadius = 8;
        skipButton.style.borderBottomRightRadius = 8;
        selectionPanel.Add(skipButton);
        
        // Hide panel initially
        selectionPanel.style.display = DisplayStyle.None;
        
        // Set as UI Document root
        if (uiDocument != null)
        {
            uiDocument.rootVisualElement.Add(root);
            
            // Make UI work with Time.timeScale = 0
            uiDocument.rootVisualElement.SetEnabled(true);
            
            Debug.Log("[RewardSelection] UI built and added to document");
        }
        else
        {
            Debug.LogError("[RewardSelection] No UIDocument found!");
        }
    }
    
    /// <summary>
    /// Update UI with current choices
    /// </summary>
    private void UpdateUI()
    {
        if (waveLabel != null)
        {
            waveLabel.text = $"Wave {currentWave}";
        }
        
        if (choicesContainer == null) return;
        
        choicesContainer.Clear();
        
        for (var i = 0; i < currentChoices.Count; i++)
        {
            var choice = currentChoices[i];
            var choiceCard = CreateChoiceCard(choice, i);
            choicesContainer.Add(choiceCard);
        }
    }
    
    /// <summary>
    /// Create a visual card for a reward choice
    /// </summary>
    private VisualElement CreateChoiceCard(RewardChoice choice, int index)
    {
        var card = new VisualElement();
        card.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
        card.style.borderTopLeftRadius = 15;
        card.style.borderTopRightRadius = 15;
        card.style.borderBottomLeftRadius = 15;
        card.style.borderBottomRightRadius = 15;
        card.style.paddingTop = 20;
        card.style.paddingBottom = 20;
        card.style.paddingLeft = 20;
        card.style.paddingRight = 20;
        card.style.marginLeft = 10;
        card.style.marginRight = 10;
        card.style.marginTop = 10;
        card.style.marginBottom = 10;
        // Responsive width - flex to fill but with constraints
        card.style.minWidth = 150;
        card.style.maxWidth = 220;
        card.style.flexGrow = 1;
        card.style.flexShrink = 1;
        card.style.alignItems = Align.Center;
        card.style.borderTopWidth = 3;
        card.style.borderTopColor = choice.GetColor();
        
        // Type label
        var typeLabel = new Label(choice.isBuff ? "BUFF" : "WEAPON");
        typeLabel.style.fontSize = 12;
        typeLabel.style.color = choice.GetColor();
        typeLabel.style.marginBottom = 10;
        card.Add(typeLabel);
        
        // Rarity label (for buffs)
        if (choice.isBuff)
        {
            var rarityLabel = new Label(choice.buff.rarity.ToString().ToUpper());
            rarityLabel.style.fontSize = 10;
            rarityLabel.style.color = choice.GetColor();
            rarityLabel.style.marginBottom = 5;
            card.Add(rarityLabel);
        }
        
        // Icon placeholder
        var iconContainer = new VisualElement();
        iconContainer.style.width = 60;
        iconContainer.style.height = 60;
        iconContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
        iconContainer.style.borderTopLeftRadius = 10;
        iconContainer.style.borderTopRightRadius = 10;
        iconContainer.style.borderBottomLeftRadius = 10;
        iconContainer.style.borderBottomRightRadius = 10;
        iconContainer.style.marginBottom = 15;
        iconContainer.style.justifyContent = Justify.Center;
        iconContainer.style.alignItems = Align.Center;
        
        // Icon symbol
        var iconSymbol = new Label(choice.isBuff ? GetBuffSymbol(choice.buff.buffType) : "🔫");
        iconSymbol.style.fontSize = 30;
        iconContainer.Add(iconSymbol);
        
        card.Add(iconContainer);
        
        // Name
        var nameLabel = new Label(choice.GetName());
        nameLabel.style.fontSize = 16;
        nameLabel.style.color = Color.white;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.marginBottom = 10;
        nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        nameLabel.style.whiteSpace = WhiteSpace.Normal;
        card.Add(nameLabel);
        
        // Description
        var descLabel = new Label(choice.GetDescription());
        descLabel.style.fontSize = 12;
        descLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
        descLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        descLabel.style.whiteSpace = WhiteSpace.Normal;
        descLabel.style.marginBottom = 15;
        card.Add(descLabel);
        
        // Select button
        var capturedIndex = index;
        var selectButton = new Button(() => SelectReward(capturedIndex));
        selectButton.text = "SELECT";
        selectButton.style.paddingTop = 8;
        selectButton.style.paddingBottom = 8;
        selectButton.style.paddingLeft = 20;
        selectButton.style.paddingRight = 20;
        selectButton.style.fontSize = 14;
        selectButton.style.backgroundColor = choice.GetColor();
        selectButton.style.color = Color.white;
        selectButton.style.borderTopLeftRadius = 8;
        selectButton.style.borderTopRightRadius = 8;
        selectButton.style.borderBottomLeftRadius = 8;
        selectButton.style.borderBottomRightRadius = 8;
        card.Add(selectButton);
        
        return card;
    }
    
    private string GetBuffSymbol(BuffType type)
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
    /// Create default reward pool with built-in buffs
    /// </summary>
    private void CreateDefaultRewardPool()
    {
        rewardPool = ScriptableObject.CreateInstance<RewardPool>();
        
        // Add default buffs programmatically
        rewardPool.availableBuffs = new List<BuffConfig>();
        
        // We'll create runtime buffs since we can't save ScriptableObjects at runtime
        // These will work for testing but for production, create actual assets
        
        Debug.Log("[RewardSelection] Created default reward pool");
    }
    
    /// <summary>
    /// Generate fallback choices if no reward pool
    /// </summary>
    private List<RewardChoice> GenerateFallbackChoices()
    {
        var choices = new List<RewardChoice>();
        
        // Create runtime buffs
        var damageBuff = ScriptableObject.CreateInstance<BuffConfig>();
        damageBuff.buffName = "Damage Up";
        damageBuff.description = "+{percent}% damage";
        damageBuff.buffType = BuffType.DamageBoost;
        damageBuff.percentageBonus = 0.15f;
        damageBuff.rarity = BuffRarity.Common;
        choices.Add(new RewardChoice(damageBuff));
        
        var speedBuff = ScriptableObject.CreateInstance<BuffConfig>();
        speedBuff.buffName = "Speed Boost";
        speedBuff.description = "+{percent}% movement speed";
        speedBuff.buffType = BuffType.SpeedBoost;
        speedBuff.percentageBonus = 0.1f;
        speedBuff.rarity = BuffRarity.Common;
        choices.Add(new RewardChoice(speedBuff));
        
        var healthBuff = ScriptableObject.CreateInstance<BuffConfig>();
        healthBuff.buffName = "Heal";
        healthBuff.description = "Restore {flat} health";
        healthBuff.buffType = BuffType.HealthBoost;
        healthBuff.flatBonus = 25f;
        healthBuff.rarity = BuffRarity.Uncommon;
        choices.Add(new RewardChoice(healthBuff));
        
        return choices;
    }
}

