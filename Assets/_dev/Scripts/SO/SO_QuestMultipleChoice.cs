using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_QuestMultipleChoice", menuName = "EduGame/Quest/MultipleChoice")]
    public class SO_QuestMultipleChoice : SO_Quest
    {
        private const string c_ChoiceSegment = "_choice_";

        [Space]
        [SerializeField, Min(1)]
        private int m_RepeatSoundQuest = 1;
        [SerializeField]
        private AudioClip m_SoundQuest;
        [SerializeField]
        private AnimatorClipConfig m_HowManySoundConfig;
        [Space]
        [SerializeField] [FormerlySerializedAs("choices")] private QuestUtilLabelChoice[] m_Choices;
        [Space()]
        
        [Header("Rule")]
        [SerializeField] [FormerlySerializedAs("timer")] private float m_Timer = 0;
        [SerializeField] [FormerlySerializedAs("randomizeChoice")] private bool m_RandomizeChoice;

        public QuestUtilLabelChoice[] Choices => m_RandomizeChoice ? GetRandomizedChoices() : m_Choices;
        public float Timer => m_Timer;
        public int TotalAnswer => GetTotalAnswer();

        [NonSerialized] private int m_TotalAnswer = 0;
        [NonSerialized] private QuestUtilLabelChoice[] m_RandomizedChoices;

        public AudioClip SoundQuest => m_SoundQuest;
        public AnimatorClipConfig HowManySoundConfig => m_HowManySoundConfig;
        public int RepeatSoundQuest => m_RepeatSoundQuest;

        public override VoiceRequest[] GetVoiceRequests()
        {
            List<VoiceRequest> requests = new List<VoiceRequest>(base.GetVoiceRequests());

            if (m_Choices == null)
                return requests.ToArray();

            for (int i = 0; i < m_Choices.Length; i++)
            {
                if (m_Choices[i] != null && !string.IsNullOrEmpty(m_Choices[i].Text))
                {
                    requests.Add(new VoiceRequest
                    {
                        id = name + c_ChoiceSegment + i,
                        text = m_Choices[i].Text,
                        voice_id = GetVoiceId()
                    });
                }
            }

            return requests.ToArray();
        }

        public override bool HasVoiceClip(string requestId)
        {
            if (TryGetChoiceIndex(requestId, out int index))
                return m_Choices[index] != null && m_Choices[index].Audio != null;

            return base.HasVoiceClip(requestId);
        }

        public override void AssignVoiceClip(string requestId, AudioClip clip)
        {
            if (TryGetChoiceIndex(requestId, out int index))
            {
                if (m_Choices[index] != null)
                    m_Choices[index].SetAudio(clip);

                return;
            }

            base.AssignVoiceClip(requestId, clip);
        }

        private bool TryGetChoiceIndex(string requestId, out int index)
        {
            index = -1;

            if (string.IsNullOrEmpty(requestId) || !requestId.Contains(c_ChoiceSegment))
                return false;

            index = StringHelper.ExtractId(requestId);
            return m_Choices != null && index >= 0 && index < m_Choices.Length;
        }

        QuestUtilLabelChoice[] GetRandomizedChoices()
        {
            if(m_RandomizedChoices == null)
            {
                m_RandomizedChoices = new QuestUtilLabelChoice[m_Choices.Length]; 

                System.Random random = new System.Random();
                m_RandomizedChoices = m_Choices.OrderBy(x => random.Next()).ToArray();
            }

            return m_RandomizedChoices;
        }

        int GetTotalAnswer()
        {
            if(m_TotalAnswer == 0)
            {
                foreach (var choice in m_Choices)
                {
                    if(choice.IsAnswer)
                        m_TotalAnswer++;
                }
            }

            return m_TotalAnswer;
        }
    }
}
