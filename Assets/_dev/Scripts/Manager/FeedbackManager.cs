using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public class FeedbackManager : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("buttonClick")] private Feedback m_ButtonClick;
        [SerializeField] [FormerlySerializedAs("buttonOver")] private Feedback m_ButtonOver;
        [SerializeField] [FormerlySerializedAs("buttonExit")] private Feedback m_ButtonExit;

        private static FeedbackManager s_Instance;
        public static FeedbackManager Instance => s_Instance;
        public Feedback ButtonClick => m_ButtonClick;
        public Feedback ButtonOver => m_ButtonOver;
        public Feedback ButtonExit => m_ButtonExit;

        void Awake()
        {
            if(!Instance)
                s_Instance = this;
        }
    }
}