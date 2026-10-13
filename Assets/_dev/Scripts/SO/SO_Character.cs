using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    [CreateAssetMenu(fileName = "SO_Character", menuName = "EduGame/Character")]
    public class SO_Character : ScriptableObject
    {
        [SerializeField] [FormerlySerializedAs("characterId")] private string m_CharacterId;
        [SerializeField] [FormerlySerializedAs("characterName")] private string m_CharacterName;
        [SerializeField] [FormerlySerializedAs("characterImage")] private Sprite m_CharacterImage;
        [SerializeField]
        private ReactionConfig m_RightReaction;
        [SerializeField]
        private ReactionConfig m_WrongReaction;
        [SerializeField] private Sprite m_ResultCharacterImage;

        [SerializeField] [FormerlySerializedAs("characterController")] private RuntimeAnimatorController m_CharacterController;

        public string CharacterId => m_CharacterId;
        public string CharacterName => m_CharacterName;
        public Sprite CharacterImage => m_CharacterImage;
        public Sprite ResultCharacterImage => m_ResultCharacterImage;
        public ReactionConfig RightReaction => m_RightReaction;
        public ReactionConfig WrongReaction => m_WrongReaction;
        public RuntimeAnimatorController CharacterController => m_CharacterController;
    }


}