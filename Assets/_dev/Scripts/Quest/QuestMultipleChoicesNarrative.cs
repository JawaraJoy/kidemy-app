using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestMultipleChoicesNarrative : QuestMultipleChoices
    {
        private const string c_SentenceEnd = ".";
        private const string c_QuestionSegment = "_question";

        [SerializeField] [FormerlySerializedAs("narrativeText")] private TMP_Text m_NarrativeText;

        protected override void Start()
        {
            base.Start();
            if (m_NarrativeText && Data is SO_QuestMultipleChoiceNarrative narrative)
                m_NarrativeText.text = narrative.Narrative;
        }

        /// <summary>
        /// Returns all VoiceRequests needed by this quest.
        /// Reads question text from 'data' and voice_id from 'character'.
        /// </summary>
        public override VoiceRequest[] GetVoiceRequests()
        {
            VoiceRequest[] voiceRequests = base.GetVoiceRequests();

            if (voiceRequests.Length > 0 && m_Data != null && m_Data.Question != null && !string.IsNullOrEmpty(m_Data.Question.Text))
            {
                string textVoice = m_Data.Question.Text;
                
                string narrative = Data is SO_QuestMultipleChoiceNarrative config ? config.Narrative : null;
                if (string.IsNullOrEmpty(narrative) && m_NarrativeText) narrative = m_NarrativeText.text;
                if (!string.IsNullOrEmpty(narrative)) textVoice = narrative + c_SentenceEnd + textVoice;

                voiceRequests[0] = new VoiceRequest
                {
                    id = name + c_QuestionSegment,
                    text = textVoice,
                    voice_id = GetVoiceId()
                };
            }

            return voiceRequests;
        }
    }
}