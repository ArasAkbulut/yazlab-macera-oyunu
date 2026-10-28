using UnityEngine;
using UnityEngine.UI;

namespace WesternAdventure.NPC
{
    /// <summary>
    /// Ollama NPC diyaloğunu hızlıca Play Mode'da denemek için basit bir giriş arayüzü köprüsü.
    /// Gerçek oyun UI'ı hazır olana kadar (tasarım ekibi) test amaçlı kullanılır.
    /// </summary>
    public class DialogueTestHarness : MonoBehaviour
    {
        [SerializeField] private NpcDialogueController dialogueController;
        [SerializeField] private InputField playerInput;
        [SerializeField] private Button sendButton;

        private void Awake()
        {
            sendButton.onClick.AddListener(HandleSendClicked);
            // Legacy InputField.onEndEdit fires both on Enter and on losing focus;
            // projenin Input System'i (new) kullanması nedeniyle tuş kontrolü yapmıyoruz,
            // her ikisinde de gönderim kabul edilebilir bir test davranışı.
            playerInput.onEndEdit.AddListener(_ => HandleSendClicked());
        }

        private void HandleSendClicked()
        {
            if (string.IsNullOrWhiteSpace(playerInput.text)) return;
            dialogueController.SendPlayerMessage(playerInput.text);
            playerInput.text = string.Empty;
            playerInput.ActivateInputField();
        }
    }
}
