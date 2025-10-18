using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int health = 100;
    private int level;
    private bool onCooldown;
    
    public GameObject weaponHolder;
    public GameObject startingWeapon;
    private GameObject equippedWeapon;
    
    public GameObject healthTextObject;
    private TextMeshProUGUI healthText;
    private void Start()
    {
        healthText = healthTextObject.GetComponent<TextMeshProUGUI>();
        EquipWeapon(startingWeapon);
        print(equippedWeapon);
    }

    private void Update()
    {
        healthText.text = "Health: " + health;
        weaponHolder.transform.localPosition = new Vector3(0.6f, 0.18f, -0.2f);
        weaponHolder.transform.localRotation = Quaternion.identity;
        if (equippedWeapon)
        {
            equippedWeapon.transform.position = weaponHolder.transform.position;
            equippedWeapon.transform.rotation = weaponHolder.transform.rotation;
        }
    }
    
    public void EquipWeapon(GameObject weapon)
    {
        Debug.Log("Equipped weapon: " + weapon.name);
        equippedWeapon = Instantiate(weapon, weaponHolder.transform.position, Quaternion.identity);
    }

    public void TakeDamage(int damage, int cooldown = 0)
    {
        switch (cooldown)
        {
            case > 0 when !onCooldown:
                TakeDamage(damage);
                Task.Delay(cooldown).ContinueWith(_ => onCooldown = false);
                onCooldown = true;
                break;
            case 0:
            {
                health -= damage;
                onCooldown = false;
                if (health <= 0)
                {
                    Die();
                }

                break;
            }
        }
    }
    
    private static void Die()
    {
        Debug.Log("Player has died.");
        // Add respawn or game over logic here
    }
}
