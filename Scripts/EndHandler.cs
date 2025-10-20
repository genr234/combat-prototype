using UnityEngine;
using UnityEngine.SceneManagement;

public class EndHandler : MonoBehaviour
{
    public string sceneName;
    public void EndEvent()
    {
        SceneManager.LoadScene(sceneName);
    }
}
