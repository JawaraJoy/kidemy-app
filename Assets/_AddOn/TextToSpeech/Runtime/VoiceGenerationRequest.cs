namespace AddOn.TextToSpeech
{
    public class VoiceGenerationRequest
    {
        public string Id { get; }
        public string Text { get; }
        public string Voice { get; }

        public VoiceGenerationRequest(
            string id,
            string text,
            string voice)
        {
            Id = id;
            Text = text;
            Voice = voice;
        }
    }
}