using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace WesternAdventure.NPC
{
    /// <summary>
    /// Yerel Ollama sunucusuyla (http://localhost:11434) /api/chat üzerinden konuşan,
    /// ana oyun döngüsünü bloklamayan (async) istemci. Yanıtın yapılandırılmış JSON
    /// olması Ollama'nın "format":"json" moduyla istenir; yine de bozuk/beklenmedik
    /// çıktılar OllamaParseException olarak ayrıca ele alınır.
    /// </summary>
    public class OllamaClient : IOllamaClient
    {
        private readonly string _baseUrl;
        private readonly string _model;
        private readonly int _timeoutSeconds;

        public OllamaClient(string baseUrl, string model, int timeoutSeconds = 20)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _model = model;
            _timeoutSeconds = timeoutSeconds;
        }

        public async Task<NpcAction> SendMessageAsync(string systemPrompt, IReadOnlyList<DialogueTurn> history,
            string userMessage, CancellationToken cancellationToken)
        {
            var messages = new List<OllamaMessage>
            {
                new OllamaMessage { role = "system", content = systemPrompt }
            };
            if (history != null)
            {
                foreach (DialogueTurn turn in history)
                {
                    messages.Add(new OllamaMessage { role = turn.Role, content = turn.Content });
                }
            }
            messages.Add(new OllamaMessage { role = "user", content = userMessage });

            var request = new OllamaChatRequest
            {
                model = _model,
                stream = false,
                messages = messages.ToArray()
            };

            string requestJson = JsonUtility.ToJson(request);
            string responseText = await PostAsync($"{_baseUrl}/api/chat", requestJson, cancellationToken);

            OllamaChatResponse chatResponse;
            try
            {
                chatResponse = JsonUtility.FromJson<OllamaChatResponse>(responseText);
            }
            catch (Exception ex)
            {
                throw new OllamaParseException($"Ollama yanıt zarfı çözümlenemedi: {ex.Message}");
            }

            if (chatResponse?.message?.content == null)
            {
                throw new OllamaParseException("Ollama yanıtı beklenen alanları içermiyor.");
            }

            try
            {
                return JsonUtility.FromJson<NpcAction>(chatResponse.message.content);
            }
            catch (Exception ex)
            {
                throw new OllamaParseException($"Model çıktısı geçerli JSON değil: {ex.Message}");
            }
        }

        private async Task<string> PostAsync(string url, string jsonBody, CancellationToken cancellationToken)
        {
            using var www = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = _timeoutSeconds;

            UnityWebRequestAsyncOperation operation;
            try
            {
                operation = www.SendWebRequest();
            }
            catch (Exception ex)
            {
                throw new OllamaConnectionException($"Ollama'ya istek gönderilemedi: {ex.Message}");
            }

            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    www.Abort();
                    throw new OperationCanceledException(cancellationToken);
                }

                await Task.Yield();
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                throw new OllamaConnectionException(
                    $"Ollama sunucusuna ulaşılamadı veya zaman aşımına uğradı ({_baseUrl}): {www.error}");
            }

            return www.downloadHandler.text;
        }
    }
}
