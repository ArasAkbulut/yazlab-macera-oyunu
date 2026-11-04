using UnityEngine;
using UnityEngine.InputSystem;
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
            // Legacy InputField.onEndEdit hem Enter'da hem de odak kaybında tetiklenir. Odak
            // kaybında da gönderirsek "Gönder"e tıklamak mesajı iki kez yolluyor (ilki iptal
            // ediliyordu) ve kutudan tıklayıp çıkmak bile yarım metni gönderiyordu — sadece Enter.
            playerInput.onEndEdit.AddListener(_ =>
            {
                if (IsEnterPressed()) HandleSendClicked();
            });
        }

        private static bool IsEnterPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed);
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
