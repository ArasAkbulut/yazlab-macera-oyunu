using UnityEngine;

namespace WesternAdventure.Companion
{
    /// <summary>
    /// Companion'ın ulaşması gereken hedef/engel noktası. Trigger collider'a sahip olmalıdır.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TargetZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var agent = other.GetComponentInParent<CompanionAgent>();
            if (agent != null)
            {
                agent.NotifyObstacleCleared();
            }
        }
    }
}
