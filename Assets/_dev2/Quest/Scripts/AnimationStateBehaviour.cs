using UnityEngine;

namespace EduGame
{
    public class AnimationStateBehaviour : StateMachineBehaviour
    {
        [SerializeField]
        private AnimatorClipConfig m_ClipConfig;

        [SerializeField]
        private int m_StateHash;

        [SerializeField]
        private float m_ClipLength;

        public AnimatorClipConfig ClipConfig => m_ClipConfig;

        public int StateHash => m_StateHash;

        public float ClipLength => m_ClipLength;
    }
}