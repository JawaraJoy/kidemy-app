using System;
using System.Globalization;
using EduGame;
using UnityEngine;
using UnityEngine.UI;

namespace Rush
{
    public sealed class OneOrManyCard : QuestMultipleChoicesItem
    {
        [SerializeField] private Image[] m_Pictures;
        [SerializeField] private Sprite[] m_LeftRightPairs;

        public override void SetChoice(QuestUtilLabelChoice choice)
        {
            base.SetChoice(choice);

            // Each bank label starts with its displayed quantity, e.g. "2 feet".
            int count = ReadQuantity(choice.Text);
            if (count < 1 || count > m_Pictures.Length)
            {
                Debug.LogError($"Invalid picture quantity in '{choice.Text}'.", this);
                DisableInternal();
                count = 0;
            }

            bool mirrorSecond = count == 2 && Array.IndexOf(m_LeftRightPairs, choice.Image) >= 0;
            for (int i = 0; i < m_Pictures.Length; i++)
            {
                Image picture = m_Pictures[i];
                picture.gameObject.SetActive(i < count);
                if (i < count)
                {
                    picture.sprite = choice.Image;
                    picture.color = choice.ImageColor;
                    picture.rectTransform.localScale = mirrorSecond && i == 1 ? new Vector3(-1f, 1f, 1f) : Vector3.one;
                }
            }
        }

        private static int ReadQuantity(string label)
        {
            int separator = label?.IndexOf(' ') ?? -1;
            if (separator > 0 && int.TryParse(label.Substring(0, separator), NumberStyles.None,
                CultureInfo.InvariantCulture, out int quantity))
                return quantity;

            return 0;
        }
    }
}
