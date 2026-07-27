using UnityEngine;

namespace Rush
{
    public abstract class GameConfig : CollectableConfig
    {
        [SerializeField]
        private int m_StarAmount;
        [SerializeField]
        protected GameKeyConfig[] m_GameKeys;
    }

    
}
