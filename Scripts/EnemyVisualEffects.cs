using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using GameObject = UnityEngine.GameObject;
using MonoBehaviour = UnityEngine.MonoBehaviour;

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
    public bool shakeOnDamage = true;
    public float shakeMagnitude = 0.1f;
    public float shakeDuration = 0.15f;
    
    [Header("Knockback Settings")]
    public bool enableKnockback = true;
    public float knockbackRecoverySpeed = 5f;
    
    [Header("Blood/Hit Particles")]
    public GameObject hitParticlesPrefab;
    public Color hitParticleColor = new Color(0.8f, 0.1f, 0.1f);
    public int hitParticleCount = 10;
    
    [Header("Death Effects")]
    public GameObject deathParticles;
    public bool explodeOnDeath = true;
    public float explosionForce = 5f;
    
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Renderer[] renderers;
    private Color[] originalColors;
    private EnemyManager enemyManager;
    private Rigidbody rb;
    private bool isSpawning = false;
    private float spawnTimer = 0f;
    private bool isShaking = false;
    private Coroutine shakeCoroutine;
    
    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        
        for (var i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material != null)
                originalColors[i] = renderers[i].material.color;
        }
        
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
        enemyManager = GetComponent<EnemyManager>();
        rb = GetComponent<Rigidbody>();
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
            var progress = Mathf.Clamp01(spawnTimer / spawnDuration);
            var scaleValue = spawnScaleCurve.Evaluate(progress);
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
        if (!isSpawning)
        {
            try
            {
                if (flashOnDamage)
                {
                    StopCoroutine("DamageFlash");
                    StartCoroutine(DamageFlash());
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[EnemyVisualEffects] Error in DamageFlash: {ex.Message}");
            }
            
            try
            {
                if (shakeOnDamage && !isShaking)
                {
                    if (shakeCoroutine != null)
                        StopCoroutine(shakeCoroutine);
                    shakeCoroutine = StartCoroutine(DamageShake());
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[EnemyVisualEffects] Error in DamageShake: {ex.Message}");
            }
            
            try
            {
                SpawnHitParticles();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[EnemyVisualEffects] Error in SpawnHitParticles: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Apply knockback force to the enemy
    /// </summary>
    public void PlayKnockback(Vector3 direction, float force)
    {
        if (!enableKnockback) return;
        
        if (rb != null)
        {
            // Use rigidbody for physics-based knockback
            rb.AddForce(direction.normalized * force, ForceMode.Impulse);
        }
        else
        {
            // Use transform-based knockback
            StartCoroutine(TransformKnockback(direction.normalized * force * 0.1f));
        }
    }
    
    private IEnumerator TransformKnockback(Vector3 offset)
    {
        var knockbackTarget = transform.position + offset;
        var elapsed = 0f;
        var duration = 0.2f;
        var startPos = transform.position;
        
        // Move to knockback position
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / duration;
            t = 1f - Mathf.Pow(1f - t, 3f); // Ease out
            transform.position = Vector3.Lerp(startPos, knockbackTarget, t);
            yield return null;
        }
    }
    
    private void SpawnHitParticles()
    {
        if (hitParticlesPrefab != null)
        {
            var particles = Instantiate(hitParticlesPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(particles, 1f);
        }
        else
        {
            // Create procedural hit particles
            CreateProceduralHitParticles();
        }
    }
    
    private void CreateProceduralHitParticles()
    {
        try
        {
            var particleObj = new GameObject("HitParticles");
            particleObj.transform.position = transform.position + Vector3.up * 0.5f;
            
            var ps = particleObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.4f;
            main.startSpeed = 3f;
            main.startSize = 0.15f;
            main.startColor = hitParticleColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1f;
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)hitParticleCount) });
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0));
            
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Particles/Standard Unlit");
            if (shader != null)
            {
                renderer.material = new Material(shader);
            }
            else
            {
                Debug.LogWarning("[EnemyVisualEffects] Particles/Standard Unlit shader not found, using default material");
            }
            
            Destroy(particleObj, 0.5f);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[EnemyVisualEffects] Error creating procedural hit particles: {ex.Message}");
        }
    }
    
    private IEnumerator DamageShake()
    {
        isShaking = true;
        var elapsed = 0f;
        var startPos = transform.localPosition;
        
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            var progress = elapsed / shakeDuration;
            var currentMagnitude = shakeMagnitude * (1f - progress);
            
            var shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * currentMagnitude,
                0,
                Random.Range(-1f, 1f) * currentMagnitude
            );
            
            transform.localPosition = startPos + shakeOffset;
            yield return null;
        }
        
        transform.localPosition = startPos;
        isShaking = false;
    }
    
    private System.Collections.IEnumerator DamageFlash()
    {
        try
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    try
                    {
                        renderer.material.color = damageFlashColor;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[EnemyVisualEffects] Error setting damage flash color: {ex.Message}");
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[EnemyVisualEffects] Error in DamageFlash loop: {ex.Message}");
        }
        
        yield return new WaitForSeconds(flashDuration);
        
        try
        {
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null)
                {
                    try
                    {
                        renderers[i].material.color = originalColors[i];
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[EnemyVisualEffects] Error restoring material color: {ex.Message}");
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[EnemyVisualEffects] Error in DamageFlash restore: {ex.Message}");
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
        var particleCount = 10;
        for (var i = 0; i < particleCount; i++)
        {
            var particle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            particle.transform.position = transform.position;
            particle.transform.localScale = Vector3.one * 0.2f;
            
            var renderer = particle.GetComponent<Renderer>();
            if (renderer != null && originalColors.Length > 0)
            {
                renderer.material.color = originalColors[0];
            }
            
            var rb = particle.AddComponent<Rigidbody>();
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
            var remaining = 0;
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

