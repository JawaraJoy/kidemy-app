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
    // Layout berbeda antar soal -> satu layout prefab per bentuk (override per soal di ChallengeConfig);
    // latar berbeda antar soal -> override latar per soal.
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
            if (AssetDatabase.IsValidFolder(folder) || File.Exists(LayoutPath(gameName, 1)))
                return "ABORT: target sudah ada: " + folder;

            string error = ReadLegacy(legacyScenePath, out List<GameObject> prefabs);
            if (error != null) return error;

            error = CheckSameFields(prefabs);
            if (error != null) return "ABORT: " + error;

            return Build(folder, gameName, prefabs);
        }

        private static string LayoutPath(string gameName, int number)
        {
            return c_LayoutDir + "/QuestLayout_" + gameName + "_" + number.ToString("000") + ".prefab";
        }

        private static string ReadLegacy(string scenePath, out List<GameObject> prefabs)
        {
            prefabs = new List<GameObject>();
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
            EditorSceneManager.CloseScene(scene, true);
            EditorSceneManager.RestoreSceneManagerSetup(setup);
            return error;
        }

        private static GameObject AsPrefab(Object o)
        {
            Component c = o as Component;
            return c ? c.gameObject : o as GameObject;
        }

        private static Object ObjectField(GameObject prefab, string field)
        {
            Quest q = prefab.GetComponent<Quest>();
            SerializedProperty p = q ? new SerializedObject(q).FindProperty(field) : null;
            return p != null ? p.objectReferenceValue : null;
        }

        private static string Tree(Transform t, bool isRoot)
        {
            StringBuilder sb = new StringBuilder(isRoot ? "ROOT" : t.name);
            sb.Append('(').Append(t.GetComponents<Component>().Length).Append(')');
            for (int i = 0; i < t.childCount; i++) sb.Append('[').Append(Tree(t.GetChild(i), false)).Append(']');
            return sb.ToString();
        }

        // Karakter dan prefab pilihan harus sama antar soal (ChallengeConfig hanya punya satu).
        private static string CheckSameFields(List<GameObject> prefabs)
        {
            for (int i = 1; i < prefabs.Count; i++)
            {
                foreach (string f in new string[] { "m_Character", "m_ChoiceItemPrefab" })
                {
                    if (ObjectField(prefabs[i], f) != ObjectField(prefabs[0], f))
                        return "field " + f + " berbeda di " + prefabs[i].name;
                }
            }
            return null;
        }

        private static string Build(string folder, string gameName, List<GameObject> prefabs)
        {
            string template = c_Dev2Root + "/" + c_TemplateFolder + "/";
            AssetDatabase.CreateFolder(c_Dev2Root, Path.GetFileName(folder));
            AssetDatabase.CreateFolder(folder, "Configs");

            // satu layout per bentuk pohon yang berbeda
            List<string> signatures = new List<string>();
            List<string> layoutPaths = new List<string>();
            int[] layoutOf = new int[prefabs.Count];
            for (int i = 0; i < prefabs.Count; i++)
            {
                string sig = Tree(prefabs[i].transform, true);
                int at = signatures.IndexOf(sig);
                if (at < 0)
                {
                    signatures.Add(sig);
                    string lp = LayoutPath(gameName, signatures.Count);
                    CreateLayout(prefabs[i], lp);
                    layoutPaths.Add(lp);
                    at = signatures.Count - 1;
                }
                layoutOf[i] = at;
            }

            Object[] sos = new Object[prefabs.Count];
            for (int i = 0; i < prefabs.Count; i++)
            {
                Object data = ObjectField(prefabs[i], "m_Data");
                string np = folder + "/Configs/SO_" + gameName + "_A_0" + i + ".asset";
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(data), np);
                sos[i] = AssetDatabase.LoadAssetAtPath<Object>(np);
            }

            string scenePath = folder + "/OtherDev_" + gameName + "_A_001.unity";
            AssetDatabase.CopyAsset(template + c_TemplateScene, scenePath);
            SceneConfig sc = ScriptableObject.CreateInstance<SceneConfig>();
            AssetDatabase.CreateAsset(sc, folder + "/SceneConfig_" + gameName + "_A_001.asset");
            SerializedObject scs = new SerializedObject(sc);
            scs.FindProperty("m_SceneAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(scenePath);
            scs.ApplyModifiedProperties();

            Sprite background = ObjectField(prefabs[0], "m_Background") as Sprite;
            string cc = folder + "/Configs/Challenge_" + gameName + "_A.asset";
            AssetDatabase.CopyAsset(template + c_TemplateChallenge, cc);
            SerializedObject cs = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(cc));
            cs.FindProperty("m_Background").objectReferenceValue = background;
            cs.FindProperty("m_ChoicePrefab").objectReferenceValue = ObjectField(prefabs[0], "m_ChoiceItemPrefab");
            cs.FindProperty("m_QuestLayoutPrefabs").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(layoutPaths[0]);
            cs.FindProperty("m_Character").objectReferenceValue = ObjectField(prefabs[0], "m_Character");
            cs.FindProperty("m_SceneConfig").objectReferenceValue = sc;
            SerializedProperty arr = cs.FindProperty("m_QuestConfigs");
            arr.arraySize = sos.Length;
            for (int i = 0; i < sos.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = sos[i];

            int layoutOverrides = 0, backgroundOverrides = 0;
            SerializedProperty lov = cs.FindProperty("m_QuestLayoutOverrides");
            SerializedProperty bov = cs.FindProperty("m_BackgroundOverrides");
            lov.arraySize = prefabs.Count;
            bov.arraySize = prefabs.Count;
            for (int i = 0; i < prefabs.Count; i++)
            {
                lov.GetArrayElementAtIndex(i).objectReferenceValue = layoutOf[i] == 0 ? null : AssetDatabase.LoadAssetAtPath<Object>(layoutPaths[layoutOf[i]]);
                if (layoutOf[i] != 0) layoutOverrides++;
                Sprite bg = ObjectField(prefabs[i], "m_Background") as Sprite;
                bool differs = bg != background;
                bov.GetArrayElementAtIndex(i).objectReferenceValue = differs ? bg : null;
                if (differs) backgroundOverrides++;
            }
            cs.ApplyModifiedProperties();

            string oldGuid = AssetDatabase.AssetPathToGUID(template + c_TemplateChallenge);
            string newGuid = AssetDatabase.AssetPathToGUID(cc);
            string text = File.ReadAllText(scenePath);
            File.WriteAllText(scenePath, text.Replace(oldGuid, newGuid));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Object character = ObjectField(prefabs[0], "m_Character");
            Object choice = ObjectField(prefabs[0], "m_ChoiceItemPrefab");
            return "OK " + gameName + ": " + sos.Length + " soal, mekanik=" + prefabs[0].GetComponent<Quest>().GetType().Name
                + ", layout=" + layoutPaths.Count + " (override " + layoutOverrides + "), latar override=" + backgroundOverrides
                + ", choice=" + (choice ? choice.name : "null") + ", char=" + (character ? character.name : "null") + ", scene=" + scenePath;
        }

        private static void CreateLayout(GameObject source, string layoutPath)
        {
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), layoutPath);
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
        }
    }
}
#endif
