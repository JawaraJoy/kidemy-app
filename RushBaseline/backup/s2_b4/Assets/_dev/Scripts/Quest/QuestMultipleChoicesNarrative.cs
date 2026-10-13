using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestMultipleChoicesNarrative : QuestMultipleChoices
    {
        [Header("Data")]

        [Header("Components")]
        [SerializeField] [FormerlySerializedAs("narrativeText")] private TMP_Text m_NarrativeText;

        private SO_QuestMultipleChoiceNarrative m_DataMultipleChoiceNarrative;

        protected override void Start()
        {
            base.Start();

            if (!m_DataMultipleChoiceNarrative)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestMultipleChoiceNarrative");

            if (m_NarrativeText)
                m_NarrativeText.text = m_DataMultipleChoiceNarrative.Narrative;
        }

        public override void Setup()
        {
            m_DataMultipleChoiceNarrative = m_Data as SO_QuestMultipleChoiceNarrative;

            base.Setup();
        }

        /// <summary>
        /// Returns all VoiceRequests needed by this quest.
        /// Reads question text from 'data' and voice_id from 'character'.
        /// </summary>
        public override VoiceRequest[] GetVoiceRequests()
        {
            Setup();

            VoiceRequest[] voiceRequests = base.GetVoiceRequests();

            if (voiceRequests.Length > 0 && m_Data != null && m_Data.Question != null && !string.IsNullOrEmpty(m_Data.Question.Text))
            {
                string textVoice = m_Data.Question.Text;
                
                TMP_Text narrative = GetComponentInChildren<TMP_Text>();
                
                if (narrative && !string.IsNullOrEmpty(narrative.text))
                    textVoice = narrative.text + "." + textVoice;

                voiceRequests[0] = new VoiceRequest
                {
                    id = name + "_question",
                    text = textVoice,
                    voice_id = GetVoiceId()
                };
            }

            return voiceRequests;
        }
    }
}