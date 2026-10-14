using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
#endif

public class SceneButtonGenerator : MonoBehaviour
{
    [Header("UI Parent & Prefab")]
    [Tooltip("Parent transform tempat tombol akan ditempatkan (misal: Panel dengan Vertical/Grid Layout Group)")]
    [SerializeField]
    [FormerlySerializedAs("container")]
    private Transform m_Container;

    [Tooltip("Optional: Prefab UI Button kustom. Jika dikosongkan, akan memakai UI Button default Unity.")]
    [SerializeField]
    [FormerlySerializedAs("buttonPrefab")]
    private GameObject m_ButtonPrefab;

#if UNITY_EDITOR
    private const string c_NoisePattern = @"(rewrite|otherdev|dev|_|\d+|\[\d+\])";
    private const string c_CamelBoundaryPattern = @"(?<=[a-z])(?=[A-Z])";
    private const string c_WhitespacePattern = @"\s+";
    private const string c_Space = " ";
    private const string c_ButtonPrefix = "Btn_";
    private const string c_UndoGenerateButton = "Generate Scene Button";
    private const string c_EventSystemName = "EventSystem";
    private const string c_UndoCreateEventSystem = "Create EventSystem";

    [ContextMenu("Generate Scene Buttons")]
    public void GenerateButtonsInEditor()
    {
        if (m_Container == null)
        {
            Debug.LogError("[SceneButtonGenerator] Tolong assign 'Container' terlebih dahulu di Inspector!");
            return;
        }

        // 1. Pastikan EventSystem ada di Scene
        EnsureEventSystemExists();

        // 2. Hapus tombol-tombol lama
        while (m_Container.childCount > 0)
        {
            Undo.DestroyObjectImmediate(m_Container.GetChild(0).gameObject);
        }

        // 3. Ambil daftar scene dari Build Settings
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        if (scenes.Length == 0)
        {
            Debug.LogWarning("[SceneButtonGenerator] Tidak ada scene di Build Settings!");
            return;
        }

        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (!scene.enabled) continue;

            string rawName = System.IO.Path.GetFileNameWithoutExtension(scene.path);

            // 4. Clean Nama Scene
            string pattern = c_NoisePattern;
            string cleanedName = Regex.Replace(rawName, pattern, "", RegexOptions.IgnoreCase);
            cleanedName = Regex.Replace(cleanedName, c_CamelBoundaryPattern, c_Space);
            cleanedName = Regex.Replace(cleanedName, c_WhitespacePattern, c_Space).Trim();

            if (string.IsNullOrWhiteSpace(cleanedName))
            {
                cleanedName = rawName;
            }

            // 5. Instantiate Button
            GameObject newButtonObj;
            if (m_ButtonPrefab != null)
            {
                newButtonObj = (GameObject)PrefabUtility.InstantiatePrefab(m_ButtonPrefab, m_Container);
            }
            else
            {
                newButtonObj = DefaultControls.CreateButton(new DefaultControls.Resources());
                newButtonObj.transform.SetParent(m_Container, false);
            }

            newButtonObj.name = c_ButtonPrefix + rawName;

            newButtonObj.TryGetComponent<Image>(out Image btnImage);
            if (btnImage != null)
            {
                btnImage.raycastTarget = true;
            }

            // 6. Set Teks
            Text legacyText = newButtonObj.GetComponentInChildren<Text>();
            if (legacyText != null)
            {
                legacyText.text = cleanedName;
                legacyText.raycastTarget = false;
            }
            else
            {
                TMPro.TMP_Text tmpText = newButtonObj.GetComponentInChildren<TMPro.TMP_Text>();
                if (tmpText != null)
                {
                    tmpText.text = cleanedName;
                    tmpText.raycastTarget = false;
                }
            }

            // 7. Pasang SceneLoader & daftarkan Event
            newButtonObj.TryGetComponent<SceneLoader>(out SceneLoader loader);
            if (loader == null)
            {
                loader = newButtonObj.AddComponent<SceneLoader>();
            }
            loader.SetTargetSceneName(rawName);

            newButtonObj.TryGetComponent<Button>(out Button btnComponent);
            if (btnComponent != null)
            {
                btnComponent.interactable = true;

                int eventCount = btnComponent.onClick.GetPersistentEventCount();
                for (int i = eventCount - 1; i >= 0; i--)
                {
                    UnityEventTools.RemovePersistentListener(btnComponent.onClick, i);
                }

                // Tambahkan event persistent baru dari instance SceneLoader yang sudah valid
                UnityEventTools.AddPersistentListener(btnComponent.onClick, loader.LoadTargetScene);
            }

            Undo.RegisterCreatedObjectUndo(newButtonObj, c_UndoGenerateButton);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Selesai! Tombol berhasil diperbarui tanpa Missing Script.");
    }

    private void EnsureEventSystemExists()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject(c_EventSystemName);
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(eventSystemObj, c_UndoCreateEventSystem);
            Debug.Log("[SceneButtonGenerator] EventSystem otomatis dibuat.");
        }
    }
#endif
}