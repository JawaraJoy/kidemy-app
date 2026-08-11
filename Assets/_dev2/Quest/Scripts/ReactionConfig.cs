using AddOn.TextAnimation;
using UnityEngine;

namespace EduGame
{
    [CreateAssetMenu(fileName = "ReactionConfig", menuName = "OtherDev/ReactionConfig")]
    public class ReactionConfig : Config
    {
        [SerializeField]
        private Sprite m_NPCPotrait;
        [SerializeField]
        private Sprite m_ConfirmationImage;
        [SerializeField]
        private Color m_FeelColor;
        [SerializeField]
        private DialogueConfig m_DialogueConfig;

        public Sprite NPCPotrait => m_NPCPotrait;
        public Sprite ConfirmationImage => m_ConfirmationImage;
        public Color FeelColor => m_FeelColor;
        public DialogueConfig DialogueConfig => m_DialogueConfig;
    }
}
