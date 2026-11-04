using UnityEngine;
using WesternAdventure.Core;

namespace WesternAdventure.Companion
{
    /// <summary>
    /// Companion, lever'a ulaşıp engeli aştığında (GameEvents.CompanionClearedObstacle)
    /// tünel kapısını yukarı kaydırarak açar ve çarpışmasını kapatır.
    /// </summary>
    public class TunnelGateController : MonoBehaviour
    {
        [SerializeField] private float openHeight = 10f;
        [SerializeField] private float openSpeed = 2f;

        private Vector3 _closedPosition;
        private Vector3 _openPosition;
        private bool _opening;

        private void Awake()
        {
            _closedPosition = transform.position;
            _openPosition = _closedPosition + Vector3.up * openHeight;
            GameEvents.CompanionClearedObstacle += HandleCompanionClearedObstacle;
        }

        private void OnDestroy()
        {
            GameEvents.CompanionClearedObstacle -= HandleCompanionClearedObstacle;
        }

        private void HandleCompanionClearedObstacle()
        {
            if (_opening) return;
            _opening = true;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            var obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obstacle != null) obstacle.enabled = false;
        }

        private void Update()
        {
            if (!_opening) return;
            transform.position = Vector3.MoveTowards(transform.position, _openPosition, openSpeed * Time.deltaTime);
        }
    }
}
