using UnityEngine;
using System.Collections;

namespace DefaultNamespace
{
    public class ShootingEffects : MonoBehaviour
    {
        [Header("References")]
        public Transform muzzlePoint;
        public Camera playerCamera;

        [Header("Screen Shake Settings")]
        public float defaultShakeForce = 0.1f;
        public float defaultShakeDuration = 0.1f;

        [Header("Floating Damage Numbers")]
        public GameObject damageNumberPrefab;
        public float damageNumberFloatSpeed = 2f;
        public float damageNumberLifetime = 1f;

        [Header("Projectile Trail")]
        public float trailDuration = 0.2f;

        private Vector3 originalCameraPosition;
        private bool isShaking = false;
        private Coroutine shakeCoroutine;
        private Coroutine recoilCoroutine;

        private void Start()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        /// <summary>
        /// Spawn muzzle flash effect at the muzzle point
        /// </summary>
        public void SpawnMuzzleFlash(WeaponConfig weapon)
        {
            if (weapon.muzzleFlashPrefab != null && muzzlePoint != null)
            {
                var flash = Instantiate(weapon.muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
                Destroy(flash, 0.15f);
            }
            else
            {
                // Create procedural muzzle flash
                CreateProceduralMuzzleFlash(weapon);
            }
        }

        private void CreateProceduralMuzzleFlash(WeaponConfig weapon)
        {
            if (muzzlePoint == null) return;

            var flashObj = new GameObject("MuzzleFlash");
            flashObj.transform.position = muzzlePoint.position;
            flashObj.transform.rotation = muzzlePoint.rotation;

            // Add light for flash effect
            var flashLight = flashObj.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = weapon.muzzleFlashColor;
            flashLight.intensity = 3f;
            flashLight.range = 5f;

            // Create particle effect
            var ps = flashObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.1f;
            main.startSpeed = 10f;
            main.startSize = 0.3f;
            main.startColor = weapon.muzzleFlashColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.1f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

            Destroy(flashObj, 0.2f);
        }

        /// <summary>
        /// Spawn impact effect at hit point
        /// </summary>
        public void SpawnImpactEffect(Vector3 position, Vector3 normal, WeaponConfig weapon)
        {
            if (weapon.impactEffectPrefab != null)
            {
                var rotation = Quaternion.LookRotation(normal);
                var impact = Instantiate(weapon.impactEffectPrefab, position, rotation);
                Destroy(impact, 1f);
            }
            else
            {
                CreateProceduralImpactEffect(position, normal, weapon);
            }
        }

        private void CreateProceduralImpactEffect(Vector3 position, Vector3 normal, WeaponConfig weapon)
        {
            var impactObj = new GameObject("ImpactEffect");
            impactObj.transform.position = position;
            impactObj.transform.rotation = Quaternion.LookRotation(normal);

            // Add light flash
            var impactLight = impactObj.AddComponent<Light>();
            impactLight.type = LightType.Point;
            impactLight.color = weapon.weaponColor;
            impactLight.intensity = 2f;
            impactLight.range = 3f;

            // Create particle system
            var ps = impactObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.4f;
            main.startSpeed = 5f;
            main.startSize = 0.2f;
            main.startColor = weapon.weaponColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.5f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.1f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

            Destroy(impactObj, 0.5f);
        }

        /// <summary>
        /// Create projectile trail from origin to hit point
        /// </summary>
        public void CreateProjectileTrail(Vector3 start, Vector3 end, WeaponConfig weapon)
        {
            StartCoroutine(AnimateProjectileTrail(start, end, weapon));
        }

        private IEnumerator AnimateProjectileTrail(Vector3 start, Vector3 end, WeaponConfig weapon)
        {
            var trailObj = new GameObject("ProjectileTrail");
            var line = trailObj.AddComponent<LineRenderer>();
            
            line.positionCount = 2;
            line.startWidth = 0.08f;
            line.endWidth = 0.02f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = weapon.weaponColor;
            line.endColor = weapon.weaponColor * 0.5f;

            var distance = Vector3.Distance(start, end);
            var duration = distance / weapon.projectileSpeed;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                
                var currentEnd = Vector3.Lerp(start, end, t);
                var currentStart = Vector3.Lerp(start, end, Mathf.Max(0, t - 0.3f));
                
                line.SetPosition(0, currentStart);
                line.SetPosition(1, currentEnd);

                yield return null;
            }

            // Fade out
            var fadeTime = 0.1f;
            elapsed = 0f;
            var startColor = line.startColor;
            var endColor = line.endColor;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                var alpha = 1f - (elapsed / fadeTime);
                line.startColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
                line.endColor = new Color(endColor.r, endColor.g, endColor.b, alpha);
                yield return null;
            }

