using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestContainer : MonoBehaviour
    {
        private const string c_ShadowName = "Shadow";

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Transform shadow = transform.Find(c_ShadowName);

            if(shadow)
            {
                shadow.TryGetComponent<Image>(out Image shadowImage);

                if(shadowImage && GameManager.Instance)
                {
                    Color newColor = GameManager.Instance.ColorTheme;
                    newColor.a = 1;

                    shadowImage.color = newColor;
                }
            }   
        }
    }
}