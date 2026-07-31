using System.Threading.Tasks;

namespace AddOn.TextToSpeech
{
    public interface ITextToSpeechProvider
    {
        Task GenerateAsync(
            VoiceGenerationRequest request,
            string outputPath);
    }
}