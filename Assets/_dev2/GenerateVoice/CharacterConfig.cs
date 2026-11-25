using UnityEngine;

[CreateAssetMenu(fileName = "Character_", menuName = "Kimee/Character")]
public class CharacterConfig : ScriptableObject
{
    [SerializeField]
    private string m_VoiceId;
    public string VoiceId => m_VoiceId;
}
