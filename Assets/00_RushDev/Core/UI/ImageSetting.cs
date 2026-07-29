using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rush
{
    [System.Serializable]
    public class ImageSetting
    {
        [SerializeField]
        private Sprite m_MainSprite;
        
        public void Config(Extenable extenable)
        {
            if (extenable.HasExtention(out Image image))
            {
                image.sprite = m_MainSprite;
            }
        }
    }
}
