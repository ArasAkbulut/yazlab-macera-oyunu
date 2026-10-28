using UnityEngine;

namespace WesternAdventure.Companion
{
    /// <summary>
    /// Oyuncunun companion'a emir verdiği noktalarda (örn. "şu engeli aş") kullanılan
    /// soyutlama; NavMesh veya ML-Agents tabanlı implementasyonlar bu arayüz üzerinden
    /// birbirinin yerine konabilir (çağıran kod somut sınıfı bilmek zorunda kalmaz).
    /// </summary>
    public interface ICompanionAgent
    {
        bool IsBusy { get; }
        void CommandClearObstacle(Transform obstacleTarget);
    }
}
