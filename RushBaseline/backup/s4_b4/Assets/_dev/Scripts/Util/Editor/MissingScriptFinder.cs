using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class MissingScriptFinder : Editor
{
    [MenuItem("Tools/Find All Missing Scripts")]
    public static void FindMissingScripts()
    {
        int missingCount = 0;

        // 1. Search Active Scene
        GameObject[] sceneObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject go in sceneObjects)
        {
            Component[] components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingCount++;
                    Debug.LogError($"[Missing Script] Found on Scene GameObject: '{go.name}'", go);
                }
            }
        }

        // 2. Search Prefabs in Assets Folder
        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                Component[] components = prefab.GetComponentsInChildren<Component>(true);
                foreach (Component c in components)
                {
                    if (c == null)
                    {
                        missingCount++;
                        Debug.LogError($"[Missing Script] Found on Prefab Asset: '{prefab.name}' at path '{path}'", prefab);
                    }
                }
            }
        }

        if (missingCount == 0)
        {
            Debug.Log("🎉 No missing scripts found anywhere in Scene or Prefabs!");
        }
        else
        {
            Debug.LogWarning($"⚠️ Total Missing Scripts Found: {missingCount}");
        }
    }
}