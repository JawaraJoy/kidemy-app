using EduGame;

namespace Rush
{
    public sealed class FirstSoundPopCard : QuestMultipleChoicesItem
    {
        private const float c_RetryFlashDuration = 0.45f;

        public override void OnClick()
        {
            if (!Button.IsInteractable() || !m_Quest)
                return;

            if (m_Choice.IsAnswer)
            {
                m_Correct.SetActive(true);
                base.OnClick();
                return;
            }

            m_Wrong.SetActive(true);
            CancelInvoke(nameof(HideRetryHighlight));
            Invoke(nameof(HideRetryHighlight), c_RetryFlashDuration);
            m_Quest.OnAnswered(false);
        }

        private void HideRetryHighlight()
        {
            m_Wrong.SetActive(false);
        }
    }
}
