namespace AddOn.TextToSpeech
{
    public class VoiceGenerationRequest
    {
        public string Id { get; }
        public string Text { get; }
        public string Voice { get; }
        public string Model { get; }

        public VoiceGenerationRequest(string id,
            string text,
            string voice,
            string model)
        {
            Id = id;
            Text = text;
            Voice = voice;
            Model = model;
        }
    }
}