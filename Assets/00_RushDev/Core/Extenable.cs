using UnityEngine;

namespace Rush
{
    public abstract class Extenable : MonoBehaviour
    {
        [SerializeField]
        protected GameObject[] m_Components;

        private bool HasExtentionInternal<T>(out T exten) where T : MonoBehaviour
        {
            if (gameObject.TryGetComponent(out exten))
            {
                return true;
            }
            return false;
        }
        public bool HasExtention<T>(out T exten) where T : MonoBehaviour
        {
            return HasExtentionInternal(out exten);
        }
    }
}
