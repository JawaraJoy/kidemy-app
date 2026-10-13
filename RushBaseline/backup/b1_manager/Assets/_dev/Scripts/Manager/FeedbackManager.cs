using UnityEngine;

namespace EduGame
{
    public class FeedbackManager : MonoBehaviour
    {
        [SerializeField] private Feedback buttonClick;
        [SerializeField] private Feedback buttonOver;
        [SerializeField] private Feedback buttonExit;

        public static FeedbackManager Instance { get; private set; }
        public Feedback ButtonClick => buttonClick;
        public Feedback ButtonOver => buttonOver;
        public Feedback ButtonExit => buttonExit;

        void Awake()
        {
            if(!Instance)
                Instance = this;
        }
    }
}