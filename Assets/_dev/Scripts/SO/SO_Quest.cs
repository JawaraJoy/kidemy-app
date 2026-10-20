using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public abstract class SO_Quest : ScriptableObject
    {
        private const string c_QuestionSegment = "_question";

        public enum QuestCategory { MATH_AND_LOGIC, LITERACY_AND_LANGUAGE, CREATIVITY_AND_EXPRESSION, SOCIAL_EMOTIONAL_LEARNING, LIFE_SKILLS_AND_DISCOVERY }

        [Header("Identifier")]
        [SerializeField] [FormerlySerializedAs("id")] protected string m_Id;
        [SerializeField] [FormerlySerializedAs("title")] protected string m_Title;
        [SerializeField] [FormerlySerializedAs("category")] protected QuestCategory m_Category;

        [Header("Question")]
        [SerializeField] [FormerlySerializedAs("character")] protected SO_Character m_Character;
        [SerializeField]
        private Image m_ImagePrefab;

        [SerializeField]
        private int m_ConstraintCount;
        [SerializeField]
        private MultipleImageMechanic[] m_MultipleQuestImages;
        
        [SerializeField] [FormerlySerializedAs("question")] protected QuestUtilLabel m_Question;

        [Header("Rule")]
        [SerializeField] [FormerlySerializedAs("autoSubmit")] protected bool m_AutoSubmit;
        [SerializeField] [FormerlySerializedAs("score")] private int m_Score = 10;
        [SerializeField] [FormerlySerializedAs("tresholdTime")] private float m_TresholdTime = 20;
        public int ConstraintCount => m_ConstraintCount;
        /*[SerializeField]
        private LayoutSettingField m_LayoutSetting;
        public LayoutSettingField LayoutSetting => m_LayoutSetting;*/
        public string Id => m_Id;
        public string Title => m_Title;
        public QuestCategory Category => m_Category;
        public SO_Character Character => m_Character;
        public QuestUtilLabel Question => m_Question;
        public bool AutoSubmit => m_AutoSubmit;
        public int Score => m_Score;
        public float TresholdTime => m_TresholdTime;

        public string GetVoiceId()
        {
            return GetVoiceIdInternal();
        }

        protected virtual string GetVoiceIdInternal()
        {
            return m_Character != null ? m_Character.CharacterId : string.Empty;
        }

        public virtual VoiceRequest[] GetVoiceRequests()
        {
            if (m_Question != null && !string.IsNullOrEmpty(m_Question.Text))
            {
                return new[]
                {
                    new VoiceRequest
                    {
                        id = name + c_QuestionSegment,
                        text = m_Question.Text,
                        voice_id = GetVoiceIdInternal()
                    }
                };
            }

            return Array.Empty<VoiceRequest>();
        }

        public virtual bool HasVoiceClip(string requestId)
        {
            return m_Question != null && m_Question.Audio != null;
        }

        public virtual void AssignVoiceClip(string requestId, AudioClip clip)
        {
            if (!string.IsNullOrEmpty(requestId) && requestId.Contains(c_QuestionSegment))
                m_Question?.SetAudio(clip);
        }
        public bool HasMultipleImages(out MultipleImageMechanic[] multipleImages, out Image prefab)
        {
            multipleImages = m_MultipleQuestImages;
            prefab = m_ImagePrefab;
            return multipleImages.Length > 0 && prefab != null;
        }
    }
}
