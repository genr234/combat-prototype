using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class WorldViewController : MonoBehaviour
{
    public GameObject player;
    [System.Serializable]
    public struct LevelPosition
    {
        public GameObject levelObject;
        public string sceneName;
    }

    public List<LevelPosition> levelPositions = new List<LevelPosition> { };
    private int currentLevel = 0;
    private Vector3 targetPosition;
    
    private GameObject focusedTarget;
    private float focusedSpeed = 30f;

    private Vector3 GetPosition(int level)
    {
        return new Vector3(levelPositions[level].levelObject.transform.position.x,
            2.14f,
            (levelPositions[level].levelObject.transform.position.z - 9));
    }

    private void Focus(GameObject target)
    {
        if (focusedTarget) return;
        
        target.transform.rotation = Quaternion.Euler(0f, -90f, 0);

        focusedTarget = target;
    }
    private void Unfocus()
    {
        if (!focusedTarget) return;
        focusedTarget.transform.rotation = Quaternion.Euler(0f, -90f, 0);
        if (focusedTarget.transform.position.y > 2f)
        {
            focusedTarget.transform.position = new Vector3(focusedTarget.transform.position.x, 
                -0.09f, focusedTarget.transform.position.z);
        }

        focusedTarget = null;
    }
    
private void Start()
{
    targetPosition = GetPosition(currentLevel);
    Focus(this.levelPositions[currentLevel].levelObject);
    player.transform.position = targetPosition;

    InputSystem.actions.FindAction("Player/Move").performed += ctx =>
    {
        var inputVector = ctx.ReadValue<Vector2>();
        switch (inputVector.y)
        {
            case > 0.5f:
                if (currentLevel - 1 != currentLevel)
                {
                    Unfocus();
                }
                currentLevel = Mathf.Clamp(currentLevel - 1, 0, levelPositions.Count - 1);
                targetPosition = GetPosition(currentLevel);
                Focus(levelPositions[currentLevel].levelObject);
                break;
            case < -0.5f:
                if (currentLevel + 1 != currentLevel)
                {
                    Unfocus();
                }
                currentLevel = Mathf.Clamp(currentLevel + 1, 0, levelPositions.Count - 1);
                targetPosition = GetPosition(currentLevel);
                Focus(levelPositions[currentLevel].levelObject);
                break;
        }
    };

}

private void FixedUpdate()
{
    if (focusedTarget && Mathf.Approximately(player.transform.position.x, focusedTarget.transform.position.x)) 
    {
        focusedTarget.transform.Rotate(Vector3.up, focusedSpeed * Time.fixedDeltaTime);
        if (focusedTarget.transform.position.y < 2f)
        {
            focusedTarget.transform.position = new Vector3(focusedTarget.transform.position.x, focusedTarget.transform.position.y + 2f, focusedTarget.transform.position.z);
        }
    }
    if (Vector3.Distance(player.transform.position, targetPosition) > 0.01f)
    {
        player.transform.position = Vector3.MoveTowards(player.transform.position, targetPosition, Time.fixedDeltaTime * 25f);
    }
    InputSystem.actions.FindAction("Player/Interact").performed += ctx =>
    {
        print("Loading " + levelPositions[currentLevel].sceneName);
        SceneManager.LoadScene(levelPositions[currentLevel].sceneName);
    };
}
}
