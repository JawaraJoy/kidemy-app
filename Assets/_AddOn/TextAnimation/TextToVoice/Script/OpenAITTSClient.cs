using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AddOn.TextToSpeech
{
    public class OpenAITTSClient
    {
        private const string c_Endpoint = "https://api.openai.com/v1/audio/speech";

        private readonly VoiceGenerationSettings m_Settings;

        public OpenAITTSClient(VoiceGenerationSettings settings)
        {
            m_Settings = settings;
        }

        [Serializable]
        private class SpeechRequest
        {
            public string model;
            public string input;
            public string voice;
            public string response_format;
        }

        public async Task<byte[]> GenerateSpeechAsync(VoiceGenerationRequest request)
        {
            SpeechRequest body = new SpeechRequest()
            {
                model = request.Model,
                input = request.Text,
                voice = request.Voice,
                response_format = "mp3"
            };

            string json = JsonUtility.ToJson(body);

            using UnityWebRequest webRequest = new UnityWebRequest(c_Endpoint, UnityWebRequest.kHttpVerbPOST);

            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {m_Settings.ApiKey}");

            UnityWebRequestAsyncOperation operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(webRequest.error);
                Debug.LogError(webRequest.downloadHandler.text);
                return null;
            }

            return webRequest.downloadHandler.data;
        }
    }
}