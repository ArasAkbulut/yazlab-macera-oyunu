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

        private void HandleThinkingStarted()
        {
            if (thinkingIndicator) thinkingIndicator.SetActive(true);
            if (errorText) errorText.text = string.Empty;
        }

        private void HandleReplyReceived(NpcAction action)
        {
            if (thinkingIndicator) thinkingIndicator.SetActive(false);
            if (replyText) replyText.text = action.reply;
        }

        private void HandleError(string message)
        {
            if (thinkingIndicator) thinkingIndicator.SetActive(false);
            if (errorText) errorText.text = message;
        }
    }
}
