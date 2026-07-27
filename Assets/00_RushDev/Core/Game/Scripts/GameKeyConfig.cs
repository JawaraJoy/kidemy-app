using UnityEngine;

namespace Rush
{
    public abstract class GameKeyConfig : CollectableConfig
    {
        [SerializeField]
        protected int m_KeyNumber = 0;
        public int KeyNumber => m_KeyNumber;
    }
}
