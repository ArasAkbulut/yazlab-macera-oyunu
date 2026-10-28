using System.Threading;
using System.Threading.Tasks;

namespace WesternAdventure.NPC
{
    /// <summary>
    /// NpcDialogueController'ın somut HTTP/Ollama detaylarından bağımsız çalışmasını sağlayan arayüz
    /// (test ederken veya modeli değiştirirken sahte bir implementasyonla değiştirilebilir).
    /// </summary>
    public interface IOllamaClient
    {
        Task<NpcAction> SendMessageAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken);
    }
}
