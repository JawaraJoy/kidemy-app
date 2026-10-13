using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    [FormerlySerializedAs("targetSceneName")]
    private string m_TargetSceneName;

    public void SetTargetSceneName(string sceneName)
    {
        m_TargetSceneName = sceneName;
    }

    public void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(m_TargetSceneName))
        {
            Debug.Log("[SceneLoader] Loading Scene: " + m_TargetSceneName);
            SceneManager.LoadScene(m_TargetSceneName);
        }
        else
        {
            Debug.LogError("[SceneLoader] Target Scene Name belum diatur!");
        }
    }
}