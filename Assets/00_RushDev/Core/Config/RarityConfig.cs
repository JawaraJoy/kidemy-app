using UnityEngine;

namespace Rush
{
    [CreateAssetMenu(fileName = "Rarity_", menuName = "Rush/Rarity")]
    public class RarityConfig : Config
    {
        [SerializeField]
        private Color m_Color = Color.white;
        public Color Color => m_Color;
    }
}
