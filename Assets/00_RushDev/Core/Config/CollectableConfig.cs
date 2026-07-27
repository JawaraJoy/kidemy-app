using UnityEngine;

namespace Rush
{
    public abstract class CollectableConfig : Config
    {
        [System.Serializable]
        public class CollectableField
        {
            [SerializeField]
            private RarityConfig m_RarityConfig;
            [SerializeField]
            private Sprite m_Icon;
            [SerializeField]
            private Sprite m_SplashImage;
            public RarityConfig RarityConfig => m_RarityConfig;
            public Sprite Icon => m_Icon;
            public Sprite SplashImage => m_SplashImage;
        }
        [SerializeField]
        private CollectableField m_Collectable;
        public CollectableField Collectable => m_Collectable;
    }
}
