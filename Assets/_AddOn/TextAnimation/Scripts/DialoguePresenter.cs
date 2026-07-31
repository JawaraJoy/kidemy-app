using EasyTextEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AddOn.TextAnimation
{
    public class DialoguePresenter : MonoBehaviour
    {
        [SerializeField]
        private Image m_Portrait;
        [SerializeField]
        private TextMeshProUGUI m_NameText;
        [SerializeField]
        private TextMeshProUGUI m_DialogueText;
        [SerializeField]
        private TextEffect m_Effects;
        [SerializeField]
        private DialogueConfig[] m_Dialogues;

        private int m_CurrentDialogueIndex;

        public void SetDialogues(DialogueConfig[] dialogues)
        {
            m_Dialogues = dialogues;
        }
        private DialogueConfig GetDialogue(string dialogueId)
        {
            foreach(DialogueConfig config in m_Dialogues)
            {
                if (config.Identic.Id == dialogueId)
                {
                    return config;
                }
            }
            return null;
        }

        private bool HasDialogueInternal(string dialogueId, out DialogueConfig config)
        {
            config = GetDialogue(dialogueId);
            return config != null;
        }
        public bool HasDialogue(string dialogueId, out DialogueConfig config)
        {
            return HasDialogueInternal(dialogueId, out config);
        }

        [ContextMenu("StartConversation")]
        public void StartConversation()
        {
            m_CurrentDialogueIndex = 0;
            ApplyConversationInternal(m_Dialogues[m_CurrentDialogueIndex]);

        }
        [ContextMenu("NextConversation")]
        public void NextConversation()
        {
            m_CurrentDialogueIndex++;
            if (m_CurrentDialogueIndex >= m_Dialogues.Length)
            {
                EndConversation();
            }
            ApplyConversationInternal(m_Dialogues[m_CurrentDialogueIndex]);
        }
        // mostly we use this function to play the specific dialogue from outside
        public void ApplyConversation(DialogueConfig dialogueConfig)
        {
            ApplyConversationInternal(dialogueConfig);
        }

        private void ApplyConversationInternal(DialogueConfig dialogueConfig)
        {
            TagEffectsPreset effectPreset = dialogueConfig.EffectPreset;
            m_Effects.preset = effectPreset;
            m_NameText.text = dialogueConfig.Identic.Name;
            m_Portrait.sprite = dialogueConfig.Potrait;
            m_DialogueText.text = dialogueConfig.GetFormattedText();
            m_Effects.Refresh();
        }
        private void EndConversation()
        {
            m_CurrentDialogueIndex = 0;
            // can add event here
        }
    }
}
