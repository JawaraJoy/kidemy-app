using UnityEngine;
using UnityEditor;
using EduGame;

public static class QuestConverter
{
    private const string MenuItemName = "Assets/Convert Quest to Narrative";

    [MenuItem(MenuItemName, false, 2000)]
    private static void ConvertQuestToNarrative()
    {
        Object[] selectedObjects = Selection.objects;
        int convertedCount = 0;

        // Find the MonoScript reference for QuestMultipleChoicesNarrative
        MonoScript targetScript = FindScriptAsset(typeof(QuestMultipleChoicesNarrative));
        if (targetScript == null)
        {
            Debug.LogError("Could not find script file for 'QuestMultipleChoicesNarrative'!");
            return;
        }

        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab")) continue;

            // Load prefab contents safely in isolation
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);

            // Find all components of strict type QuestMultipleChoices (ignoring already-converted ones)
            QuestMultipleChoices[] components = prefabRoot.GetComponentsInChildren<QuestMultipleChoices>(true);
            bool modified = false;

            foreach (QuestMultipleChoices comp in components)
            {
                // Strict check so we don't process components already converted to QuestMultipleChoicesNarrative
                if (comp.GetType() == typeof(QuestMultipleChoices))
                {
                    // Swap m_Script reference directly on the component
                    SerializedObject so = new SerializedObject(comp);
                    SerializedProperty scriptProperty = so.FindProperty("m_Script");

                    scriptProperty.objectReferenceValue = targetScript;
                    so.ApplyModifiedProperties();

                    modified = true;
                    convertedCount++;
                }
            }

            if (modified)
            {
                // Save updated prefab back to disk
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Successfully converted {convertedCount} QuestMultipleChoices component(s) to QuestMultipleChoicesNarrative!");
    }

    private static MonoScript FindScriptAsset(System.Type type)
    {
        string[] guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script != null && script.GetClass() == type)
            {
                return script;
            }
        }
        return null;
    }

    // Validation rule (Only enables the context menu item if a valid Prefab with QuestMultipleChoices is selected)
    [MenuItem(MenuItemName, true)]
    private static bool ValidateConvertQuestToNarrative()
    {
        if (Selection.objects == null || Selection.objects.Length == 0) return false;

        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path != null && path.EndsWith(".prefab"))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    QuestMultipleChoices comp = prefab.GetComponentInChildren<QuestMultipleChoices>(true);
                    // Check if it has a QuestMultipleChoices component that isn't ALREADY a Narrative instance
                    if (comp != null && comp.GetType() == typeof(QuestMultipleChoices))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}