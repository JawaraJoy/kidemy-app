using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

namespace EduGame
{
    public static class VoiceAPI
    {
        private const string API_URL =
            "https://kimee-ai-stg.linkit360.ai/v1/api/audio/generate";

        public const string DefaultVoiceId =
            "n3tkGOcEribaYI5zg4pu";

#if UNITY_EDITOR

        public static void Generate(
            string text,
            string voiceId,
            string apiToken,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                onFailure?.Invoke("Text is empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(apiToken))
            {
                onFailure?.Invoke("API Token is empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(voiceId))
            {
                voiceId = DefaultVoiceId;
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(
                GenerateRoutine(
                    text,
                    voiceId,
                    apiToken,
                    onSuccess,
                    onFailure
                )
            );
        }

        private static IEnumerator GenerateRoutine(
            string text,
            string voiceId,
            string apiToken,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            // ========================================================
            // CREATE REQUEST
            // ========================================================

            VoiceRequest requestData = new VoiceRequest
            {
                text = text,
                voice_id = voiceId
            };

            string json =
                JsonUtility.ToJson(requestData);

            Debug.Log(
                $"[VoiceAPI] Requesting voice...\n" +
                $"Voice ID: {voiceId}\n" +
                $"Text: {text}"
            );

            // ========================================================
            // CREATE WEB REQUEST
            // ========================================================

            byte[] body =
                Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request =
                new UnityWebRequest(
                    API_URL,
                    UnityWebRequest.kHttpVerbPOST
                );

            request.uploadHandler =
                new UploadHandlerRaw(body);

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Authorization",
                "Bearer " + apiToken
            );

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            request.SetRequestHeader(
                "Accept",
                "application/json"
            );

            // ========================================================
            // SEND
            // ========================================================

            yield return request.SendWebRequest();

            // ========================================================
            // ERROR
            // ========================================================

            if (
                request.result !=
                UnityWebRequest.Result.Success
            )
            {
                string error =
                    $"HTTP {request.responseCode}: " +
                    $"{request.error}\n" +
                    $"Response: {request.downloadHandler.text}";

                Debug.LogError(
                    $"[VoiceAPI] Request failed:\n{error}"
                );

                onFailure?.Invoke(error);

                yield break;
            }

            // ========================================================
            // RESPONSE
            // ========================================================

            string response =
                request.downloadHandler.text;

            Debug.Log(
                $"[VoiceAPI] Response:\n{response}"
            );

            VoiceResponse voiceResponse;

            try
            {
                voiceResponse =
                    JsonUtility.FromJson<VoiceResponse>(
                        response
                    );
            }
            catch (Exception exception)
            {
                string error =
                    $"Failed to parse API response.\n" +
                    $"{exception.Message}\n" +
                    $"Response: {response}";

                Debug.LogError(
                    $"[VoiceAPI] {error}"
                );

                onFailure?.Invoke(error);

                yield break;
            }

            if (voiceResponse == null)
            {
                onFailure?.Invoke(
                    "API response could not be parsed."
                );

                yield break;
            }

            if (
                string.IsNullOrWhiteSpace(
                    voiceResponse.audio_url
                )
            )
            {
                onFailure?.Invoke(
                    "API response does not contain audio_url.\n" +
                    $"Response: {response}"
                );

                yield break;
            }

            Debug.Log(
                $"[VoiceAPI] Audio URL received:\n" +
                $"{voiceResponse.audio_url}"
            );

            onSuccess?.Invoke(
                voiceResponse.audio_url
            );
        }

#endif

        // ============================================================
        // REQUEST DATA
        // ============================================================

        [Serializable]
        private class VoiceRequest
        {
            public string text;
            public string voice_id;
        }

        // ============================================================
        // RESPONSE DATA
        // ============================================================

        [Serializable]
        private class VoiceResponse
        {
            public string audio_url;
        }
    }
}