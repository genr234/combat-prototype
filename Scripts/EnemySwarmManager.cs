using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemySwarmManager : MonoBehaviour
{
    [Header("Wave Configuration")]
    public List<SwarmConfig> swarmWaves = new List<SwarmConfig>();
    public bool autoStartWaves = true;
    public float timeBetweenWaves = 5f;
    
    [Header("Progression")]
    public bool increaseDifficultyPerWave = true;
    public float difficultyScalePerWave = 0.15f;
    
    [Header("Spawn Location")]
    public Transform spawnCenter;
    public bool spawnAroundPlayer;
    public string playerTag = "Player";
    
    [Header("Wave State")]
    [SerializeField] private int currentWaveIndex;
    [SerializeField] private int enemiesAlive;
    [SerializeField] private int totalEnemiesSpawned;
    private bool waveInProgress;
    
    [Header("Events")]
    public UnityEvent<int> OnWaveStart;
    public UnityEvent<int> OnWaveComplete;
    public UnityEvent OnAllWavesComplete;
    
    [Header("UI/Debug")]
    public bool showDebugInfo = true;
    
    private GameObject player;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    
    private void Start()
    {
        if (spawnCenter == null)
            spawnCenter = transform;
            
        if (spawnAroundPlayer)
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
        }
        
        if (autoStartWaves && swarmWaves.Count > 0)
        {
            StartCoroutine(WaveSequence());
        }
    }
    
    private void Update()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
        enemiesAlive = spawnedEnemies.Count;
        
        if (waveInProgress && enemiesAlive == 0 && totalEnemiesSpawned > 0)
        {
            CompleteWave();
        }
    }
    
    private IEnumerator WaveSequence()
    {
        while (currentWaveIndex < swarmWaves.Count)
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            SpawnWave(currentWaveIndex);
            
            while (waveInProgress)
            {
                yield return null;
            }
            
            currentWaveIndex++;
        }
        
        OnAllWavesComplete?.Invoke();
        if (showDebugInfo)
            Debug.Log("All waves completed!");
    }
    
    public void SpawnWave(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= swarmWaves.Count)
        {
            Debug.LogWarning($"Invalid wave index: {waveIndex}");
            return;
        }
        
        SwarmConfig config = swarmWaves[waveIndex];
        waveInProgress = true;
        totalEnemiesSpawned = 0;
        
        OnWaveStart?.Invoke(waveIndex);
        
        if (showDebugInfo)
            Debug.Log($"Starting Wave {waveIndex + 1}: {config.swarmName}");
        
        Vector3 center = GetSpawnCenter();
        
        if (config.spawnAllAtOnce)
        {
            SpawnAllEnemies(config, center);
        }
        else
        {
            StartCoroutine(SpawnEnemiesOverTime(config, center));
        }
    }
    
    private void SpawnAllEnemies(SwarmConfig config, Vector3 center)
    {
        List<GameObject> enemiesToSpawn = PrepareEnemyList(config);
        List<Vector3> positions = CalculateSpawnPositions(config, enemiesToSpawn.Count, center);
        
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            SpawnEnemy(enemiesToSpawn[i], positions[i], config);
        }
    }
    
    private IEnumerator SpawnEnemiesOverTime(SwarmConfig config, Vector3 center)
    {
        List<GameObject> enemiesToSpawn = PrepareEnemyList(config);
        List<Vector3> positions = CalculateSpawnPositions(config, enemiesToSpawn.Count, center);
        
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            SpawnEnemy(enemiesToSpawn[i], positions[i], config);
            yield return new WaitForSeconds(config.spawnDelay);
        }
    }
    
    private List<GameObject> PrepareEnemyList(SwarmConfig config)
    {
        List<GameObject> enemies = new List<GameObject>();
        config.enemyTypes.Sort((a, b) => a.spawnPriority.CompareTo(b.spawnPriority));
        
        foreach (var enemyData in config.enemyTypes)
        {
            for (int i = 0; i < enemyData.count; i++)
            {
                enemies.Add(enemyData.enemyPrefab);
            }
        }
        
        return enemies;
    }
    
    private List<Vector3> CalculateSpawnPositions(SwarmConfig config, int count, Vector3 center)
    {
        List<Vector3> positions;
        
        switch (config.formationType)
        {
            case SpawnFormationType.Circle:
                positions = GetCircleFormation(count, center, config.spawnRadius);
                break;
            case SpawnFormationType.Line:
                positions = GetLineFormation(count, center, config.spawnRadius);
                break;
            case SpawnFormationType.Grid:
                positions = GetGridFormation(count, center, config.spawnRadius);
                break;
            case SpawnFormationType.Spiral:
                positions = GetSpiralFormation(count, center, config.spawnRadius);
                break;
            case SpawnFormationType.VFormation:
                positions = GetVFormation(count, center, config.spawnRadius);
                break;
            case SpawnFormationType.Surrounding:
                positions = GetSurroundingFormation(count, center, config.spawnRadius);
                break;
            default:
                positions = GetRandomFormation(count, center, config.spawnRadius);
                break;
        }
        
        return positions;
    }
    
    private void SpawnEnemy(GameObject enemyPrefab, Vector3 position, SwarmConfig config)
    {
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        
        EnemyManager enemyManager = enemy.GetComponent<EnemyManager>();
        if (enemyManager != null)
        {
            float difficultyMultiplier = 1f + (currentWaveIndex * difficultyScalePerWave);
            
            if (increaseDifficultyPerWave)
            {
                enemyManager.health = Mathf.RoundToInt(enemyManager.health * config.healthMultiplier * difficultyMultiplier);
                enemyManager.speed *= config.speedMultiplier * Mathf.Sqrt(difficultyMultiplier);
                enemyManager.damage = Mathf.RoundToInt(enemyManager.damage * config.damageMultiplier * difficultyMultiplier);
            }
            else
            {
                enemyManager.health = Mathf.RoundToInt(enemyManager.health * config.healthMultiplier);
                enemyManager.speed *= config.speedMultiplier;
                enemyManager.damage = Mathf.RoundToInt(enemyManager.damage * config.damageMultiplier);
            }
            
            enemyManager.ApplyStats();
        }
        
        if (config.useSpawnEffect)
        {
            CreateSpawnEffect(position, config);
        }
        
        spawnedEnemies.Add(enemy);
        totalEnemiesSpawned++;
    }
    
    private void CreateSpawnEffect(Vector3 position, SwarmConfig config)
    {
        if (config.customSpawnEffect != null)
        {
            GameObject effect = Instantiate(config.customSpawnEffect, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        else
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.transform.position = position + Vector3.up * 0.5f;
            particle.transform.localScale = Vector3.one * 0.3f;
            
            Renderer particleRenderer = particle.GetComponent<Renderer>();
            if (particleRenderer != null)
            {
                particleRenderer.material.color = config.swarmColor;
            }
            
            Destroy(particle.GetComponent<Collider>());
            Destroy(particle, 0.5f);
        }
    }
    
    private void CompleteWave()
    {
        waveInProgress = false;
        OnWaveComplete?.Invoke(currentWaveIndex);
        
        if (showDebugInfo)
            Debug.Log($"Wave {currentWaveIndex + 1} completed!");
    }
    
    private Vector3 GetSpawnCenter()
    {
        if (spawnAroundPlayer && player != null)
            return player.transform.position;
        return spawnCenter.position;
    }
    
    #region Formation Patterns
    
    private List<Vector3> GetCircleFormation(int count, Vector3 center, float radius)
    {
        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            positions.Add(pos);
        }
        return positions;
    }
    
    private List<Vector3> GetLineFormation(int count, Vector3 center, float length)
    {
        List<Vector3> positions = new List<Vector3>();
        float spacing = length / Mathf.Max(1, count - 1);
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = center + Vector3.right * (i * spacing - length / 2f);
            positions.Add(pos);
        }
        return positions;
    }
    
    private List<Vector3> GetGridFormation(int count, Vector3 center, float size)
    {
        List<Vector3> positions = new List<Vector3>();
        int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
        float spacing = size / Mathf.Max(1, columns - 1);
        
        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int col = i % columns;
            Vector3 pos = center + new Vector3(
                col * spacing - size / 2f,
                0,
                row * spacing - size / 2f
            );
            positions.Add(pos);
        }
        return positions;
    }
    
    private List<Vector3> GetSpiralFormation(int count, Vector3 center, float maxRadius)
    {
        List<Vector3> positions = new List<Vector3>();
        float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
        
        for (int i = 0; i < count; i++)
        {
            float angle = i * goldenAngle;
            float radius = maxRadius * Mathf.Sqrt(i / (float)count);
            Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            positions.Add(pos);
        }
        return positions;
    }
    
    private List<Vector3> GetVFormation(int count, Vector3 center, float size)
    {
        List<Vector3> positions = new List<Vector3>();
        int halfCount = count / 2;
        
        for (int i = 0; i < count; i++)
        {
            float offset = (i < halfCount) ? i : (count - i - 1);
            float side = (i < halfCount) ? -1f : 1f;
            Vector3 pos = center + new Vector3(side * offset * size / halfCount, 0, -offset * size / halfCount);
            positions.Add(pos);
        }
        return positions;
    }
    
    private List<Vector3> GetSurroundingFormation(int count, Vector3 center, float radius)
    {
        List<Vector3> positions = new List<Vector3>();
        int rings = Mathf.Max(1, Mathf.CeilToInt(count / 8f));
        int enemiesPerRing = Mathf.CeilToInt(count / (float)rings);
        
        for (int ring = 0; ring < rings; ring++)
        {
            float ringRadius = radius * (ring + 1) / rings;
            int enemiesInThisRing = Mathf.Min(enemiesPerRing, count - positions.Count);
            
            for (int i = 0; i < enemiesInThisRing; i++)
            {
                float angle = i * Mathf.PI * 2f / enemiesInThisRing + (ring * 0.5f);
                Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ringRadius;
                positions.Add(pos);
            }
        }
        
        return positions;
    }
    
    private List<Vector3> GetRandomFormation(int count, Vector3 center, float radius)
    {
        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 pos = center + new Vector3(randomCircle.x, 0, randomCircle.y);
            positions.Add(pos);
        }
        return positions;
    }
    
    #endregion
    
    public void StartNextWave()
    {
        if (currentWaveIndex < swarmWaves.Count && !waveInProgress)
        {
            SpawnWave(currentWaveIndex);
        }
    }
    
    public void ResetWaves()
    {
        StopAllCoroutines();
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        spawnedEnemies.Clear();
        currentWaveIndex = 0;
        waveInProgress = false;
        enemiesAlive = 0;
        totalEnemiesSpawned = 0;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (spawnCenter == null) return;
        
        Gizmos.color = Color.yellow;
        Vector3 center = GetSpawnCenter();
        
        if (swarmWaves.Count > 0 && currentWaveIndex < swarmWaves.Count)
        {
            SwarmConfig config = swarmWaves[currentWaveIndex];
            Gizmos.DrawWireSphere(center, config.spawnRadius);
            
            List<Vector3> previewPositions = CalculateSpawnPositions(config, config.GetTotalEnemyCount(), center);
            Gizmos.color = config.swarmColor;
            foreach (var pos in previewPositions)
            {
                Gizmos.DrawWireSphere(pos, 0.5f);
            }
        }
    }
}

