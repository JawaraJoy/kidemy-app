using System;

namespace EduGame
{

    [Serializable]
    public class VoiceResultError
    {
        public VoiceResultErrorDetail[] detail;

        [Serializable]
        public class VoiceResultErrorDetail
        {
            public string[] loc;
            public string msg;
            public string type;
            public string input;
        }
    }
}