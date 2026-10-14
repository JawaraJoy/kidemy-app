using UnityEngine;
using System;

namespace EduGame
{
    [Serializable]
    public class VoiceRequest
    {
        private const string c_DefaultFormat = "wav";

        public string id;
        public string text;
        public string voice_id;
        public string format = c_DefaultFormat;
    }
}