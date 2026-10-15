using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.InteropServices;
using UnityEngine.Serialization;

namespace EduGame
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Runtime.InteropServices;

    public class AssetManager : MonoBehaviour
    {
        // WebGL IndexedDB Sync Plugin Call
        [DllImport("__Internal")]
        private static extern void JS_FileSystem_Sync();

        // JS Loading Bridge Imports
        [DllImport("__Internal")]
        private static extern void JS_UpdateVoiceProgress(int current, int total);

        [DllImport("__Internal")]
        private static extern void JS_OnVoiceDownloadComplete();

        private static AssetManager s_Instance;
        

        [Header("Debug Settings")]
        [Tooltip("Check this in the Inspector to force API downloads and overwrite existing files.")]
        [SerializeField] [FormerlySerializedAs("forceRedownload")] private bool m_ForceRedownload = false;

        private List<VoiceRequest> m_VoiceRequests = new List<VoiceRequest>();
        private bool m_CurrentRequestFinished = false;
        private VoiceRequest m_ActiveRequest;
        private GameManager m_GameManager;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
            TryGetComponent<GameManager>(out m_GameManager);
        }

        /// <summary>
        /// Adds a VoiceRequest to the download queue.
        /// </summary>
        public void AddVoiceRequest(VoiceRequest request)
        {
            if (request == null) return;

            // Prevent duplicates in queue based on ID
            if (!m_VoiceRequests.Exists(r => r.id == request.id))
            {
                m_VoiceRequests.Add(request);
            }
        }

        /// <summary>
        /// Processes queued requests and downloads missing audio files into IndexedDB cache.
        /// </summary>
        /// <param name="onComplete">Optional callback triggered when processing finishes.</param>
        public void DownloadMissingVoices(Action onComplete = null)
        {
            StartCoroutine(Routine_DownloadMissingVoices(onComplete));
        }

        private IEnumerator Routine_DownloadMissingVoices(Action onComplete)
        {
            int totalRequests = m_VoiceRequests.Count;

            if (totalRequests == 0)
            {
                Debug.Log("[AssetManager] No voice requests in queue.");
                ReportComplete(onComplete);
                yield break;
            }

            int processedCount = 0;

#if UNITY_WEBGL && !UNITY_EDITOR
        JS_UpdateVoiceProgress(0, totalRequests);
#endif

            foreach (VoiceRequest req in m_VoiceRequests)
            {
                string fileName = $"{req.id}.mp3";
                string localPath = Path.Combine(Application.persistentDataPath, fileName);

                // Skip download ONLY if forceRedownload is false AND file already exists
                if (!m_ForceRedownload && File.Exists(localPath))
                {
                    Debug.Log($"[AssetManager] Voice '{req.id}' already cached. Skipping download.");
                    processedCount++;

#if UNITY_WEBGL && !UNITY_EDITOR
                JS_UpdateVoiceProgress(processedCount, totalRequests);
#endif
                    continue;
                }

                if (m_ForceRedownload && File.Exists(localPath))
                {
                    Debug.Log($"[AssetManager] [DEBUG] Voice '{req.id}' exists, but 'forceRedownload' is active. Overwriting...");
                }

                m_ActiveRequest = req;
                m_CurrentRequestFinished = false;

                // Trigger API request
                m_GameManager.API.RequestVoice(
                    text: req.text,
                    voiceId: req.voice_id,
                    onSuccess: OnVoiceSuccess,
                    onFailure: OnVoiceFailure
                );

                // Wait for current request & file writing process to complete
                yield return new WaitUntil(() => m_CurrentRequestFinished);

                processedCount++;

#if UNITY_WEBGL && !UNITY_EDITOR
            JS_UpdateVoiceProgress(processedCount, totalRequests);
#endif
            }

            Debug.Log("[AssetManager] All voice downloads finished!");
            ReportComplete(onComplete);
        }

        private void ReportComplete(Action onComplete)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
        JS_OnVoiceDownloadComplete();
