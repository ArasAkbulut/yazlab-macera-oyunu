using UnityEngine;
using UnityEngine.UI;
using WesternAdventure.NPC;

namespace WesternAdventure.UI
{
    /// <summary>
    /// NpcDialogueController olaylarını dinleyip ekrana yansıtan ince bir katman.
    /// Görsel tasarım/UI prefab'ı tasarım ekibi tarafından değiştirilebilir;
    /// burada korunması beklenen tek şey "Düşünüyor..." geri bildirimidir
    /// (rapor gereksinimi: latency sırasında görsel geri bildirim zorunlu).
    /// </summary>
    [RequireComponent(typeof(NpcDialogueController))]
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private NpcDialogueController dialogueController;
        [SerializeField] private Text replyText;
        [SerializeField] private GameObject thinkingIndicator;
        [SerializeField] private Text errorText;

        private void OnEnable()
        {
            dialogueController.OnThinkingStarted += HandleThinkingStarted;
            dialogueController.OnReplyReceived += HandleReplyReceived;
            dialogueController.OnErrorOccurred += HandleError;
        }

        private void OnDisable()
        {
            dialogueController.OnThinkingStarted -= HandleThinkingStarted;
            dialogueController.OnReplyReceived -= HandleReplyReceived;
            dialogueController.OnErrorOccurred -= HandleError;
        }

        /// <summary>Muhafızın açılış repliği gibi LLM'den gelmeyen bir satırı gösterir.</summary>
        public void ShowLine(string line)
        {
            if (thinkingIndicator) thinkingIndicator.SetActive(false);
            if (errorText) errorText.text = string.Empty;
            if (replyText) replyText.text = line;
        }

        private void HandleThinkingStarted()
        {
            if (thinkingIndicator) thinkingIndicator.SetActive(true);
            if (errorText) errorText.text = string.Empty;
        }

        private void HandleReplyReceived(NpcAction action)
        {
            if (thinkingIndicator) thinkingIndicator.SetActive(false);
            if (!replyText) return;

            string line = $"Muhafız: \"{action.reply}\"";
            int remaining = dialogueController.MaxTurns - dialogueController.CurrentTurn;
            if (action.GetActionType() == NpcActionType.None && remaining > 0)
            {
                line += $"\n<size=24>(Kalan cevap hakkın: {remaining})</size>";
            }
            replyText.text = line;
        }

        private void HandleError(string message)
        {
            if (thinkingIndicator) thinkingIndicator.SetActive(false);
            if (errorText) errorText.text = message;
        }
    }
}
