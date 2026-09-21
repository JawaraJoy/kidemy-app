using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestMultipleChoicesCountingItem : QuestMultipleChoicesItem    
    {
        public override void SetChoice(QuestUtilLabelChoice choice)
        {
            this.choice = choice;

            if(!string.IsNullOrEmpty(choice.Text) && choice.Image != null)
            {
                int num = StringHelper.ToInt(choice.Text, out bool success);

                Transform container = transform.Find("Container");

                if(container && success)
                {
                    for (int i = container.childCount - 1; i >= 0; i--)
                        Destroy(container.GetChild(i).gameObject);
                    
                    for(int i = 0; i < num; i++)
                    {
                        GameObject go = new GameObject("Image_" + container.name + "_" + i);
                        go.transform.SetParent(container);
                        go.transform.localScale = Vector3.one;
                        go.transform.localPosition = Vector3.zero;
                        go.AddComponent<Image>().sprite = choice.Image;
                    }
                }
            }

            base.SetChoice(choice);
        }
    }
}