using UnityEngine;

public class SwarmConfigHelper : MonoBehaviour
{
    public GameObject enemyPrefab;
    
    [ContextMenu("Basic Swarm Config")]
    public void CreateBasicSwarm()
    {
        SwarmConfig config = ScriptableObject.CreateInstance<SwarmConfig>();
        config.swarmName = "Basic Assault";
        config.description = "A simple wave of enemies";
        config.formationType = SpawnFormationType.Circle;
        config.spawnRadius = 10f;
        config.spawnDelay = 0.2f;
        config.swarmColor = Color.red;
        
        if (enemyPrefab != null)
        {
            config.enemyTypes.Add(new EnemyWaveData { enemyPrefab = enemyPrefab, count = 8, spawnPriority = 0.5f });
        }
        
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(config, "Assets/SwarmConfigs/BasicSwarm.asset");
        UnityEditor.AssetDatabase.SaveAssets();
        #endif
    }
    
    [ContextMenu("Spiral Swarm Config")]
    public void CreateSpiralSwarm()
    {
        SwarmConfig config = ScriptableObject.CreateInstance<SwarmConfig>();
        config.swarmName = "Spiral Onslaught";
        config.description = "Enemies spawn in a mesmerizing spiral pattern";
        config.formationType = SpawnFormationType.Spiral;
        config.spawnRadius = 15f;
        config.spawnDelay = 0.1f;
        config.swarmColor = Color.cyan;
        config.healthMultiplier = 1.2f;
        config.speedMultiplier = 1.1f;
        
        if (enemyPrefab != null)
        {
            config.enemyTypes.Add(new EnemyWaveData { enemyPrefab = enemyPrefab, count = 15, spawnPriority = 0.5f });
        }
        
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(config, "Assets/SwarmConfigs/SpiralSwarm.asset");
        UnityEditor.AssetDatabase.SaveAssets();
        #endif
    }
    
    [ContextMenu("Boss Swarm Config")]
    public void CreateBossSwarm()
    {
        SwarmConfig config = ScriptableObject.CreateInstance<SwarmConfig>();
        config.swarmName = "Elite Squadron";
        config.description = "Few but powerful enemies";
        config.formationType = SpawnFormationType.VFormation;
        config.spawnRadius = 12f;
        config.spawnDelay = 0.5f;
        config.swarmColor = Color.magenta;
        config.healthMultiplier = 3f;
        config.speedMultiplier = 0.8f;
        config.damageMultiplier = 2f;
        config.spawnAllAtOnce = true;
        
        if (enemyPrefab != null)
        {
            config.enemyTypes.Add(new EnemyWaveData { enemyPrefab = enemyPrefab, count = 5, spawnPriority = 0.5f });
        }
        
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(config, "Assets/SwarmConfigs/BossSwarm.asset");
        UnityEditor.AssetDatabase.SaveAssets();
        #endif
    }
    
    [ContextMenu("Surrounding Swarm Config")]
    public void CreateSurroundingSwarm()
    {
        SwarmConfig config = ScriptableObject.CreateInstance<SwarmConfig>();
        config.swarmName = "Encirclement";
        config.description = "Enemies surround the player from all sides";
        config.formationType = SpawnFormationType.Surrounding;
        config.spawnRadius = 20f;
        config.spawnDelay = 0.15f;
        config.swarmColor = Color.yellow;
        config.speedMultiplier = 1.3f;
        
        if (enemyPrefab != null)
        {
            config.enemyTypes.Add(new EnemyWaveData { enemyPrefab = enemyPrefab, count = 20, spawnPriority = 0.5f });
        }
        
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(config, "Assets/SwarmConfigs/SurroundingSwarm.asset");
        UnityEditor.AssetDatabase.SaveAssets();
        #endif
    }
}

