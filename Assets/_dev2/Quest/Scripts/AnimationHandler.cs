using Rush;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class AnimationHandler : MonoBehaviour
    {
        private AnimatorClipConfig m_ClipConfig;

        [SerializeField]
        private Animator m_Animator;

        private AnimationStateBehaviour[] m_StateMachines;

        [SerializeField]
        private AnimationEventCapture m_EventCapture;
        [SerializeField]
        private Button m_Play;
        private void Start()
        {
            //m_Play.onClick.AddListener(PlayAnimationInternal);
        }

        public void SetClip(AnimatorClipConfig clipConfig)
        {
            if (clipConfig == null)
            {
                Debug.LogWarning($"{nameof(AnimationHandler)}: ClipConfig is null.", this);

                return;
            }

            if (m_Animator == null)
            {
                Debug.LogError($"{nameof(AnimationHandler)}: Animator is not assigned.", this);

                return;
            }

            if (m_ClipConfig == clipConfig) return;
            m_ClipConfig = clipConfig;

            // Pasang Animator Controller
            m_Animator.runtimeAnimatorController = m_ClipConfig.Controller;
            // Ambil semua AnimationStateBehaviour
            m_StateMachines = m_Animator.GetBehaviours<AnimationStateBehaviour>();
            PlayAnimationInternal();
        }

        private AnimationStateBehaviour GetAnimationState(AnimatorClipConfig clipConfig)
        {
            if (clipConfig == null)
            {
                return null;
            }

            foreach (AnimationStateBehaviour stateBehaviour in m_StateMachines)
            {
                if (stateBehaviour == null)
                {
                    continue;
                }

                if (stateBehaviour.ClipConfig == null)
                {
                    continue;
                }

                if (stateBehaviour.ClipConfig.Identic.Id == clipConfig.Identic.Id)
                {
                    return stateBehaviour;
                }
            }

            return null;
        }

        private bool HasAnimationState(AnimatorClipConfig clipConfig, out AnimationStateBehaviour animationState)
        {
            animationState = GetAnimationState(clipConfig);

            return animationState != null;
        }
        public void PlayAnimation()
        {
            PlayAnimationInternal();
        }
        private void PlayAnimationInternal()
        {
            if (m_Animator == null) return;
            if (m_ClipConfig == null) return;
            m_EventCapture.SetPlayedConfig(m_ClipConfig);
            if (!HasAnimationState(m_ClipConfig, out AnimationStateBehaviour animationState))
            {
                Debug.LogWarning($"AnimationStateBehaviour for " + $"'{m_ClipConfig.Identic.Id}' was not found.", this);

                return;
            }

            // apakah animationState.name = nama dari state tersebut?
            StartCoroutine(Playing(m_ClipConfig, animationState));
        }

        private IEnumerator Playing(AnimatorClipConfig clipConfig, AnimationStateBehaviour animationState)
        {
            yield return new WaitForSeconds(1.5f);
            int loopCount = clipConfig.LoopTime;
            AudioSource music = GameManager.Instance.Music;
            music.volume = 0.4f;
            for (int i = 0; i < loopCount; i++)
            {
                m_Animator.Play(animationState.StateHash);
                //m_Animator.SetTrigger("Bunyi");
                Debug.Log($"Play clip animation {animationState.StateHash}");
                yield return new WaitForSeconds(animationState.ClipLength);
            }
            music.volume = 1f;
        }
    }
}