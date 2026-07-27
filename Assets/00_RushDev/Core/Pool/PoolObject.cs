using UnityEngine;

namespace Rush
{
    public class PoolObject : MonoBehaviour
    {
        [SerializeField]
        private PoolableConfig m_SourceConfig;

        public PoolableConfig SourceConfig => m_SourceConfig;

        public void Init(PoolableConfig sourceConfig)
        {
            m_SourceConfig = sourceConfig;
        }
    }
}