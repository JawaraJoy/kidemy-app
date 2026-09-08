using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestMultipleChoice", menuName = "EduGame/Quest/MultipleChoice")]
    public class SO_QuestMultipleChoice : SO_Quest
    {
        [Space]
        [SerializeField, Min(1)]
        private int m_RepeatSoundQuest = 1;
        [SerializeField]
        private AudioClip m_SoundQuest;
        [SerializeField]
        private AnimatorClipConfig m_HowManySoundConfig;
        [Space]
        [SerializeField] private QuestUtilLabelChoice[] choices;
        [Space()]
        
        [Header("Rule")]
        [SerializeField] private float timer = 0;
        [SerializeField] private bool randomizeChoice;

        public QuestUtilLabelChoice[] Choices => randomizeChoice ? GetRandomizedChoices() : choices;
        public float Timer => timer;
        public int TotalAnswer => GetTotalAnswer();

        [NonSerialized] private int totalAnswer = 0;
        [NonSerialized] private QuestUtilLabelChoice[] randomizedChoices;

        public AudioClip SoundQuest => m_SoundQuest;
        public AnimatorClipConfig HowManySoundConfig => m_HowManySoundConfig;
        public int RepeatSoundQuest => m_RepeatSoundQuest;

        public override VoiceRequest[] GetVoiceRequests()
        {
            List<VoiceRequest> requests = new List<VoiceRequest>(base.GetVoiceRequests());

            if (choices == null)
                return requests.ToArray();

            for (int i = 0; i < choices.Length; i++)
            {
                if (choices[i] != null && !string.IsNullOrEmpty(choices[i].Text))
                {
                    requests.Add(new VoiceRequest
                    {
                        id = name + "_choice_" + i,
                        text = choices[i].Text,
                        voice_id = GetVoiceId()
                    });
                }
            }

            return requests.ToArray();
        }

        public override bool HasVoiceClip(string requestId)
        {
            if (TryGetChoiceIndex(requestId, out int index))
                return choices[index] != null && choices[index].Audio != null;

            return base.HasVoiceClip(requestId);
        }

        public override void AssignVoiceClip(string requestId, AudioClip clip)
        {
            if (TryGetChoiceIndex(requestId, out int index))
            {
                if (choices[index] != null)
                    choices[index].SetAudio(clip);

                return;
            }

            base.AssignVoiceClip(requestId, clip);
        }

        private bool TryGetChoiceIndex(string requestId, out int index)
        {
            index = -1;

            if (string.IsNullOrEmpty(requestId) || !requestId.Contains("_choice_"))
                return false;

            index = StringHelper.ExtractId(requestId);
            return choices != null && index >= 0 && index < choices.Length;
        }

        QuestUtilLabelChoice[] GetRandomizedChoices()
        {
            if(randomizedChoices == null)
            {
                randomizedChoices = new QuestUtilLabelChoice[choices.Length]; 

                System.Random random = new System.Random();
                randomizedChoices = choices.OrderBy(x => random.Next()).ToArray();
            }

            return randomizedChoices;
        }

        int GetTotalAnswer()
        {
            if(totalAnswer == 0)
            {
                foreach (var choice in choices)
                {
                    if(choice.IsAnswer)
                        totalAnswer++;
                }
            }

            return totalAnswer;
        }
    }
}
