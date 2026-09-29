using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "Clip_", menuName = "OtherDev/Animation/Clip")]
    public class AnimatorClipConfig : Config
    {
        [SerializeField]
        private Sprite m_StartingSprite;
        [SerializeField, Min(0)]
        private int m_LoopTime;
        [SerializeField]
        private RuntimeAnimatorController m_Controller;
        [SerializeField]
        private AudioClip m_SoundSFX;
        public RuntimeAnimatorController Controller => m_Controller;
        public AudioClip SoundSFX => m_SoundSFX;
        public int LoopTime => m_LoopTime;
        public Sprite StartingSprite => m_StartingSprite;
    }
}
