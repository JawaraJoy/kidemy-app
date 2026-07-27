using UnityEngine;

namespace Rush
{
    public abstract class PoolableConfig : Config
    {
        [SerializeField]
        private GameObject m_Prefab;

        [SerializeField]
        private int m_PrewarmCount = 10;

        public GameObject Prefab => m_Prefab;
        public int PrewarmCount => m_PrewarmCount;
    }
}