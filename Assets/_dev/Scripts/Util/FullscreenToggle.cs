using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class FullscreenToggle : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("toggleSprite")] private Sprite m_ToggleSprite;

        [SerializeField] [FormerlySerializedAs("icon")] private Image m_Icon;

        private Sprite m_OriginalSprite;

        void Awake()
        {
            if(m_Icon)
                m_OriginalSprite = m_Icon.sprite;
        }

        public void ToggleFullscreen()
        {
            // Alternates between fullscreen and windowed mode
            Screen.fullScreen = !Screen.fullScreen;

            if(m_ToggleSprite && m_OriginalSprite)
                m_Icon.sprite = Screen.fullScreen? m_ToggleSprite : m_OriginalSprite;
        }
    }
}