using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class QuestContainer : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Transform shadow = transform.Find("Shadow");

            if(shadow)
            {
                Image shadowImage = shadow.GetComponent<Image>();

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