using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public List<GameObject> enemies = new List<GameObject> { };
    private void Start()
    {
        GameObject.Instantiate(enemies[Random.Range(0, enemies.Count)], transform.position, Quaternion.Euler(0, Random.Range(0, 360), 0));
    }

    private void FixedUpdate()
    {
        
    }
}
