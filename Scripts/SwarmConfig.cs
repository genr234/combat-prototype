using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Swarm Config", menuName = "Combat Dystopian/Swarm Configuration")]
public class SwarmConfig : ScriptableObject
{
    [Header("Swarm Identity")]
    public string swarmName = "Basic Swarm";
    [TextArea(2, 4)]
    public string description = "A basic enemy swarm";
    
    [Header("Enemy Composition")]
    public List<EnemyWaveData> enemyTypes = new List<EnemyWaveData>();
    
    [Header("Spawn Settings")]
    public SpawnFormationType formationType = SpawnFormationType.Circle;
    public float spawnRadius = 10f;
    public float spawnDelay = 0.2f;
    public bool spawnAllAtOnce;
    
    [Header("Wave Modifiers")]
    public float healthMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float damageMultiplier = 1f;
    
    [Header("Special Effects")]
    public bool useSpawnEffect = true;
    public UnityEngine.Color swarmColor = UnityEngine.Color.red;
    public GameObject customSpawnEffect;
    
    [Header("Audio")]
    public AudioClip spawnSound;
    
    public int GetTotalEnemyCount()
    {
        int total = 0;
        foreach (var enemy in enemyTypes)
        {
            total += enemy.count;
        }
        return total;
    }
}

[System.Serializable]
public class EnemyWaveData
{
    public GameObject enemyPrefab;
    public int count = 5;
    [Range(0f, 1f)]
    public float spawnPriority = 0.5f;
}

public enum SpawnFormationType
{
    Circle,
    Line,
    Grid,
    Random,
    Spiral,
    VFormation,
    Surrounding
}

