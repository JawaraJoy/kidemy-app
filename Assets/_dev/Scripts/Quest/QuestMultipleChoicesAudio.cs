using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestMultipleChoicesAudio : QuestMultipleChoices
    {
        [SerializeField, FormerlySerializedAs("audioPlayer")] private AudioPlayer m_AudioPlayer;

        public override void ShowTutorial()
        {
            if (m_AudioPlayer && CorrectChoiceRect && m_AudioPlayer.TryGetComponent(out RectTransform play))
                Host.ShowTutorialCustom(new[] { play, CorrectChoiceRect });
            else
                base.ShowTutorial();
        }
    }
}
