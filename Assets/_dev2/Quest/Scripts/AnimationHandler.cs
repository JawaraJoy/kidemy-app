using Rush;
using System.Collections;
using UnityEngine;

namespace EduGame
{
    public class AnimationHandler : MonoBehaviour
    {
        private const float c_ResultDelay = 1.5f;

        private AnimatorClipConfig m_ClipConfig;

        [SerializeField]
        private Animator m_Animator;

        private AnimationStateBehaviour[] m_StateMachines;

        [SerializeField]
        private AnimationEventCapture m_EventCapture;
        private IQuestHost m_Host;
        private Coroutine m_Playback;

        public void SetHost(IQuestHost host) => m_Host = host;

        private void OnDisable()
        {
            if (m_Playback != null) StopCoroutine(m_Playback);
            m_Playback = null;
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

        public void PlayAnimation()
        {
            PlayAnimationInternal();
        }
        private void PlayAnimationInternal()
        {
            if (m_Animator == null) return;
            if (m_ClipConfig == null) return;
            if (m_EventCapture) m_EventCapture.SetPlayedConfig(m_ClipConfig);
            var animationState = GetAnimationState(m_ClipConfig);
            if (!animationState)
            {
                Debug.LogWarning($"AnimationStateBehaviour for " + $"'{m_ClipConfig.Identic.Id}' was not found.", this);

                return;
            }

            if (m_Playback != null) StopCoroutine(m_Playback);
            m_Playback = StartCoroutine(Playing(m_ClipConfig, animationState));
        }

        private IEnumerator Playing(AnimatorClipConfig clipConfig, AnimationStateBehaviour animationState)
        {
            yield return new WaitForSeconds(c_ResultDelay);
            int loopCount = clipConfig.LoopTime;
            m_Host?.DuckMusicFor(animationState.ClipLength * loopCount);
            for (int i = 0; i < loopCount; i++)
            {
                m_Animator.Play(animationState.StateHash);
                yield return new WaitForSeconds(animationState.ClipLength);
            }
            m_Playback = null;
        }
    }
}