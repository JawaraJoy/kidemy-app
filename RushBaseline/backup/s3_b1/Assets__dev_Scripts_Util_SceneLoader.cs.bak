using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [HideInInspector]
    public string targetSceneName;

    public void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log("[SceneLoader] Loading Scene: " + targetSceneName);
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("[SceneLoader] Target Scene Name belum diatur!");
        }
    }
}