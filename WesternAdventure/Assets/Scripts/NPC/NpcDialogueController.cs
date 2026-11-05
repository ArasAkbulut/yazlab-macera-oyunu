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
        private bool _awaitingReply;
        // Konuşmanın soru/cevap dökümü: her turda tamamı LLM'e gönderilir, böylece muhafız
        // oyuncunun önceki cevaplarını da değerlendirir (çelişki, güven örneği vb.).
        private readonly List<string> _questions = new List<string>();
        private readonly List<string> _answers = new List<string>();

        /// <summary>Oyuncunun şu ana kadar verdiği cevap sayısı (1'den başlar).</summary>
        public int CurrentTurn => _currentTurn;
        public int MaxTurns => persona != null ? persona.MaxConversationTurns : 0;

        private void Awake()
        {
            _client = new OllamaClient(ollamaBaseUrl, modelName, timeoutSeconds);
        }

        /// <summary>
        /// Konuşmayı baştan başlatır. openingLine (muhafızın açılış sorusu) dökümün ilk sorusu olur.
        /// </summary>
        public void ResetConversation(string openingLine = null)
        {
            _activeRequest?.Cancel();
            _currentTurn = 0;
            _awaitingReply = false;
            _questions.Clear();
            _answers.Clear();
            _questions.Add(string.IsNullOrEmpty(openingLine) ? "Hazineye neden layıksın?" : openingLine);
        }

        /// <summary>UI'dan (örn. bir input alanının submit olayından) çağrılır.</summary>
        public async void SendPlayerMessage(string playerText)
        {
            if (persona == null)
            {
                OnErrorOccurred?.Invoke("NPC kişiliği atanmamış (NpcPersona eksik).");
                return;
            }

            // Önceki istek hâlâ bekliyorsa (oyuncu yeni mesaj gönderdi) onun cevabını dökümden çıkar.
            if (_awaitingReply) DropLastAnswer();
            _activeRequest?.Cancel();
            _activeRequest = new CancellationTokenSource();
            CancellationToken token = _activeRequest.Token;

            _currentTurn++;
            OnThinkingStarted?.Invoke();

            if (_questions.Count == 0) _questions.Add("Hazineye neden layıksın?");
            _answers.Add(playerText);

            try
            {
                string systemPrompt = persona.BuildSystemPrompt(_currentTurn);
                string transcript = NpcPersona.BuildTranscript(_questions, _answers);
                _awaitingReply = true;
                NpcAction result = await _client.SendMessageAsync(systemPrompt, null, transcript, token);
                if (token.IsCancellationRequested) return; // daha yeni bir mesaj bu isteği geçersiz kıldı
                _awaitingReply = false;

                // Kararı model değil kod verir: model değerlendirme alanlarını doldurur.
                //  - hakaret -> hemen saldırı (hangi turda olursa olsun)
                //  - son tur -> tüm koşullar sağlandıysa geçiş, değilse saldırı
                //  - ara tur -> modelin kısa tepkisi + sıradaki sabit sorgu sorusu
                // Böylece son tur hiçbir zaman kararsız kalmaz ve gösterilen replik kararla çelişmez.
                NpcActionType actionType;
                bool finalTurn = _currentTurn >= persona.MaxConversationTurns;
                if (result.hakaret)
                {
                    actionType = NpcActionType.AttackPlayer;
                    result.reply = persona.InsultLine;
                    result.disposition = "hostile";
                }
                else if (finalTurn)
                {
                    actionType = result.IsConvincing ? NpcActionType.OpenDoor : NpcActionType.AttackPlayer;
                    result.reply = result.IsConvincing ? persona.PassLine : persona.FailLine;
                    result.disposition = result.IsConvincing ? "friendly" : "hostile";
                }
                else
                {
                    actionType = NpcActionType.None;
                    string question = persona.GetQuestionAfterTurn(_currentTurn);
                    result.reply = $"{result.reply?.Trim()} {question}".Trim();
                    _questions.Add(question);
                }
                result.action = actionType switch
                {
                    NpcActionType.OpenDoor => "open_door",
                    NpcActionType.AttackPlayer => "attack_player",
                    _ => "none"
                };

                Debug.Log($"[NPC] Tur {_currentTurn}: hakaret={result.hakaret}, yardım={result.yardim_amaci}, " +
                          $"güven={result.guven_ornegi}, çelişki={result.celiski}, alakasız={result.alakasiz} -> {result.action}");
                OnReplyReceived?.Invoke(result);
                ApplyAction(actionType);
            }
            catch (OperationCanceledException)
            {
                // Bu istek, oyuncunun gönderdiği daha yeni bir mesaj tarafından iptal edildi
                // (cevabı dökümden SendPlayerMessage başında zaten çıkarıldı).
            }
            catch (OllamaConnectionException ex)
            {
                if (token.IsCancellationRequested) return;
                Debug.LogWarning($"[NPC] Bağlantı sorunu: {ex.Message}");
                _awaitingReply = false;
                DropLastAnswer(); // başarısız istek oyuncunun cevap hakkından düşmesin
                OnErrorOccurred?.Invoke("NPC şu an seni duyamıyor. Ollama'nın çalıştığından emin ol.");
            }
            catch (OllamaParseException ex)
            {
                if (token.IsCancellationRequested) return;
                Debug.LogWarning($"[NPC] Ayrıştırma sorunu: {ex.Message}");
                _awaitingReply = false;
                DropLastAnswer(); // başarısız istek oyuncunun cevap hakkından düşmesin
                OnErrorOccurred?.Invoke("NPC'nin kafası biraz karıştı, tekrar söyler misin?");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NPC] Beklenmeyen hata: {ex}");
                OnErrorOccurred?.Invoke("Beklenmeyen bir sorun oluştu.");
            }
        }

        private void DropLastAnswer()
        {
            if (_answers.Count > 0) _answers.RemoveAt(_answers.Count - 1);
            if (_currentTurn > 0) _currentTurn--;
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
