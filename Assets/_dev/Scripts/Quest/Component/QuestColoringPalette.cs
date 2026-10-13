using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestColoringPalette : QuestComponent
    {
        [SerializeField] [FormerlySerializedAs("image")] private Image m_Image;
        [SerializeField] [FormerlySerializedAs("button")] private Button m_Button;
        
        private Color m_DefaultColor;
        private QuestColoring m_QuestColoring;
    
        protected override void Start()
        {
            m_QuestColoring = GetComponentInParent<QuestColoring>();

            if (!m_QuestColoring)
                Debug.LogError("QuestColoring Component not found");

            if (!m_Image)
                Debug.LogError("Image Component not found");

            if (!m_Button)
                Debug.LogError("Button Component not found");
        }

        public void SetPaletteColor(Color color)
        {
            m_Image.color = new Color(color.r, color.g, color.b, 1);

            m_DefaultColor = m_Image.color;
        }

        public void BindColor()
        {
            m_QuestColoring.BindColor(m_DefaultColor);
        }
    }
}