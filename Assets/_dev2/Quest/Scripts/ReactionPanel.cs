using EasyTextEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class ReactionPanel : MonoBehaviour
    {
        [SerializeField]
        private PanelView m_Panel;
        [SerializeField]
        private Image m_NPCPotrait;
        [SerializeField]
        [FormerlySerializedAs("ConfirmationImage")] private Image m_ConfirmationImage;
        [SerializeField]
        private Image m_FeelImage;
        [SerializeField]
        private TextMeshProUGUI m_NPCText;
        [SerializeField]
        private TextEffect m_TextEffect;

        [SerializeField]
        private Button m_NextButton;
        [SerializeField]
        private Button m_ResetButton;
        private GameManager m_GameManager;
        public void Init(GameManager gameManager)
        {
            m_GameManager = gameManager;
            m_NextButton.onClick.AddListener(Next);
            m_ResetButton.onClick.AddListener(Home);

            
        }
        public void ShowReaction(ReactionConfig reactionConfig)
        {
            m_NPCPotrait.sprite = reactionConfig.NPCPotrait;
            m_ConfirmationImage.sprite = reactionConfig.ConfirmationImage;
            m_FeelImage.color = reactionConfig.FeelColor;
            m_TextEffect.Refresh();
            m_TextEffect.preset = reactionConfig.DialogueConfig.EffectPreset;
            m_NPCText.text = reactionConfig.DialogueConfig.GetFormattedText();
            m_Panel.Show();
        }

        private void Next()
        {
            if (m_GameManager)
            {
                if (m_GameManager.IsLastQuest())
                {
                    // call the result panel
                    int wrongAnswer = m_GameManager.QuestsCount - m_GameManager.RightAnswerCount;
                    ResultContext result = new ResultContext(wrongAnswer, m_GameManager.RightAnswerCount);
                    m_GameManager.ResultPanel.ShowResult(result);
                }
                else
                {
                    m_GameManager.Next();
                }
                m_Panel.Hide();
            }
            
        }
        private void Replay()
        {
            if (m_GameManager)
            {
                m_GameManager.Reset();
                m_Panel.Hide();
            }
        }
        private void Home()
        {
            if (m_GameManager)
            {
                m_GameManager.GoHome();
                m_Panel.Hide();
            }
        }
    }
}
