using UnityEngine;
using UnityEngine.Serialization;

namespace EduGame
{
    public class FeedbackManager : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("buttonClick")] private Feedback m_ButtonClick;
        [SerializeField] [FormerlySerializedAs("buttonOver")] private Feedback m_ButtonOver;
        [SerializeField] [FormerlySerializedAs("buttonExit")] private Feedback m_ButtonExit;

        public static FeedbackManager Instance { get; private set; }
        public Feedback ButtonClick => m_ButtonClick;
        public Feedback ButtonOver => m_ButtonOver;
        public Feedback ButtonExit => m_ButtonExit;

        void Awake()
        {
            if(!Instance)
                Instance = this;
        }
    }
}