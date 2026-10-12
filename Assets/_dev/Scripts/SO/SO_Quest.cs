using System;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public abstract class SO_Quest : ScriptableObject
    {
        public enum QuestCategory { MATH_AND_LOGIC, LITERACY_AND_LANGUAGE, CREATIVITY_AND_EXPRESSION, SOCIAL_EMOTIONAL_LEARNING, LIFE_SKILLS_AND_DISCOVERY }

        [Header("Identifier")]
        [SerializeField] protected string id;
        [SerializeField] protected string title;
        [SerializeField] protected QuestCategory category;

        [Header("Question")]
        [SerializeField] protected SO_Character character;
        [SerializeField]
        private Image m_ImagePrefab;
        [SerializeField]
        private Sprite[] m_MultipleQuestImages;
        [SerializeField] protected QuestUtilLabel question;

        [Header("Rule")]
        [SerializeField] protected bool autoSubmit;
        [SerializeField] private int score = 10;
        [SerializeField] private float tresholdTime = 20;

        /*[SerializeField]
        private LayoutSettingField m_LayoutSetting;
        public LayoutSettingField LayoutSetting => m_LayoutSetting;*/
        public string Id => id;
        public string Title => title;
        public QuestCategory Category => category;
        public SO_Character Character => character;
        public QuestUtilLabel Question => question;
        public bool AutoSubmit => autoSubmit;
        public int Score => score;
        public float TresholdTime => tresholdTime;

        public virtual string GetVoiceId()
        {
            return character != null ? character.CharacterId : string.Empty;
        }

        public virtual VoiceRequest[] GetVoiceRequests()
        {
            if (question != null && !string.IsNullOrEmpty(question.Text))
            {
                return new[]
                {
                    new VoiceRequest
                    {
                        id = name + "_question",
                        text = question.Text,
                        voice_id = GetVoiceId()
                    }
                };
            }

            return Array.Empty<VoiceRequest>();
        }

        public virtual bool HasVoiceClip(string requestId)
        {
            return question != null && question.Audio != null;
        }

        public virtual void AssignVoiceClip(string requestId, AudioClip clip)
        {
            if (!string.IsNullOrEmpty(requestId) && requestId.Contains("_question"))
                question?.SetAudio(clip);
        }
        public bool HasMultipleImages(out Sprite[] multipleImages, out Image prefab)
        {
            multipleImages = m_MultipleQuestImages;
            prefab = m_ImagePrefab;
            return multipleImages.Length > 0 && prefab != null;
        }
    }
}
