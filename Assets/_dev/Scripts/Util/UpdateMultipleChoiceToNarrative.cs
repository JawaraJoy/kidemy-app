using UnityEngine;
using UnityEditor;
using EduGame;

public class UpdateMultipleChoiceToNarrative : EditorWindow
{
    private const string MenuItemName = "Assets/Update to Narrative";

    // 1. Context Menu Action
    [MenuItem(MenuItemName, false, 2000)]
    public static void UpdateSelectedToNarrative()
    {
        // Get all selected assets in the Project window
        Object[] selectedObjects = Selection.objects;

        int convertedCount = 0;

        foreach (Object obj in selectedObjects)
        {
            // Replace 'SO1' and 'SO2' with your actual class names
            if (obj is SO_QuestMultipleChoice oldSO && !(obj is SO_QuestMultipleChoiceNarrative)) 
            {
                string path = AssetDatabase.GetAssetPath(oldSO);

                // 1. Create temporary memory instance of SO2
                SO_QuestMultipleChoiceNarrative newSO = ScriptableObject.CreateInstance<SO_QuestMultipleChoiceNarrative>();

                // 2. Deep-copy all serialized fields from SO1 to SO2
                CopyMatchingSerializedProperties(oldSO, newSO);

                // 3. (Optional) Initialize new SO2-specific default fields here
                // newSO.myNewField = 100;

                // 4. Overwrite existing asset file or create new one
                // Option A: Overwrite asset at original path
                string assetName = oldSO.name;
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(newSO, path);

                // Option B: To keep originals safely, save to a new path instead:
                // string newPath = Path.Combine(Path.GetDirectoryName(path), assetName + "_Upgraded.asset");
                // AssetDatabase.CreateAsset(newSO, newPath);

                convertedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Successfully converted {convertedCount} ScriptableObject(s) to SO_QuestMultipleChoiceNarrative!");
    }

    // Custom helper to copy properties safely across different types
    private static void CopyMatchingSerializedProperties(Object source, Object destination)
    {
        SerializedObject sourceSO = new SerializedObject(source);
        SerializedObject destSO = new SerializedObject(destination);

        SerializedProperty prop = sourceSO.GetIterator();
        
        // Enter children to iterate through all fields
        if (prop.NextVisible(true))
        {
            do
            {
                // Skip internal script pointer (don't overwrite newSO's script type back to SO1)
                if (prop.name == "m_Script") continue;

                // Find matching property in target SO2
                SerializedProperty targetProp = destSO.FindProperty(prop.name);
                if (targetProp != null && targetProp.propertyType == prop.propertyType)
                {
                    destSO.CopyFromSerializedProperty(prop);
                }
            }
            while (prop.NextVisible(false));
        }

        destSO.ApplyModifiedPropertiesWithoutUndo();
    }

    // 2. Validation Rule (Controls menu visibility / active state)
    [MenuItem(MenuItemName, true)]
    private static bool ValidateUpdateSelectedToNarrative()
    {
        if (Selection.objects == null || Selection.objects.Length == 0)
            return false;

        foreach (Object obj in Selection.objects)
        {
            if (obj != null && obj.GetType() == typeof(SO_QuestMultipleChoice))
            {
                return true;
            }
        }

        return false;
    }
}