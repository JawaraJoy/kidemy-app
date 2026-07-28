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
    [SerializeField] private RuntimeAnimatorController characterController;
    
    public string CharacterId => characterId;
    public string CharacterName => characterName;
    public Sprite CharacterImage => characterImage;
    public RuntimeAnimatorController CharacterController => characterController;
  }
}