using UnityEngine;

/// <summary>
/// Quick setup script to initialize the roguelike system at runtime
/// Add this to any GameObject in the scene
/// </summary>
public class RoguelikeRuntimeSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [Tooltip("Automatically create RewardSelectionManager if not found")]
    public bool autoCreateRewardManager = true;
    
    [Tooltip("Automatically add PlayerBuffSystem to player if not found")]
    public bool autoSetupPlayer = true;

    private void Awake()
    {
        if (autoCreateRewardManager)
        {
            SetupRewardManager();
        }

        if (autoSetupPlayer)
        {
            SetupPlayer();
        }
    }

    private void SetupRewardManager()
    {
        var existingManager = FindFirstObjectByType<RewardSelectionManager>();
        if (existingManager == null)
        {
            Debug.Log("[RoguelikeSetup] Creating RewardSelectionManager...");
            var managerObj = new GameObject("RewardSelectionManager");
            var manager = managerObj.AddComponent<RewardSelectionManager>();
            manager.autoCreateDefaults = true;
            Debug.Log("[RoguelikeSetup] RewardSelectionManager created!");
        }
        else
        {
            Debug.Log("[RoguelikeSetup] RewardSelectionManager already exists");
        }
    }

    private void SetupPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[RoguelikeSetup] No player found with 'Player' tag!");
            return;
        }

        var buffSystem = player.GetComponent<PlayerBuffSystem>();
        if (buffSystem == null)
        {
            Debug.Log("[RoguelikeSetup] Adding PlayerBuffSystem to player...");
            player.AddComponent<PlayerBuffSystem>();
        }
        else
        {
            Debug.Log("[RoguelikeSetup] PlayerBuffSystem already exists on player");
        }
    }
}

