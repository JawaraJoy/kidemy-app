using System.Collections;
using EduGame;
using UnityEngine;

namespace Rush
{
    public sealed class FirstSoundPopQuest : QuestMultipleChoices
    {
        private const float c_GlowDuration = 0.5f;

        [SerializeField] private CanvasGroup m_AnswerInput;
        [SerializeField] private AudioClip m_TryAgain;

        public override void OnAnswered(bool result, bool submit = false)
        {
            if (m_IsAnswered)
                return;

            StopAllCoroutines();
            m_AudioSource.Stop();
            if (!result)
            {
                m_AudioSource.PlayOneShot(m_TryAgain);
                return;
            }

            m_IsAnswered = true;
            m_AnswerInput.interactable = false;
            StartCoroutine(ConfirmAfterGlow());
        }

        private IEnumerator ConfirmAfterGlow()
        {
            yield return new WaitForSecondsRealtime(c_GlowDuration);
            base.OnAnswered(true);
        }

        protected override void PlayQuestionVoiceInternal()
        {
            if (m_IsAnswered)
                return;

            m_AudioSource.Stop();
            base.PlayQuestionVoiceInternal();
        }

        protected override void ResetInternal()
        {
            StopAllCoroutines();
            m_IsAnswered = false;
            m_AnswerInput.interactable = true;
            base.ResetInternal();
        }
    }
}
