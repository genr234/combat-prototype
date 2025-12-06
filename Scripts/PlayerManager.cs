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
            OnHealthChanged();
        }
    }
    [SerializeField] public int health = 100;
    public int maxHealth = 100;
    
    private int level;
    private bool onCooldown;
    private bool isInvincible;
    public float initialInvincibilitySeconds = 1f;

    public GameObject weaponHolder;
    public GameObject startingWeapon;
    private GameObject equippedWeapon;

    // HUD Controller reference
    private PlayerHudController hudController;

    private void Awake()
    {
        // Ensure health is initialized
        health = Mathf.Max(0, health);
    }

    private void Start()
    {
        // Find HUD Controller
        hudController = FindFirstObjectByType<PlayerHudController>();
        if (hudController != null)
        {
            hudController.maxHealth = maxHealth;
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
        
        print("Player took " + damage + " damage. Current health: " + Health);

        // Flash health bar on damage
        if (hudController != null)
        {
            hudController.FlashHealthBar();
        }

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

    private IEnumerator DamageCooldownCoroutine(int milliseconds)
    {
        var seconds = milliseconds / 1000f;
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

    private void OnHealthChanged()
    {
        // Health changed - HUD controller updates automatically in Update()
        Debug.Log($"Health changed: {health}/{maxHealth}");
    }

    private void Die()
    {
        Debug.Log("Player has died.");
        SceneManager.LoadScene("Scenes/WorldView");
    }
}
