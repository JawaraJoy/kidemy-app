using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EduGame
{
    // use this to create a ScriptableObject that holds a reference to a SceneAsset and its name
    // so no need to hardcode the scene name in the code, just drag and drop the SceneAsset in the inspector
    [CreateAssetMenu(fileName = "SceneConfig", menuName = "OtherDev/SceneConfig")]
    public class SceneConfig : ScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField]
        private SceneAsset m_SceneAsset;
#endif

        private string m_SceneName;
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_SceneAsset != null)
                m_SceneName = m_SceneAsset.name;
        }
#endif

        // then call this method to load the scene by name
        public void Goto()
        {
            if (string.IsNullOrEmpty(m_SceneName))
            {
                Debug.LogError($"{name} has no scene assigned.");
                return;
            }

            SceneManager.LoadScene(m_SceneName);
        }
    }
}

