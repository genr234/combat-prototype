using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 20f;
    public int damage = 10;
    public float lifetime = 5f;
    public float knockbackForce = 100f;

    [Header("Visual Effects")]
    public GameObject impactEffectPrefab;
    public TrailRenderer trailRenderer;
    public Color projectileColor = Color.red;

    private Rigidbody rb;
    private bool hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
        
        // Setup visual appearance
        SetupVisuals();
    }

    private void SetupVisuals()
    {
        // If there's no mesh renderer, create a simple sphere visual
        if (GetComponent<MeshRenderer>() == null)
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.mesh = CreateSphereMesh();
            }
            
            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = projectileColor;
            material.SetFloat("_Metallic", 0.5f);
            material.SetFloat("_Smoothness", 0.8f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", projectileColor * 2f);
            meshRenderer.material = material;
        }
        
        // Add trail if not present
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
                SetupTrail();
            }
        }
        
        // Add light for glow effect
        var pointLight = gameObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = projectileColor;
        pointLight.intensity = 2f;
        pointLight.range = 3f;
    }

    private void SetupTrail()
    {
        if (trailRenderer != null)
        {
            trailRenderer.time = 0.3f;
            trailRenderer.startWidth = 0.2f;
            trailRenderer.endWidth = 0.05f;
            trailRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            trailRenderer.material.color = projectileColor;
            
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(projectileColor, 0.0f), 
                    new GradientColorKey(projectileColor, 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1.0f, 0.0f), 
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            trailRenderer.colorGradient = gradient;
        }
    }

    private Mesh CreateSphereMesh()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = sphere.GetComponent<MeshFilter>().mesh;
        Destroy(sphere);
        return mesh;
    }

    public void Launch(Vector3 direction)
    {
        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Ignore collision with enemies
        if (other.GetComponent<EnemyManager>() != null)
        {
            return;
        }

        // Check if hit player
        var playerManager = other.GetComponent<PlayerManager>();
        if (playerManager != null)
        {
            playerManager.TakeDamage(damage, (int)knockbackForce);
            hasHit = true;
            SpawnImpactEffect(transform.position);
            Destroy(gameObject);
            return;
        }

        // Hit environment
        if (other.gameObject.layer != LayerMask.NameToLayer("Enemy"))
        {
            hasHit = true;
            SpawnImpactEffect(transform.position);
            Destroy(gameObject);
        }
    }

    private void SpawnImpactEffect(Vector3 position)
    {
        if (impactEffectPrefab != null)
        {
            var impact = Instantiate(impactEffectPrefab, position, Quaternion.identity);
            Destroy(impact, 2f);
        }
        else
        {
            // Create a simple particle effect
            CreateSimpleImpact(position);
        }
    }

    private void CreateSimpleImpact(Vector3 position)
    {
        GameObject impactObj = new GameObject("Impact Effect");
        impactObj.transform.position = position;

        var ps = impactObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 5f;
        main.startSize = 0.2f;
        main.startColor = projectileColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.duration = 0.3f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        Destroy(impactObj, 1f);
    }
}

