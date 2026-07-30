using EasyTextEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AddOn.TextAnimation
{
    public class ConversationPresenter : MonoBehaviour
    {
        [SerializeField]
        private Image m_Portrait;
        [SerializeField]
        private TextMeshProUGUI m_NameText;
        [SerializeField]
        private TextMeshProUGUI m_ConversationText;
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
            ApplyConversation(m_Dialogues[m_CurrentDialogueIndex]);

        }
        [ContextMenu("NextConversation")]
        public void NextConversation()
        {
            m_CurrentDialogueIndex++;
            if (m_CurrentDialogueIndex >= m_Dialogues.Length)
            {
                EndConversation();
                return;
            }
            ApplyConversation(m_Dialogues[m_CurrentDialogueIndex]);
        }
        // mostly we use this function to play the specific dialogue
        public void PlayConversation(DialogueConfig dialogueConfig)
        {
            if (HasDialogueInternal(dialogueConfig.Identic.Id, out DialogueConfig config)) 
            {
                ApplyConversation(config);
            }
        }

        private void ApplyConversation(DialogueConfig dialogueConfig)
        {
            TagEffectsPreset effectPreset = dialogueConfig.EffectPreset;
            m_Effects.preset = effectPreset;
            m_NameText.text = dialogueConfig.Identic.Name;
            m_Portrait.sprite = dialogueConfig.Potrait;
            m_ConversationText.text = dialogueConfig.GetWords();
            m_Effects.Refresh();
        }
        private void EndConversation()
        {
            m_CurrentDialogueIndex = 0;
            // can add event here
        }
    }
}
