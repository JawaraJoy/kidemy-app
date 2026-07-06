using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestColoringPalette : QuestComponent
    {
        [SerializeField] private Image image;
        [SerializeField] private Button button;
        
        private Color defaultColor;
        private QuestColoring questColoring;
    
        protected override void Start()
        {
            questColoring = GetComponentInParent<QuestColoring>();

            if (!questColoring)
                Debug.LogError("QuestColoring Component not found");

            if (!image)
                Debug.LogError("Image Component not found");

            if (!button)
                Debug.LogError("Button Component not found");
        }

        public void SetPaletteColor(Color color)
        {
            image.color = new Color(color.r, color.g, color.b, 1);

            defaultColor = image.color;
        }

        public void BindColor()
        {
            questColoring.BindColor(defaultColor);
        }
    }
}