using UnityEngine;

namespace EduGame
{
    public class GameManagerAgent : MonoBehaviour
    {
        [SerializeField] private Quest m_Quest;

        public void PlayQuestionVoice() => m_Quest?.Host?.PlayQuestionVoice();
        public void PlaySoundQuest() => m_Quest?.Host?.PlaySoundQuest();
    }
}
