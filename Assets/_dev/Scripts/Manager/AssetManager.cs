using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.InteropServices;

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

        public static AssetManager Instance { get; private set; }

        [Header("Debug Settings")]
        [Tooltip("Check this in the Inspector to force API downloads and overwrite existing files.")]
        [SerializeField] private bool forceRedownload = false;

        private List<VoiceRequest> voiceRequests = new List<VoiceRequest>();
        private bool currentRequestFinished = false;
        private VoiceRequest activeRequest;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Adds a VoiceRequest to the download queue.
        /// </summary>
        public void AddVoiceRequest(VoiceRequest request)
        {
            if (request == null) return;

            // Prevent duplicates in queue based on ID
            if (!voiceRequests.Exists(r => r.id == request.id))
            {
                voiceRequests.Add(request);
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
            int totalRequests = voiceRequests.Count;

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

            foreach (VoiceRequest req in voiceRequests)
            {
                string fileName = $"{req.id}.mp3";
                string localPath = Path.Combine(Application.persistentDataPath, fileName);

                // Skip download ONLY if forceRedownload is false AND file already exists
                if (!forceRedownload && File.Exists(localPath))
                {
                    Debug.Log($"[AssetManager] Voice '{req.id}' already cached. Skipping download.");
                    processedCount++;

#if UNITY_WEBGL && !UNITY_EDITOR
                JS_UpdateVoiceProgress(processedCount, totalRequests);
#endif
                    continue;
                }

                if (forceRedownload && File.Exists(localPath))
                {
                    Debug.Log($"[AssetManager] [DEBUG] Voice '{req.id}' exists, but 'forceRedownload' is active. Overwriting...");
                }

                activeRequest = req;
                currentRequestFinished = false;

                // Trigger API request
                GameManager.Instance.API.RequestVoice(
                    text: req.text,
                    voiceId: req.voice_id,
                    onSuccess: OnVoiceSuccess,
                    onFailure: OnVoiceFailure
                );

                // Wait for current request & file writing process to complete
                yield return new WaitUntil(() => currentRequestFinished);

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

            if (GameManager.Instance != null)
            {
                Debug.Log("[AssetManager] All assets ready! Launching game via GameManager.Instance.Play()...");
                GameManager.Instance.Play();
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
                    StartCoroutine(Routine_DownloadAndCacheAudio(responseData.audio_url, activeRequest?.id));
                }
                else
                {
                    Debug.LogError($"[AssetManager] audio_url was null or empty for ID '{activeRequest?.id}'.");
                    currentRequestFinished = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetManager] Failed to parse success JSON for ID '{activeRequest?.id}': {e.Message}");
                currentRequestFinished = true;
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
                        Debug.LogError($"[AssetManager] Validation Error (422) for Voice ID '{activeRequest?.id}': {detail.msg} (Type: {detail.type})");
                    }
                }
                else
                {
                    Debug.LogError($"[AssetManager] API Request failed for Voice ID '{activeRequest?.id}': {errorJsonResponse}");
                }
            }
            catch
            {
                Debug.LogError($"[AssetManager] API Request failed for Voice ID '{activeRequest?.id}': {errorJsonResponse}");
            }

            currentRequestFinished = true; // Unblock loop so remaining downloads can continue
        }

        // --- File Downloading & Overwriting ---

        private IEnumerator Routine_DownloadAndCacheAudio(string audioUrl, string voiceId)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(audioUrl))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string fileName = $"{voiceId}.mp3";
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

            currentRequestFinished = true;
        }

        /// <summary>
        /// Fetches a stored audio file from cache as an AudioClip.
        /// </summary>
        public void GetCachedAudio(string voiceId, Action<AudioClip> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(Routine_GetCachedAudio(voiceId, onSuccess, onError));
        }

        private IEnumerator Routine_GetCachedAudio(string voiceId, Action<AudioClip> onSuccess, Action<string> onError)
        {
            string fileName = $"{voiceId}.mp3";
            string localPath = Path.Combine(Application.persistentDataPath, fileName);

            if (!File.Exists(localPath))
            {
                string errorMessage = $"[AssetManager] Audio file for ID '{voiceId}' does not exist in local cache.";
                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
                yield break;
            }

            string fileUri = "file://" + localPath;

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUri, AudioType.MPEG))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    onSuccess?.Invoke(clip);
                }
                else
                {
                    string errorMessage = $"[AssetManager] Failed to load cached clip for ID '{voiceId}': {www.error}";
                    Debug.LogError(errorMessage);
                    onError?.Invoke(errorMessage);
                }
            }
        }
    }
}