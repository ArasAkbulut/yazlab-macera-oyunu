using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WesternAdventure.NPC
{
    /// <summary>Konuşma geçmişindeki tek bir mesaj ("assistant" = NPC, "user" = oyuncu).</summary>
    public readonly struct DialogueTurn
    {
        public readonly string Role;
        public readonly string Content;

        public DialogueTurn(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    /// <summary>
    /// NpcDialogueController'ın somut HTTP/Ollama detaylarından bağımsız çalışmasını sağlayan arayüz
    /// (test ederken veya modeli değiştirirken sahte bir implementasyonla değiştirilebilir).
    /// </summary>
    public interface IOllamaClient
    {
        /// <param name="history">Bu mesajdan önceki konuşma (eski turlar); NPC'nin önceki
        /// cevapları hatırlayıp onlara dayanan takip soruları sorabilmesi için.</param>
        Task<NpcAction> SendMessageAsync(string systemPrompt, IReadOnlyList<DialogueTurn> history,
            string userMessage, CancellationToken cancellationToken);
    }
}
