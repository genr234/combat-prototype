using System.Collections;
using Unity.Properties;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PlayerManager : MonoBehaviour
{
    [CreateProperty]
    public int Health
    {
        get => health;
        set
        {
            health = Mathf.Max(0, value);
            UpdateHealthUI();
        }
    }
    [SerializeField] public int health = 100;
    private int level;
    private bool onCooldown;
    private bool isInvincible;
    public float initialInvincibilitySeconds = 1f;

    public GameObject weaponHolder;
    public GameObject startingWeapon;
    private GameObject equippedWeapon;

    public UIDocument uiDocument;
    private VisualElement Root => uiDocument != null ? uiDocument.rootVisualElement : null;
    private Label healthLabel;

    private void Awake()
    {
        // Ensure health is initialized
        health = Mathf.Max(0, health);
    }

    private void Start()
    {
        if (uiDocument != null)
        {
            var root = Root;

            if (root != null)
            {
                Debug.LogWarning("Root has no children! Creating Label programmatically...");
                healthLabel = new Label($"Health: {health}/100");
                healthLabel.name = "health-text";
                root.Add(healthLabel);
            }
            else
            {
                Debug.LogWarning("Root is null!");
            }
            
        }
        else
        {
            Debug.LogWarning("UIDocument not assigned!");
        }

        if (weaponHolder == null)
        {
            Debug.LogWarning("weaponHolder is not assigned on PlayerManager.");
        }

        if (startingWeapon != null && weaponHolder != null)
        {
            EquipWeapon(startingWeapon);
            Debug.Log("Equipped weapon: " + (equippedWeapon ? equippedWeapon.name : "null after instantiate"));
        }

        if (initialInvincibilitySeconds > 0f)
        {
            StartCoroutine(InvincibilityCoroutine(initialInvincibilitySeconds));
        }
    }

    private void FixedUpdate()
    {
        if (!weaponHolder) return;

        weaponHolder.transform.localPosition = new Vector3(0.6f, 0.18f, -0.2f);
        weaponHolder.transform.localRotation = Quaternion.identity;
        if (!equippedWeapon) return;
        equippedWeapon.transform.position = weaponHolder.transform.position;
        equippedWeapon.transform.rotation = weaponHolder.transform.rotation * Quaternion.Euler(0, 180, 0);
    }

    private void EquipWeapon(GameObject weapon)
    {
        if (weapon == null)
        {
            Debug.LogWarning("Attempted to equip a null weapon.");
            return;
        }

        if (equippedWeapon) Destroy(equippedWeapon);
        Debug.Log("Equipped weapon: " + weapon.name);
        equippedWeapon = Instantiate(weapon, weaponHolder.transform.position, weaponHolder.transform.rotation, weaponHolder.transform);
    }
    
    public void TakeDamage(int damage, int cooldown = 0)
    {
        if (damage <= 0) return;
        if (isInvincible) return;
        if (onCooldown) return;

        Health -= damage;

        if (Health <= 0)
        {
            Die();
            return;
        }

        if (cooldown > 0)
        {
            StartCoroutine(DamageCooldownCoroutine(cooldown));
        }
    }

    private IEnumerator DamageCooldownCoroutine(int seconds)
    {
        onCooldown = true;
        yield return new WaitForSeconds(seconds);
        onCooldown = false;
    }
    
    private IEnumerator InvincibilityCoroutine(float seconds)
    {
        isInvincible = true;
        yield return new WaitForSeconds(seconds);
        isInvincible = false;
    }

    private void UpdateHealthUI()
    {
        if (healthLabel == null)
        {
            Debug.LogWarning("Health label is null, cannot update UI!");
            return;
        }
        
        // Update the text to reflect current health
        healthLabel.text = $"Health: {health}/100";
        
        Debug.Log($"UI Updated - Health: {health}/100");
    }

    private void Die()
    {
        Debug.Log("Player has died.");
        SceneManager.LoadScene("Scenes/WorldView");
    }
}
