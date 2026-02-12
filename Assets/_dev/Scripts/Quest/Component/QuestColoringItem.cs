using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestColoringItem : QuestComponent
    {
        private Image image;
        private Texture2D texture;
        private Color defaultColor;

        private QuestColoring questColoring;
        private QuestUtilRegionalClick regionalClick;

        protected override void Awake()
        {
            image = GetComponent<Image>();
            questColoring = GetComponentInParent<QuestColoring>();
            regionalClick = GetComponent<QuestUtilRegionalClick>();

            if (!image)
                Debug.LogError("Image Component not found");
            
            if(!questColoring)
                Debug.LogError("Quest Component not found");

            if(!regionalClick)
                Debug.LogError("QuestUtilRegionalEvent Component not found");

            defaultColor = image.color;

            questColoring.AddColor(defaultColor);

            regionalClick.AddClickEvent(ChangeColor);

            ChangeColor(Color.white);    
        }

        public void ChangeColor(Color color)
        {
            if(image.color != color)
                image.color = new Color(color.r, color.g, color.b, 1);
        }

        public void ChangeColor()
        {
            if(questColoring.SelectedColor.a > 0.5f)
                ChangeColor(questColoring.SelectedColor);
        }
    }
}