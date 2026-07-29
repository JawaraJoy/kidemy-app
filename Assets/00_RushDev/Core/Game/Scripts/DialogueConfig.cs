using UnityEngine;

namespace Rush
{
    [CreateAssetMenu(fileName = "Dialogue_", menuName = "Rush/Game/Quest/Dialogue")]
    public class DialogueConfig : Config
    {
        [SerializeField, ]
        private DialogueSetting[] m_StartingDialogues;
        [SerializeField]
        private DialogueSetting[] m_ResponseDialogues;

        public DialogueSetting GetStartingDialogue()
        {
            int random = Random.Range(0, m_StartingDialogues.Length);
            return m_StartingDialogues[random];
        }
        public DialogueSetting GetRandomResponseDialogue()
        {
            int random = Random.Range(0, m_ResponseDialogues.Length);
            return m_ResponseDialogues[random];
        }
    }

    [System.Serializable]
    public class DialogueSetting
    {
        [SerializeField]
        private Sprite m_Potrait;
        [SerializeField]
        private string m_PotraitName;
        [SerializeField, TextArea(0, 3)]
        private string[] m_Talks;

        public string GetTalk()
        {
            int random = Random.Range(0, m_Talks.Length);
            return m_Talks[random];
        }
    }
}