            Destroy(trailObj);
        }

        /// <summary>
        /// Perform screen shake effect
        /// </summary>
        public void DoScreenShake(float force, float duration)
        {
            if (playerCamera == null) return;
            
            if (shakeCoroutine != null)
                StopCoroutine(shakeCoroutine);
            
            shakeCoroutine = StartCoroutine(ScreenShakeCoroutine(force, duration));
        }

        public void DoScreenShake(WeaponConfig weapon)
        {
            DoScreenShake(weapon.screenShakeForce, weapon.screenShakeDuration);
        }

        private IEnumerator ScreenShakeCoroutine(float force, float duration)
        {
            isShaking = true;
            var camTransform = playerCamera.transform;
            var originalLocalPos = camTransform.localPosition;

            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = elapsed / duration;
                var currentForce = force * (1f - progress); // Decay over time

                var shakeOffset = new Vector3(
                    Random.Range(-1f, 1f) * currentForce,
                    Random.Range(-1f, 1f) * currentForce,
                    0
                );

                camTransform.localPosition = originalLocalPos + shakeOffset;
                yield return null;
            }

            camTransform.localPosition = originalLocalPos;
            isShaking = false;
        }

        /// <summary>
        /// Perform camera recoil effect
        /// </summary>
        public void DoCameraRecoil(float force, float duration)
        {
            if (playerCamera == null) return;

            if (recoilCoroutine != null)
                StopCoroutine(recoilCoroutine);

            recoilCoroutine = StartCoroutine(CameraRecoilCoroutine(force, duration));
        }

        public void DoCameraRecoil(WeaponConfig weapon)
        {
            DoCameraRecoil(weapon.recoilForce, weapon.recoilDuration);
        }

        private IEnumerator CameraRecoilCoroutine(float force, float duration)
        {
            var camTransform = playerCamera.transform;
            var originalLocalPos = camTransform.localPosition;
            var recoilOffset = -camTransform.forward * force;

            var elapsed = 0f;
            var halfDuration = duration * 0.3f;

            // Recoil back
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / halfDuration;
                camTransform.localPosition = Vector3.Lerp(originalLocalPos, originalLocalPos + recoilOffset, t);
                yield return null;
            }

            // Return to original
            elapsed = 0f;
            var returnDuration = duration * 0.7f;
            var recoiledPos = camTransform.localPosition;

            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / returnDuration;
                t = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
                camTransform.localPosition = Vector3.Lerp(recoiledPos, originalLocalPos, t);
                yield return null;
            }

            camTransform.localPosition = originalLocalPos;
        }

        /// <summary>
        /// Create floating damage number at position
        /// </summary>
        public void CreateFloatingDamageNumber(Vector3 position, int damage, bool isCritical = false)
        {
            StartCoroutine(AnimateFloatingDamageNumber(position, damage, isCritical));
        }

        private IEnumerator AnimateFloatingDamageNumber(Vector3 worldPosition, int damage, bool isCritical)
        {
            // Create a world-space floating text using a simple approach
            var damageObj = new GameObject("DamageNumber");
            damageObj.transform.position = worldPosition + Vector3.up * 1.5f;

            // Use TextMesh for 3D text (simpler than UI)
            var textMesh = damageObj.AddComponent<TextMesh>();
            textMesh.text = damage.ToString();
            textMesh.fontSize = isCritical ? 48 : 36;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = isCritical ? Color.yellow : Color.white;
            textMesh.characterSize = 0.1f;

            // Make it face the camera
            var meshRenderer = damageObj.GetComponent<MeshRenderer>();
            meshRenderer.material = new Material(Shader.Find("GUI/Text Shader"));

            var elapsed = 0f;
            var startPos = damageObj.transform.position;
            var randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));

            while (elapsed < damageNumberLifetime)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / damageNumberLifetime;

                // Float upward
                damageObj.transform.position = startPos + randomOffset + Vector3.up * (t * damageNumberFloatSpeed);

                // Face camera
                if (playerCamera != null)
                {
                    damageObj.transform.LookAt(damageObj.transform.position + playerCamera.transform.forward);
                }

                // Fade out
                var color = textMesh.color;
                color.a = 1f - t;
                textMesh.color = color;

                // Scale effect for critical hits
                if (isCritical)
                {
                    var scale = 1f + Mathf.Sin(t * Mathf.PI * 4) * 0.2f * (1f - t);
                    damageObj.transform.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            Destroy(damageObj);
        }

        /// <summary>
        /// Create explosion effect
        /// </summary>
        public void CreateExplosionEffect(Vector3 position, float radius, WeaponConfig weapon)
        {
            var explosionObj = new GameObject("Explosion");
            explosionObj.transform.position = position;

            // Add explosion light
            var explosionLight = explosionObj.AddComponent<Light>();
            explosionLight.type = LightType.Point;
            explosionLight.color = Color.Lerp(weapon.weaponColor, Color.red, 0.5f);
            explosionLight.intensity = 5f;
            explosionLight.range = radius * 2f;

            // Create particle system for explosion
            var ps = explosionObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = radius * 2f;
            main.startSize = 0.5f;
            main.startColor = new ParticleSystem.MinMaxGradient(Color.yellow, Color.red);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.3f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0));

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

            // Screen shake for explosion
            DoScreenShake(weapon.screenShakeForce * 2f, weapon.screenShakeDuration * 2f);

            StartCoroutine(FadeLight(explosionLight, 0.5f));
            Destroy(explosionObj, 1f);
        }

        private IEnumerator FadeLight(Light light, float duration)
        {
            var startIntensity = light.intensity;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                light.intensity = Mathf.Lerp(startIntensity, 0, elapsed / duration);
                yield return null;
            }
        }

        /// <summary>
        /// Hit stop effect (brief slow motion)
        /// </summary>
        public void DoHitStop(float duration)
        {
            if (duration <= 0) return;
            StartCoroutine(HitStopCoroutine(duration));
        }

        private IEnumerator HitStopCoroutine(float duration)
        {
            Time.timeScale = 0.1f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Play shooting sound
        /// </summary>
        public void PlayShootSound(WeaponConfig weapon)
        {
            if (weapon.shootSound != null)
            {
                AudioSource.PlayClipAtPoint(weapon.shootSound, transform.position, weapon.shootVolume);
            }
        }

        /// <summary>
        /// Play all effects for a shot
        /// </summary>
        public void PlayAllShootEffects(WeaponConfig weapon, Vector3 hitPoint, Vector3 hitNormal, bool didHit)
        {
            SpawnMuzzleFlash(weapon);
            DoScreenShake(weapon);
            DoCameraRecoil(weapon);
            PlayShootSound(weapon);

            if (muzzlePoint != null)
            {
                var endPoint = didHit ? hitPoint : muzzlePoint.position + muzzlePoint.forward * weapon.range;
                CreateProjectileTrail(muzzlePoint.position, endPoint, weapon);
            }

            if (didHit)
            {
                SpawnImpactEffect(hitPoint, hitNormal, weapon);
            }
        }
    }
}

