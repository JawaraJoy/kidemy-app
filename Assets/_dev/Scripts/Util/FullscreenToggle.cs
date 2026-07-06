using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class FullscreenToggle : MonoBehaviour
    {
        [SerializeField] private Sprite toggleSprite;

        [SerializeField] private Image icon;

        private Sprite originalSprite;

        void Awake()
        {
            if(icon)
                originalSprite = icon.sprite;
        }

        public void ToggleFullscreen()
        {
            // Alternates between fullscreen and windowed mode
            Screen.fullScreen = !Screen.fullScreen;

            if(toggleSprite && originalSprite)
                icon.sprite = Screen.fullScreen? toggleSprite : originalSprite;
        }
    }
}