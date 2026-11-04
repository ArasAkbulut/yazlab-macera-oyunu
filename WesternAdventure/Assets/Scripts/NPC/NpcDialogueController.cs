using System;
using System.Collections.Generic;
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
        [SerializeField] private string modelName = "llama3.2:3b";
        [SerializeField] private int timeoutSeconds = 20;

        public event Action OnThinkingStarted;
        public event Action<NpcAction> OnReplyReceived;
        public event Action<string> OnErrorOccurred;

        private IOllamaClient _client;
        private CancellationTokenSource _activeRequest;
        private int _currentTurn;
        private readonly List<DialogueTurn> _history = new List<DialogueTurn>();

        /// <summary>Oyuncunun şu ana kadar verdiği cevap sayısı (1'den başlar).</summary>
        public int CurrentTurn => _currentTurn;
        public int MaxTurns => persona != null ? persona.MaxConversationTurns : 0;

        private void Awake()
        {
            _client = new OllamaClient(ollamaBaseUrl, modelName, timeoutSeconds);
        }

        /// <summary>
        /// Konuşmayı baştan başlatır. openingLine verilirse (ör. "Hazineye neden layıksın?"),
        /// NPC'nin bunu zaten sorduğu geçmişe eklenir ki model aynı soruyu tekrar sormasın.
        /// </summary>
        public void ResetConversation(string openingLine = null)
        {
            _activeRequest?.Cancel();
            _currentTurn = 0;
            _history.Clear();
            if (!string.IsNullOrEmpty(openingLine))
            {
                var opening = new NpcAction { reply = openingLine, disposition = "neutral", action = "none" };
                _history.Add(new DialogueTurn("assistant", JsonUtility.ToJson(opening)));
            }
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

            _currentTurn++;
            OnThinkingStarted?.Invoke();

            try
            {
                string systemPrompt = persona.BuildSystemPrompt(_currentTurn);
                NpcAction result = await _client.SendMessageAsync(systemPrompt, _history, playerText, token);
                if (token.IsCancellationRequested) return;

                // Bu tur geçmişe ekleniyor: sonraki turda NPC oyuncunun önceki cevaplarını
                // hatırlayıp onlara dayanan takip soruları sorabilsin.
                _history.Add(new DialogueTurn("user", playerText));
                _history.Add(new DialogueTurn("assistant", JsonUtility.ToJson(result)));

                NpcActionType actionType = result.GetActionType();

                // Model son turda da kesin bir karara varmazsa (kurala uymazsa), güvenli bir
                // varsayılana düş: oyuncu ikna edemedi sayılır. Oyunun sonsuza dek "none"da
                // takılı kalmaması için savunmacı bir geri dönüş.
                if (actionType == NpcActionType.None && _currentTurn >= persona.MaxConversationTurns)
                {
                    actionType = NpcActionType.AttackPlayer;
                }

                OnReplyReceived?.Invoke(result);
                ApplyAction(actionType);
            }
            catch (OperationCanceledException)
            {
                // Bu istek, oyuncunun gönderdiği daha yeni bir mesaj tarafından iptal edildi.
            }
            catch (OllamaConnectionException ex)
            {
                Debug.LogWarning($"[NPC] Bağlantı sorunu: {ex.Message}");
                _currentTurn--; // başarısız istek oyuncunun cevap hakkından düşmesin
                OnErrorOccurred?.Invoke("NPC şu an seni duyamıyor. Ollama'nın çalıştığından emin ol.");
            }
            catch (OllamaParseException ex)
            {
                Debug.LogWarning($"[NPC] Ayrıştırma sorunu: {ex.Message}");
                _currentTurn--; // başarısız istek oyuncunun cevap hakkından düşmesin
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
                    GameEvents.RaiseDialogueEnded();
                    break;
                case NpcActionType.AttackPlayer:
                    GameEvents.RaiseNpcAttackedPlayer();
                    GameEvents.RaiseDialogueEnded();
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
