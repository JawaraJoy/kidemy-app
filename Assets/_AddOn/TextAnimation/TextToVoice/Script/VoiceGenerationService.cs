using AddOn.TextAnimation;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace AddOn.TextToSpeech
{
    public class VoiceGenerationService
    {
        private readonly OpenAITTSClient m_Client;

        public VoiceGenerationService(OpenAITTSClient client)
        {
            m_Client = client;
        }

        /// <summary>
        /// Generate voice from dialogue and save it.
        /// </summary>
        public async Task GenerateAsync(
            DialogueConfig dialogue,
            string outputFolder)
        {
            if (dialogue == null)
                throw new ArgumentNullException(nameof(dialogue));

            if (string.IsNullOrWhiteSpace(outputFolder))
                throw new ArgumentException(nameof(outputFolder));

            string text = dialogue.GetPlainText();

            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning($"Dialogue '{dialogue.name}' has no text.");
                return;
            }

            byte[] audioBytes =
                await m_Client.GenerateSpeechAsync(text);

            if (audioBytes == null || audioBytes.Length == 0)
            {
                Debug.LogError("Failed to generate voice.");
                return;
            }

            SaveAudio(dialogue, outputFolder, audioBytes);
        }

        private void SaveAudio(
            DialogueConfig dialogue,
            string outputFolder,
            byte[] bytes)
        {
            Directory.CreateDirectory(outputFolder);

            string filePath = Path.Combine(
                outputFolder,
                $"{dialogue.Identic.Id}.mp3");

            File.WriteAllBytes(filePath, bytes);

            Debug.Log($"Voice saved : {filePath}");
        }
    }
}