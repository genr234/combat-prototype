using UnityEngine;
using GameObject = UnityEngine.GameObject;
using MonoBehaviour = UnityEngine.MonoBehaviour;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EnemyVisualEffects : MonoBehaviour
{
    [Header("Spawn Effects")]
    public bool playSpawnAnimation = true;
    public float spawnDuration = 0.5f;
    public AnimationCurve spawnScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Damage Effects")]
    public bool flashOnDamage = true;
    public Color damageFlashColor = Color.red;
    public float flashDuration = 0.1f;
    
    [Header("Death Effects")]
    public GameObject deathParticles;
    public bool explodeOnDeath = true;
    public float explosionForce = 5f;
    
    private Vector3 originalScale;
    private Renderer[] renderers;
    private Color[] originalColors;
    private EnemyManager enemyManager;
    private bool isSpawning = false;
    private float spawnTimer = 0f;
    
    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material != null)
                originalColors[i] = renderers[i].material.color;
        }
        
        originalScale = transform.localScale;
        enemyManager = GetComponent<EnemyManager>();
    }
    
    private void Start()
    {
        if (playSpawnAnimation)
        {
            isSpawning = true;
            spawnTimer = 0f;
            transform.localScale = Vector3.zero;
        }
    }
    
    private void Update()
    {
        if (isSpawning)
        {
            spawnTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(spawnTimer / spawnDuration);
            float scaleValue = spawnScaleCurve.Evaluate(progress);
            transform.localScale = originalScale * scaleValue;
            
            if (progress >= 1f)
            {
                isSpawning = false;
                transform.localScale = originalScale;
            }
        }
    }
    
    public void PlayDamageEffect()
    {
        if (flashOnDamage && !isSpawning)
        {
            StopAllCoroutines();
            StartCoroutine(DamageFlash());
        }
    }
    
    private System.Collections.IEnumerator DamageFlash()
    {
        foreach (var renderer in renderers)
        {
            if (renderer != null && renderer.material != null)
                renderer.material.color = damageFlashColor;
        }
        
        yield return new WaitForSeconds(flashDuration);
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
                renderers[i].material.color = originalColors[i];
        }
    }
    
    private void OnDestroy()
    {
        if (deathParticles != null)
        {
            Instantiate(deathParticles, transform.position, Quaternion.identity);
        }
        
        if (explodeOnDeath)
        {
            CreateDeathExplosion();
        }
    }
    
    private void CreateDeathExplosion()
    {
        int particleCount = 10;
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            particle.transform.position = transform.position;
            particle.transform.localScale = Vector3.one * 0.2f;
            
            Renderer renderer = particle.GetComponent<Renderer>();
            if (renderer != null && originalColors.Length > 0)
            {
                renderer.material.color = originalColors[0];
            }
            
            Rigidbody rb = particle.AddComponent<Rigidbody>();
            rb.AddExplosionForce(explosionForce * 100f, transform.position, 5f);
            
            Destroy(particle, 1f);
        }
    }
}

public class SwarmUIManager : MonoBehaviour
{
    [UnityEngine.Header("UI References")]
    public TextMeshProUGUI waveNumberText;
    public TextMeshProUGUI waveNameText;
    public TextMeshProUGUI enemiesRemainingText;
    public TextMeshProUGUI nextWaveTimerText;
    public Slider waveProgressBar;
    public GameObject waveCompletePanel;
    
    [UnityEngine.Header("Settings")]
    public bool autoHideCompletePanel = true;
    public float panelDisplayTime = 2f;
    
    private EnemySwarmManager swarmManager;
    private float nextWaveTimer = 0f;
    private bool countingDown = false;
    
    private void Start()
    {
        swarmManager = FindObjectOfType<EnemySwarmManager>();
        
        if (swarmManager != null)
        {
            swarmManager.OnWaveStart.AddListener(OnWaveStarted);
            swarmManager.OnWaveComplete.AddListener(OnWaveCompleted);
            swarmManager.OnAllWavesComplete.AddListener(OnAllWavesCompleted);
        }
        
        if (waveCompletePanel != null)
            waveCompletePanel.SetActive(false);
    }
    
    private void Update()
    {
        if (swarmManager == null) return;
        
        UpdateUI();
        
        if (countingDown && nextWaveTimerText != null)
        {
            nextWaveTimer -= Time.deltaTime;
            if (nextWaveTimer > 0)
            {
                nextWaveTimerText.text = $"Next Wave in: {Mathf.CeilToInt(nextWaveTimer)}s";
            }
            else
            {
                nextWaveTimerText.text = "";
                countingDown = false;
            }
        }
    }
    
    private void UpdateUI()
    {
        if (enemiesRemainingText != null)
        {
            int remaining = 0;
            var enemies = FindObjectsOfType<EnemyManager>();
            remaining = enemies.Length;
            enemiesRemainingText.text = $"Enemies: {remaining}";
        }
    }
    
    private void OnWaveStarted(int waveIndex)
    {
        countingDown = false;
        
        if (waveNumberText != null)
            waveNumberText.text = $"Wave {waveIndex + 1}";
        
        if (waveNameText != null && swarmManager.swarmWaves.Count > waveIndex)
            waveNameText.text = swarmManager.swarmWaves[waveIndex].swarmName;
        
        if (waveCompletePanel != null)
            waveCompletePanel.SetActive(false);
    }
    
    private void OnWaveCompleted(int waveIndex)
    {
        if (waveCompletePanel != null)
        {
            waveCompletePanel.SetActive(true);
            
            if (autoHideCompletePanel)
            {
                Invoke(nameof(HideCompletePanel), panelDisplayTime);
            }
        }
        
        nextWaveTimer = swarmManager.timeBetweenWaves;
        countingDown = true;
    }
    
    private void OnAllWavesCompleted()
    {
        if (waveNumberText != null)
            waveNumberText.text = "Victory!";
        
        if (waveNameText != null)
            waveNameText.text = "All Waves Defeated";
        
        countingDown = false;
        if (nextWaveTimerText != null)
            nextWaveTimerText.text = "";
    }
    
    private void HideCompletePanel()
    {
        if (waveCompletePanel != null)
            waveCompletePanel.SetActive(false);
    }
}

