using UnityEngine;

namespace EduGame
{
    public class GameManagerAgent : MonoBehaviour
    {
        public void PlayQuestionVoice()
        {
            GetComponentInParent<Quest>(true).Host.PlayQuestionVoice();
        }
        public void PlaySoundQuest()
        {
            GetComponentInParent<Quest>(true).Host.PlaySoundQuest();
        }
    }
}
