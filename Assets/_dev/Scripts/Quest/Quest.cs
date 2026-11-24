using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public abstract class Quest : MonoBehaviour
    {
        protected const int c_MaxStar = 3;
        private const float c_TutorialDelay = 2f;
        private Coroutine m_PromptPlayback;

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
        protected IQuestHost m_Host;
        protected ChallengeConfig m_Challenged;
        public ChallengeConfig Challenged => m_Challenged;
        protected int m_ChallengeIndex;
        public int ChallengeIndex => m_ChallengeIndex;
        public Sprite Background => m_Background;
        public Color Color => m_Color;
        
        public SO_Quest Data => m_Data;
        public SO_Character Character => m_Challenged && m_Challenged.Character ? m_Challenged.Character : m_Character;
        
        protected virtual void Start()
        {
            if(!m_Data)
                Debug.LogError("Quest not set");
            
            if (Character && Character.CharacterController)
                m_Host.SetNPC(Character.CharacterController);
            m_Host.InitQuest(m_Data);

            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            
        }
        public void SetBackground(Sprite sprite)
        {
            m_Background = sprite;
        }
        public void SetChallengeIndex(int index)
        {
            m_ChallengeIndex = index;
        }
        public void SetChallenge(ChallengeConfig challenge)
        {
            m_Challenged = challenge;
        }
        public void SetData(SO_Quest questData)
        {
            m_Data = questData;
        }
        public void Setup()
        {
            SetupInternal();
        }

        protected virtual void SetupInternal()
        {
            if(m_Host != null)
                m_AudioSource = m_Host.AudioSource;
        }

        public void PlayQuestionVoice()
        {
            PlayQuestionVoiceInternal();
        }

        protected virtual void PlayQuestionVoiceInternal()
        {
            if (!m_AudioSource || !isActiveAndEnabled) return;
            StopPromptInternal();
            m_PromptPlayback = StartCoroutine(PlayPromptSequence());
        }

        private IEnumerator PlayPromptSequence()
        {
            AudioClip question = m_Data ? m_Data.Question?.Audio : null;
            if (question)
            {
                PlayClipInternal(question);
                yield return new WaitWhile(() => m_AudioSource.isPlaying);
            }

            yield return PlaySoundClips();
            yield return PlayChoiceVoices();
            m_PromptPlayback = null;
        }

        protected virtual IEnumerator PlayChoiceVoices() { yield break; }

        public virtual void PlaySoundQuest() => PlaySoundQuestInternal();

        protected virtual void PlaySoundQuestInternal()
        {
            if (!m_AudioSource || !isActiveAndEnabled) return;
            StopPromptInternal();
            m_PromptPlayback = StartCoroutine(PlaySoundClips());
        }

        private IEnumerator PlaySoundClips()
        {
            if (!(m_Data is SO_QuestMultipleChoice choice) || !choice.SoundQuest) yield break;
            for (int i = 0; i < Mathf.Max(1, choice.RepeatSoundQuest); i++)
            {
                PlayClipInternal(choice.SoundQuest);
                yield return new WaitWhile(() => m_AudioSource.isPlaying);
            }
        }

        public void PlayQuestionVoice(AudioClip clip) => PlayQuestionVoiceInternal(clip);

        protected virtual void PlayQuestionVoiceInternal(AudioClip clip)
        {
            if (!clip || !m_AudioSource) return;
            StopPromptInternal();
            PlayClipInternal(clip);
        }

        protected void PlayClipInternal(AudioClip clip)
        {
            m_AudioSource.Stop();
            m_AudioSource.PlayOneShot(clip);
            m_Host.DuckMusicFor(clip.length);
        }

        private void StopPromptInternal()
        {
            if (m_PromptPlayback == null) return;
            StopCoroutine(m_PromptPlayback);
            m_PromptPlayback = null;
        }

        public virtual void OnAnswered(bool result, bool submit = true)
        {
            m_IsAnswered = true;

            if(submit)
                SubmitInternal(result ? c_MaxStar : 1);
        }

        public void Submit(int star = 1)
        {
            SubmitInternal(star);
        }

        protected virtual void SubmitInternal(int star = 1)
        {
            m_Host.Submit(star);
        }

        public virtual void ShowTutorial() { }

        public void Reset()
        {
            ResetInternal();
        }

        protected virtual void ResetInternal()
        {
            CancelInvoke(nameof(ShowTutorial));
            Invoke(nameof(ShowTutorial), c_TutorialDelay);
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

        public IQuestHost Host => m_Host;

        public void SetHost(IQuestHost host)
        {
            m_Host = host;
        }
    }
}
