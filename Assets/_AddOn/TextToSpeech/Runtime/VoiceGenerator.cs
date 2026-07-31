using System;
using System.IO;
using System.Threading.Tasks;
using AddOn.TextAnimation;

namespace AddOn.TextToSpeech
{
    public class VoiceGenerator
    {
        private readonly ITextToSpeechProvider m_Provider;

        public VoiceGenerator(ITextToSpeechProvider provider)
        {
            m_Provider = provider;
        }

        public async Task<string> GenerateAsync(
            DialogueConfig dialogue,
            string outputFolder,
            string voice)
        {
            if (dialogue == null)
                throw new ArgumentNullException(nameof(dialogue));

            Directory.CreateDirectory(outputFolder);

            string outputPath = Path.Combine(
                outputFolder,
                $"{dialogue.Identic.Id}.mp3");

            VoiceGenerationRequest request =
                new VoiceGenerationRequest(
                    dialogue.Identic.Id,
                    dialogue.GetPlainText(),
                    voice);

            await m_Provider.GenerateAsync(
                request,
                outputPath);

            return outputPath;
        }
    }
}