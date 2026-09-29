using System.Collections;
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
        [SerializeField]
        private Image m_LightImage;

        private TextEffect m_ScoreTextEff;
        private TextEffect m_LabelTextEff;

        private GameManager m_GameManager;

        private void Start()
        {
            m_GameManager = GameManager.Instance;

            if(m_GameManager.ChallengeConfig)
                m_NPCIcon.sprite = m_GameManager.ChallengeConfig.Character.ResultCharacterImage; 
            else if(m_GameManager.CurrentQuest && m_GameManager.CurrentQuest.Character)
                m_NPCIcon.sprite = m_GameManager.CurrentQuest.Character.ResultCharacterImage;   
            
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
            float rate = context.GetRate() * 100f;
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
            if(m_LightImage != null)
            {
                Debug.Log("Set Light Image RectTransform");

                StartCoroutine(ShowLightAnimation());
            }

            ShowInternal();
            m_StarPresenter.DisableAllStars();
            m_StarPresenter.StarsResult(context);
        }
        private void ResetQuest()
        {
            // Alternates between fullscreen and windowed mode
            Screen.fullScreenMode = FullScreenMode.Windowed;
            m_GameManager.ReloadScene();
        }
        private void Home()
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            m_GameManager.GoHome();
        }

        IEnumerator ShowLightAnimation()
        {
            RectTransform lightRect = m_LightImage.GetComponent<RectTransform>();

            float startTime = Time.time;

            while ((Time.time - startTime) < 1.5f || lightRect.offsetMax != Vector2.zero || lightRect.offsetMin != Vector2.zero)
            {
                lightRect.offsetMin = Vector2.zero;
                lightRect.offsetMax = Vector2.zero;

                yield return new WaitForSeconds(Time.deltaTime);
            }

            yield return null;
        }
    }
}
