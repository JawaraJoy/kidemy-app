using System;
using System.Collections;
using UnityEngine;

namespace EduGame
{
    public abstract class Quest : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] protected SO_Quest data;

        [Header("Theme")]
        [SerializeField] protected Sprite background;
        [SerializeField] protected Color color;
        [SerializeField] protected RuntimeAnimatorController npcController;
        [SerializeField] protected SO_Character character;
        
        [Header("Rules")]
        [SerializeField] protected int score = 10;

        protected bool isAnswered = false;
        protected bool isCorrect = false;
        protected AudioSource audioSource;
        protected ChallengeConfig m_Challenged;
        public ChallengeConfig Challenged => m_Challenged;
        public Sprite Background => background;
        public Color Color => color;
        
        public SO_Quest Data => data;
        
        protected virtual void Start()
        {
            if(!data)
                Debug.LogError("Quest not set");
            
            //if(npcController)
            //    GameManager.Instance.SetNPC(npcController);

            if(character && character.CharacterController)
                GameManager.Instance.SetNPC(character.CharacterController);

            GameManager.Instance.InitQuest(data);

            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            
        }
        public void SetBackground(Sprite sprite)
        {
            background = sprite;
        }
        public void SetChallenge(ChallengeConfig challenge)
        {
            m_Challenged = challenge;
        }
        public void SetData(SO_Quest questData)
        {
            data = questData;
        }
        public virtual void Setup()
        {
            if(GameManager.Instance)
                audioSource = GameManager.Instance.AudioSource;
        }

        public virtual void PlayQuestionVoice()
        {
            Debug.Log(data.Question.Audio);

            if(data.Question.Audio)
            {
                PlayQuestionVoice(data.Question.Audio);
                StartCoroutine(LowerBGMVolume(data.Question.Audio.length));
            }
        }

        public virtual void PlayQuestionVoice(AudioClip audioClip)
        {
            if(audioSource)
            {
                if(audioSource.isPlaying) audioSource.Stop();

                audioSource.PlayOneShot(audioClip);

                StartCoroutine(LowerBGMVolume(audioClip.length));
            }
        }

        private IEnumerator LowerBGMVolume(float duration = 2f)
        {
            GameManager.Instance.BGMSource.volume = 0.2f;

            yield return new WaitForSeconds(duration + 0.2f);
            
            GameManager.Instance.BGMSource.volume = 1f;

            yield return null;
        }

        public virtual void OnAnswered(bool result, bool submit = true)
        {
            isAnswered = true;

            if(submit)
                Submit(result ? 3 : 1);
        }

        public virtual void Submit(int star = 1)
        {
            GameManager.Instance.Submit(star);
        }

        public virtual void Reset()
        {
            Invoke(nameof(PlayQuestionVoice), 1f);
            Invoke(nameof(ShowTutorial), 1f);
        }

        public virtual void ShowTutorial()
        {
            GameManager.Instance.ShowTutorial(null);
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
            return character != null ? character.CharacterId : "";
        }

        /// <summary>
        /// Returns all VoiceRequests needed by this quest.
        /// Reads question text from 'data' and voice_id from 'character'.
        /// </summary>
        public virtual VoiceRequest[] GetVoiceRequests()
        {
            if (data != null && data.Question != null && !string.IsNullOrEmpty(data.Question.Text))
            {
                return new VoiceRequest[]
                {
                    new VoiceRequest
                    {
                        id = name + "_question",
                        text = data.Question.Text,
                        voice_id = GetVoiceId()
                    }
                };
            }

            return Array.Empty<VoiceRequest>();
        }

        /// <summary>
        /// Checks if a specific voice request already has an AudioClip assigned in 'data'.
        /// </summary>
        public virtual bool HasVoiceClip(string requestId)
        {
            return data.Question !=  null && data.Question.Audio != null;
        }

        /// <summary>
        /// Assigns the downloaded & imported AudioClip directly to the SO_Quest referenced in 'data'.
        /// </summary>
        public virtual void AssignVoiceClip(string requestId, AudioClip clip)
        {
            if (requestId.IndexOf("_question") > 0)
            {
                data.Question.SetAudio(clip);
            }
        }
    }
}