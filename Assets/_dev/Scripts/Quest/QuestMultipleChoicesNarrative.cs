using TMPro;
using UnityEngine;

namespace EduGame
{
    public class QuestMultipleChoicesNarrative : QuestMultipleChoices
    {
        [Header("Data")]

        [Header("Components")]
        [SerializeField] private TMP_Text narrativeText;

        private SO_QuestMultipleChoiceNarrative dataMultipleChoiceNarrative;

        protected override void Start()
        {
            base.Start();

            if (!dataMultipleChoiceNarrative)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestMultipleChoiceNarrative");

            if (narrativeText)
                narrativeText.text = dataMultipleChoiceNarrative.Narrative;
        }

        public override void Setup()
        {
            dataMultipleChoiceNarrative = data as SO_QuestMultipleChoiceNarrative;

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

            if (voiceRequests.Length > 0 && data != null && data.Question != null && !string.IsNullOrEmpty(data.Question.Text))
            {
                string textVoice = data.Question.Text;
                
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