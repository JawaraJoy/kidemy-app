using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_Character", menuName = "EduGame/Character")]
    public class SO_Character : ScriptableObject
    {
        [SerializeField] private string characterId;
        [SerializeField] private string characterName;
        [SerializeField] private Sprite characterImage;
        [SerializeField]
        private ReactionConfig m_RightReaction;
        [SerializeField]
        private ReactionConfig m_WrongReaction;
        [SerializeField] private Sprite m_ResultCharacterImage;

        [SerializeField] private RuntimeAnimatorController characterController;

        public string CharacterId => characterId;
        public string CharacterName => characterName;
        public Sprite CharacterImage => characterImage;
        public Sprite ResultCharacterImage => m_ResultCharacterImage;
        public ReactionConfig RightReaction => m_RightReaction;
        public ReactionConfig WrongReaction => m_WrongReaction;
        public RuntimeAnimatorController CharacterController => characterController;
    }


}