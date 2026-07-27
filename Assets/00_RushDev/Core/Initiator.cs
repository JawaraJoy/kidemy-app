using UnityEngine;
using UnityEngine.Events;

namespace Rush
{
    public class Initiator : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent m_OnGameStart;

        private void Start()
        {
            m_OnGameStart.Invoke();
        }
    }
}