#endif

            onComplete?.Invoke();

            if (m_GameManager != null)
            {
                Debug.Log("[AssetManager] All assets ready! Launching game via GameManager.Instance.Play()...");
                m_GameManager.Play();
            }
            else
            {
                Debug.LogError("[AssetManager] Could not start game: GameManager.Instance is null!");
            }
        }

        // --- API Callbacks ---

        private void OnVoiceSuccess(string jsonResponse)
        {
            try
            {
                VoiceResult responseData = JsonUtility.FromJson<VoiceResult>(jsonResponse);

                if (!string.IsNullOrEmpty(responseData?.audio_url))
                {
                    StartCoroutine(Routine_DownloadAndCacheAudio(responseData.audio_url, m_ActiveRequest?.id));
                }
                else
                {
                    Debug.LogError($"[AssetManager] audio_url was null or empty for ID '{m_ActiveRequest?.id}'.");
                    m_CurrentRequestFinished = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetManager] Failed to parse success JSON for ID '{m_ActiveRequest?.id}': {e.Message}");
                m_CurrentRequestFinished = true;
            }
        }

        private void OnVoiceFailure(string errorJsonResponse)
        {
            try
            {
                VoiceResultError validationError = JsonUtility.FromJson<VoiceResultError>(errorJsonResponse);

                if (validationError?.detail != null && validationError.detail.Length > 0)
                {
                    foreach (var detail in validationError.detail)
                    {
                        Debug.LogError($"[AssetManager] Validation Error (422) for Voice ID '{m_ActiveRequest?.id}': {detail.msg} (Type: {detail.type})");
                    }
                }
                else
                {
                    Debug.LogError($"[AssetManager] API Request failed for Voice ID '{m_ActiveRequest?.id}': {errorJsonResponse}");
                }
            }
            catch
            {
                Debug.LogError($"[AssetManager] API Request failed for Voice ID '{m_ActiveRequest?.id}': {errorJsonResponse}");
            }

            m_CurrentRequestFinished = true; // Unblock loop so remaining downloads can continue
        }

        // --- File Downloading & Overwriting ---

        private IEnumerator Routine_DownloadAndCacheAudio(string audioUrl, string voiceId)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(audioUrl))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string fileName = $"{voiceId}.wav";
                    string localPath = Path.Combine(Application.persistentDataPath, fileName);

                    // File.WriteAllBytes automatically overwrites existing files
                    File.WriteAllBytes(localPath, www.downloadHandler.data);
                    Debug.Log($"[AssetManager] Successfully cached/overwrote audio for ID '{voiceId}': {localPath}");

#if UNITY_WEBGL && !UNITY_EDITOR
                JS_FileSystem_Sync();
#endif
                }
                else
                {
                    Debug.LogError($"[AssetManager] Failed downloading raw file from {audioUrl}: {www.error}");
                }
            }

            m_CurrentRequestFinished = true;
        }

        // <summary>
        /// Fetches a stored WAV audio file from cache as an AudioClip directly without UnityWebRequest.
        /// </summary>
        public void GetCachedAudio(string voiceId, Action<AudioClip> onSuccess, Action<string> onError = null)
        {
            // Make sure file extension matches what you save (.wav)
            string fileName = $"{voiceId}.wav";
            string localPath = Path.Combine(Application.persistentDataPath, fileName);

            if (!File.Exists(localPath))
            {
                string errorMessage = $"[AssetManager] Audio file for ID '{voiceId}' does not exist in local cache.";
                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
                return;
            }

            try
            {
                // Direct C# read from WebGL virtual memory (MEMFS / IndexedDB)
                byte[] wavBytes = File.ReadAllBytes(localPath);
                AudioClip clip = WavUtility.ToAudioClip(wavBytes, voiceId);

                if (clip != null)
                {
                    onSuccess?.Invoke(clip);
                }
                else
                {
                    string errorMessage = $"[AssetManager] Failed to parse WAV bytes for ID '{voiceId}'.";
                    Debug.LogError(errorMessage);
                    onError?.Invoke(errorMessage);
                }
            }
            catch (Exception e)
            {
                string errorMessage = $"[AssetManager] Exception reading cached file '{voiceId}': {e.Message}";
                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
            }
        }
    }
}