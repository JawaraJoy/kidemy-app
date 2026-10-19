#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using EduGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RushRefactor
{
    // Alat sekali pakai: replikasi game _dev (Stage legacy) ke pola _dev2 (ChallengeConfig + scene tipis).
    // Aset legacy tidak diubah. Mengembalikan teks laporan; "ABORT" bila ada ketidakcocokan (tidak ada aset dibuat).
    public static class RushMigrate
    {
        private const string c_Dev2Root = "Assets/_dev2/Quest/Multiple Choice";
        private const string c_LayoutDir = "Assets/_dev2/Quest/Prefabs/Layouts";
        private const string c_TemplateFolder = "Island_5_Sound Quest";
        private const string c_TemplateScene = "OtherDev_Sound Quest_A_001.unity";
        private const string c_TemplateChallenge = "Configs/Challenge_Sound Quest_A.asset";

        public static string Migrate(string legacyScenePath, string gameName, int island)
        {
            string folder = c_Dev2Root + "/Island_" + island + "_" + gameName;
            string layoutPath = c_LayoutDir + "/QuestLayout_" + gameName + "_001.prefab";
            if (AssetDatabase.IsValidFolder(folder) || File.Exists(layoutPath))
                return "ABORT: target sudah ada: " + folder;

            string plan = ReadLegacy(legacyScenePath, out List<GameObject> prefabs, out Sprite background, out Object choice, out Object character);
            if (plan != null) return plan;

            string mismatch = CheckSameTree(prefabs);
            if (mismatch != null) return "ABORT: " + mismatch;

            return Build(folder, layoutPath, gameName, prefabs, background, choice, character);
        }

        private static string ReadLegacy(string scenePath, out List<GameObject> prefabs, out Sprite background, out Object choice, out Object character)
        {
            prefabs = new List<GameObject>();
            background = null; choice = null; character = null;
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            GameManager gm = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                gm = root.GetComponentInChildren<GameManager>(true);
                if (gm) break;
            }
            string error = null;
            if (gm == null) error = "ABORT: GameManager tidak ditemukan";
            else
            {
                SerializedObject so = new SerializedObject(gm);
                SerializedProperty many = so.FindProperty("m_QuestPrefabs");
                SerializedProperty one = so.FindProperty("m_QuestPrefab");
                if (many != null && many.arraySize > 0)
                {
                    for (int i = 0; i < many.arraySize; i++) prefabs.Add(AsPrefab(many.GetArrayElementAtIndex(i).objectReferenceValue));
                }
                else if (one != null && one.objectReferenceValue) prefabs.Add(AsPrefab(one.objectReferenceValue));
                if (prefabs.Count == 0 || prefabs.Contains(null)) error = "ABORT: prefab quest tidak lengkap";
            }
            if (error == null)
            {
                Quest q = prefabs[0].GetComponent<Quest>();
                SerializedObject qs = new SerializedObject(q);
                background = qs.FindProperty("m_Background").objectReferenceValue as Sprite;
                character = qs.FindProperty("m_Character").objectReferenceValue;
                SerializedProperty ci = qs.FindProperty("m_ChoiceItemPrefab");
                choice = ci != null ? ci.objectReferenceValue : null;
            }
            EditorSceneManager.CloseScene(scene, true);
            EditorSceneManager.RestoreSceneManagerSetup(setup);
            return error;
        }

        private static GameObject AsPrefab(Object o)
        {
            Component c = o as Component;
            return c ? c.gameObject : o as GameObject;
        }

        private static string Tree(Transform t, bool isRoot)
        {
            StringBuilder sb = new StringBuilder(isRoot ? "ROOT" : t.name);
            sb.Append('(').Append(t.GetComponents<Component>().Length).Append(')');
            for (int i = 0; i < t.childCount; i++) sb.Append('[').Append(Tree(t.GetChild(i), false)).Append(']');
            return sb.ToString();
        }

        private static string CheckSameTree(List<GameObject> prefabs)
        {
            string first = Tree(prefabs[0].transform, true);
            SerializedObject s0 = new SerializedObject(prefabs[0].GetComponent<Quest>());
            for (int i = 1; i < prefabs.Count; i++)
            {
                if (Tree(prefabs[i].transform, true) != first) return "pohon objek prefab " + prefabs[i].name + " berbeda dari " + prefabs[0].name;
                SerializedObject si = new SerializedObject(prefabs[i].GetComponent<Quest>());
                foreach (string f in new string[] { "m_Background", "m_Character", "m_ChoiceItemPrefab", "m_NpcController" })
                {
                    SerializedProperty a = s0.FindProperty(f); SerializedProperty b = si.FindProperty(f);
                    if (a != null && b != null && a.objectReferenceValue != b.objectReferenceValue) return "field " + f + " berbeda di " + prefabs[i].name;
                }
            }
            return null;
        }

        private static string Build(string folder, string layoutPath, string gameName, List<GameObject> prefabs, Sprite background, Object choice, Object character)
        {
            StringBuilder report = new StringBuilder();
            QuestMultipleChoices legacyMc = prefabs[0].GetComponent<QuestMultipleChoices>();
            SerializedProperty legacyChoice = legacyMc ? new SerializedObject(legacyMc).FindProperty("m_ChoiceItemPrefab") : null;
            if (legacyChoice != null) choice = legacyChoice.objectReferenceValue;
            string template = c_Dev2Root + "/" + c_TemplateFolder + "/";
            AssetDatabase.CreateFolder(c_Dev2Root, Path.GetFileName(folder));
            AssetDatabase.CreateFolder(folder, "Configs");

            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(prefabs[0]), layoutPath);
            GameObject root = PrefabUtility.LoadPrefabContents(layoutPath);
            SerializedObject ls = new SerializedObject(root.GetComponent<Quest>());
            foreach (string f in new string[] { "m_Data", "m_Background", "m_Character", "m_ChoiceItemPrefab" })
            {
                SerializedProperty p = ls.FindProperty(f);
                if (p != null) p.objectReferenceValue = null;
            }
            ls.ApplyModifiedProperties();
            root.name = Path.GetFileNameWithoutExtension(layoutPath);
            PrefabUtility.SaveAsPrefabAsset(root, layoutPath);
            PrefabUtility.UnloadPrefabContents(root);

            Object[] sos = new Object[prefabs.Count];
            for (int i = 0; i < prefabs.Count; i++)
            {
                SerializedProperty d = new SerializedObject(prefabs[i].GetComponent<Quest>()).FindProperty("m_Data");
                string np = folder + "/Configs/SO_" + gameName + "_A_0" + i + ".asset";
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(d.objectReferenceValue), np);
                sos[i] = AssetDatabase.LoadAssetAtPath<Object>(np);
            }

            string scenePath = folder + "/OtherDev_" + gameName + "_A_001.unity";
            AssetDatabase.CopyAsset(template + c_TemplateScene, scenePath);
            SceneConfig sc = ScriptableObject.CreateInstance<SceneConfig>();
            AssetDatabase.CreateAsset(sc, folder + "/SceneConfig_" + gameName + "_A_001.asset");
            SerializedObject scs = new SerializedObject(sc);
            scs.FindProperty("m_SceneAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(scenePath);
            scs.ApplyModifiedProperties();

            string cc = folder + "/Configs/Challenge_" + gameName + "_A.asset";
            AssetDatabase.CopyAsset(template + c_TemplateChallenge, cc);
            SerializedObject cs = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(cc));
            cs.FindProperty("m_Background").objectReferenceValue = background;
            cs.FindProperty("m_ChoicePrefab").objectReferenceValue = choice;
            cs.FindProperty("m_QuestLayoutPrefabs").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(layoutPath);
            cs.FindProperty("m_Character").objectReferenceValue = character;
            cs.FindProperty("m_SceneConfig").objectReferenceValue = sc;
            SerializedProperty arr = cs.FindProperty("m_QuestConfigs");
            arr.arraySize = sos.Length;
            for (int i = 0; i < sos.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = sos[i];
            cs.ApplyModifiedProperties();

            string oldGuid = AssetDatabase.AssetPathToGUID(template + c_TemplateChallenge);
            string newGuid = AssetDatabase.AssetPathToGUID(cc);
            string text = File.ReadAllText(scenePath);
            File.WriteAllText(scenePath, text.Replace(oldGuid, newGuid));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.Append("OK ").Append(gameName).Append(": ").Append(sos.Length).Append(" soal, layout=").Append(prefabs[0].GetComponent<Quest>().GetType().Name);
            report.Append(", bg=").Append(background ? background.name : "null").Append(", choice=").Append(choice ? choice.name : "null");
            report.Append(", char=").Append(character ? character.name : "null").Append(", scene=").Append(scenePath);
            return report.ToString();
        }
    }
}
#endif
