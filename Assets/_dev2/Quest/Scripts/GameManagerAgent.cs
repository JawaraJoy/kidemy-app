using UnityEngine;

namespace EduGame
{
    public class GameManagerAgent : MonoBehaviour
    {
        public void PlayQuestionVoice()
        {
            GameManager.Instance.PlayQuestionVoice();
        }
        public void PlaySoundQuest()
        {
            GameManager.Instance.PlaySoundQuest();
        }
    }
}
