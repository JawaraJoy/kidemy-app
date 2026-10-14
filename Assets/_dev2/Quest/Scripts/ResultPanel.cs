using AddOn.TextAnimation;
using EasyTextEffects;
using EasyTextEffects.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    public class ResultPanel : PanelView
    {
        private const float c_Percent = 100f;

        [SerializeField]
        private Image m_NPCIcon;
        [SerializeField]
        private TextMeshProUGUI m_LabelText;
        [SerializeField]
        private TextMeshProUGUI m_ScoreText;
        [SerializeField]
        private TextEffectInstance m_WrongTextEffect;
        [SerializeField]
        private TextEffectInstance m_RightTextEffect;
        [SerializeField]
        private StarPresenter m_StarPresenter;
        [SerializeField]
        private Button m_ResetButton;
        [SerializeField]
        private Button m_HomeButton;

        private TextEffect m_ScoreTextEff;
        private TextEffect m_LabelTextEff;

        private IResultHost m_Host;

        private void Start()
        {
            // m_Host disuntik GameManager lewat SetHost sebelum Start

            m_NPCIcon.sprite = m_Host.ChallengeConfig.Character.ResultCharacterImage;
            m_ResetButton.onClick.AddListener(ResetQuest);
            m_HomeButton.onClick.AddListener(Home);
            if (m_LabelText.TryGetComponent(out TextEffect textEffect))
            {
                m_LabelTextEff = textEffect;
            }
            if (m_ScoreText.TryGetComponent(out TextEffect scoreTextEff))
            {
                m_ScoreTextEff = scoreTextEff;
            }
            if (m_StarPresenter)
            {
                m_StarPresenter.SetResultPanel(this);
            }
        }
        public void SetLabelText(string text)
        {
            m_LabelText.text = text;
        }
        public void ShowResult(ResultContext context)
        {
            int wrongCount = context.WrongCount;
            int rightCount = context.RightCount;
            float rate = context.GetRate() * c_Percent;
            string wrongCountText = WordSetting.GetFormatedText(wrongCount.ToString(), new TextEffectInstance[1] { m_WrongTextEffect });
            string rightCountText = WordSetting.GetFormatedText(rightCount.ToString(), new TextEffectInstance[1] {m_RightTextEffect});

            string scoreText = $"{rightCountText} right(s) of {context.GetTotalQuest()}";
            m_ScoreText.text = scoreText;

            if (m_ScoreTextEff != null)
            {
                m_ScoreTextEff.Refresh();
            }
            if (m_LabelTextEff != null)
            {
                m_LabelTextEff.Refresh();
            }

            ShowInternal();
            m_StarPresenter.DisableAllStars();
            m_StarPresenter.StarsResult(context);
        }
        private void ResetQuest()
        {
            // Alternates between fullscreen and windowed mode
            Screen.fullScreenMode = FullScreenMode.Windowed;
            m_Host.ReloadScene();
        }
        private void Home()
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            m_Host.GoHome();
        }

        public void SetHost(IResultHost host)
        {
            m_Host = host;
        }

        public void SetRightAnswer(int set)
        {
            m_Host.SetRightAsnwer(set);
        }
    }
}
