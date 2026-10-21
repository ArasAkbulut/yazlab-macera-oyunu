using System;
using System.Threading;
using UnityEngine;
using WesternAdventure.Core;

namespace WesternAdventure.NPC
{
    /// <summary>
    /// Oyuncu metnini alıp yerel LLM'e gönderen, yanıtı bekleyen ve sonucu (diyalog metni +
    /// oyun durumu eylemi) UI ve oyun mantığına olaylar üzerinden ileten köprü.
    /// Ollama'ya ulaşılamaması, zaman aşımı ya da bozuk yanıt oyunu hiçbir zaman çökertmez;
    /// bunun yerine OnErrorOccurred ile kullanıcıya anlamlı bir geri bildirim verilir.
    /// </summary>
    public class NpcDialogueController : MonoBehaviour
    {
        [SerializeField] private NpcPersona persona;
        [SerializeField] private string ollamaBaseUrl = "http://localhost:11434";
        [SerializeField] private string modelName = "llama3.2:1b";
        [SerializeField] private int timeoutSeconds = 20;

        public event Action OnThinkingStarted;
        public event Action<NpcAction> OnReplyReceived;
        public event Action<string> OnErrorOccurred;

        private IOllamaClient _client;
        private CancellationTokenSource _activeRequest;

        private void Awake()
        {
            _client = new OllamaClient(ollamaBaseUrl, modelName, timeoutSeconds);
        }

        /// <summary>UI'dan (örn. bir input alanının submit olayından) çağrılır.</summary>
        public async void SendPlayerMessage(string playerText)
        {
            if (persona == null)
            {
                OnErrorOccurred?.Invoke("NPC kişiliği atanmamış (NpcPersona eksik).");
                return;
            }

            _activeRequest?.Cancel();
            _activeRequest = new CancellationTokenSource();
            CancellationToken token = _activeRequest.Token;

            OnThinkingStarted?.Invoke();

            try
            {
                NpcAction result = await _client.SendMessageAsync(persona.BuildSystemPrompt(), playerText, token);
                if (token.IsCancellationRequested) return;

                OnReplyReceived?.Invoke(result);
                ApplyAction(result.GetActionType());
            }
            catch (OperationCanceledException)
            {
                // Bu istek, oyuncunun gönderdiği daha yeni bir mesaj tarafından iptal edildi.
            }
            catch (OllamaConnectionException ex)
            {
                Debug.LogWarning($"[NPC] Bağlantı sorunu: {ex.Message}");
                OnErrorOccurred?.Invoke("NPC şu an seni duyamıyor. Ollama'nın çalıştığından emin ol.");
            }
            catch (OllamaParseException ex)
            {
                Debug.LogWarning($"[NPC] Ayrıştırma sorunu: {ex.Message}");
                OnErrorOccurred?.Invoke("NPC'nin kafası biraz karıştı, tekrar söyler misin?");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NPC] Beklenmeyen hata: {ex}");
                OnErrorOccurred?.Invoke("Beklenmeyen bir sorun oluştu.");
            }
        }

        private void ApplyAction(NpcActionType actionType)
        {
            switch (actionType)
            {
                case NpcActionType.OpenDoor:
                    GameEvents.RaiseNpcUnlockedDoor();
                    break;
                case NpcActionType.EndConversation:
                    GameEvents.RaiseDialogueEnded();
                    break;
            }
        }

        private void OnDestroy()
        {
            _activeRequest?.Cancel();
        }
    }
}
