using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LockOnReticle : MonoBehaviour
{
    [Header("References")]
    public TargetingSystem targetingSystem;
        
    [Header("Reticle Settings")]
    public float baseScale = 1f;
    public float maxScale = 1.5f;
    public float scaleSpeed = 5f;
    public float rotationSpeed = 90f;
        
    [Header("Visual Settings")]
    public Color lockedColor = Color.green;
    public Color unlockedColor = Color.red;
    public float colorTransitionSpeed = 5f;

    private Image reticleImage;
    private CanvasGroup canvasGroup;
    private float targetScale;
    private Color targetColor;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        // Get or add Image component for reticle visual
        reticleImage = GetComponent<Image>();
        if (reticleImage == null)
        {
            reticleImage = gameObject.AddComponent<Image>();
        }

        // Get or add CanvasGroup for fade effects
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Find TargetingSystem if not assigned
        if (targetingSystem == null)
        {
            targetingSystem = FindFirstObjectByType<TargetingSystem>();
        }

        targetScale = baseScale;
        targetColor = unlockedColor;
    }

    private void Update()
    {
        if (!targetingSystem) return;

        // Update position - follow cursor always, follow target when locked
        UpdateReticlePosition();
        // Update color based on lock-on state
        targetColor = targetingSystem.IsLockedOn ? lockedColor : unlockedColor;
        var currentColor = reticleImage.color;
        reticleImage.color = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorTransitionSpeed);

        // Update scale based on lock-on strength
        targetScale = Mathf.Lerp(baseScale, maxScale, targetingSystem.LockOnStrength);
        transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(targetScale, targetScale, 1f), Time.deltaTime * scaleSpeed);

        // Rotate reticle
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // Update alpha when locked
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetingSystem.IsLockedOn ? 1f : 0.7f, Time.deltaTime * 3f);
    }

    private void UpdateReticlePosition()
    {
        if (!cam) return;

        var rectTransform = GetComponent<RectTransform>();
        if (!rectTransform) return;

        Vector3 targetScreenPos;

        // If locked on a target, follow the target
        if (targetingSystem.IsLockedOn && targetingSystem.CurrentTarget)
        {
            var targetWorldPos = targetingSystem.CurrentTarget.position;
            targetScreenPos = cam.WorldToScreenPoint(targetWorldPos);
        }
        else
        {
            // Otherwise, follow the mouse cursor
            targetScreenPos = Mouse.current.position.ReadValue();
        }

        // Smoothly move towards target position
        var currentPos = rectTransform.position;
        rectTransform.position = Vector3.Lerp(currentPos, targetScreenPos, Time.deltaTime * 8f);
    }
}
