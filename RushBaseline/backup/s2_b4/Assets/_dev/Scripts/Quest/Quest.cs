using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public abstract class Quest : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] [FormerlySerializedAs("data")] protected SO_Quest m_Data;

        [Header("Theme")]
        [SerializeField] [FormerlySerializedAs("background")] protected Sprite m_Background;
        [SerializeField] [FormerlySerializedAs("color")] protected Color m_Color;
        [SerializeField] [FormerlySerializedAs("npcController")] protected RuntimeAnimatorController m_NpcController;
        [SerializeField] [FormerlySerializedAs("character")] protected SO_Character m_Character;
        
        [Header("Rules")]
        [SerializeField] [FormerlySerializedAs("score")] protected int m_Score = 10;

        protected bool m_IsAnswered = false;
        protected bool m_IsCorrect = false;
        protected AudioSource m_AudioSource;
        protected ChallengeConfig m_Challenged;
        public ChallengeConfig Challenged => m_Challenged;
        public Sprite Background => m_Background;
        public Color Color => m_Color;
        
        public SO_Quest Data => m_Data;
        
        protected virtual void Start()
        {
            if(!m_Data)
                Debug.LogError("Quest not set");
            
            //if(npcController)
            //    GameManager.Instance.SetNPC(npcController);
            if (m_Challenged != null)
            {
                if (m_Challenged.Character != null)
                {
                    GameManager.Instance.SetNPC(m_Challenged.Character.CharacterController);
                }
            }
            else
            {
                if (m_Character && m_Character.CharacterController)
                    GameManager.Instance.SetNPC(m_Character.CharacterController);
            }
            GameManager.Instance.InitQuest(m_Data);

            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            
        }
        public void SetBackground(Sprite sprite)
        {
            m_Background = sprite;
        }
        public void SetChallenge(ChallengeConfig challenge)
        {
            m_Challenged = challenge;
        }
        public void SetData(SO_Quest questData)
        {
            m_Data = questData;
        }
        public virtual void Setup()
        {
            if(GameManager.Instance)
                m_AudioSource = GameManager.Instance.AudioSource;
        }

        public virtual void PlayQuestionVoice()
        {
            Debug.Log(m_Data.Question.Audio);

            if(m_Data.Question.Audio)
                PlayQuestionVoice(m_Data.Question.Audio);
        }
        public virtual void PlaySoundQuest()
        {
            PlaySoundQuestInternal();
        }

        protected virtual void PlaySoundQuestInternal()
        {
            if (m_Data is SO_QuestMultipleChoice quest)
            {
                if (quest.SoundQuest)
                {
                    StartCoroutine(PlayingSoundQuest(quest));
                }
            }
        }

        private IEnumerator PlayingSoundQuest(SO_QuestMultipleChoice quest)
        {
            int repeat = quest.RepeatSoundQuest;
            if (repeat <= 0)
            {
                repeat = 1;
            }
            for (int i = 0; i < repeat; i++)
            {
                m_AudioSource.PlayOneShot(quest.SoundQuest);
                yield return new WaitForSeconds(quest.SoundQuest.length);
            }
        }
        
        public virtual void PlayQuestionVoice(AudioClip audioClip)
        {
            //StartCoroutine(PlayingQuestionVoice(audioClip));
            if (m_AudioSource)
            {
                m_AudioSource.PlayOneShot(audioClip);
                GameManager.Instance.StartCoroutine(ReduceMusicAWhile(audioClip));
                if (m_Data is SO_QuestMultipleChoice quest)
                {
                    if (quest.SoundQuest != null)
                    {
                        float clipduration = quest.SoundQuest.length;
                        StartCoroutine(PlaySoundQuestAfterQuestionVoiceDone(clipduration));
                    }
                }
            }
        }
        private IEnumerator ReduceMusicAWhile(AudioClip audioClip)
        {
            AudioSource music = GameManager.Instance.Music;
            float durationvoice = audioClip.length;
            music.volume = 0.4f;
            yield return new WaitForSeconds(durationvoice);
            music.volume = 1f;
        }
        private IEnumerator PlaySoundQuestAfterQuestionVoiceDone(float delay)
        {
            yield return new WaitForSeconds(delay);
            PlaySoundQuestInternal();
        }

        public virtual void OnAnswered(bool result, bool submit = true)
        {
            m_IsAnswered = true;

            if(submit)
                Submit(result ? 3 : 1);
        }

        public virtual void Submit(int star = 1)
        {
            GameManager.Instance.Submit(star);
        }

        public virtual void ShowTutorial()
        {
            GameManager.Instance.ShowTutorial(null);
        }

        public virtual void Reset()
        {
            //PlayQuestionVoice();

            Invoke(nameof(ShowTutorial), 2f);
        }

        public virtual void Disabled()
        {
            gameObject.SetActive(false);
        }

        public virtual void Enabled()
        {
            gameObject.SetActive(true);
        }

        // --- Voice Request & Assignment Interface ---

        /// <summary>
        /// Retrieves the voice_id from the SO_Character assigned to this Quest prefab.
        /// </summary>
        public virtual string GetVoiceId()
        {
            return m_Data != null ? m_Data.GetVoiceId() : "";
        }

        /// <summary>
        /// Returns all VoiceRequests needed by this quest.
        /// Reads question text from 'data' and voice_id from 'character'.
        /// </summary>
        public virtual VoiceRequest[] GetVoiceRequests()
        {
            return m_Data != null ? m_Data.GetVoiceRequests() : Array.Empty<VoiceRequest>();
        }

        /// <summary>
        /// Checks if a specific voice request already has an AudioClip assigned in 'data'.
        /// </summary>
        public virtual bool HasVoiceClip(string requestId)
        {
            return m_Data != null && m_Data.HasVoiceClip(requestId);
        }

        /// <summary>
        /// Assigns the downloaded & imported AudioClip directly to the SO_Quest referenced in 'data'.
        /// </summary>
        public virtual void AssignVoiceClip(string requestId, AudioClip clip)
        {
            if (m_Data != null)
                m_Data.AssignVoiceClip(requestId, clip);
        }
    }
}
