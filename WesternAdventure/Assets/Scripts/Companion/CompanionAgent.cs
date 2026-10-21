using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace WesternAdventure.Companion
{
    /// <summary>
    /// Pekiştirmeli öğrenme (RL) ile eğitilecek otonom companion.
    /// Gözlem olarak kendi konumunu, hedef engelin konumunu ve hızını kullanır; buna ek
    /// olarak sahnede bir RayPerceptionSensorComponent2D eklenmesi beklenir (Inspector
    /// üzerinden, kod gerektirmez) — bu, gereksinimdeki "sensör tabanlı gözlem (Raycast)"
    /// maddesini karşılar. Ödül: engele ulaşınca +1, her adımda küçük zaman cezası.
    /// Bu sınıfın derlenebilmesi için Packages/manifest.json'a eklenen
    /// com.unity.ml-agents paketinin Unity tarafından indirilmiş olması gerekir.
    /// </summary>
    public class CompanionAgent : Agent, ICompanionAgent
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private float moveSpeed = 4f;

        private Transform _obstacleTarget;

        public bool IsBusy { get; private set; }

        public override void OnEpisodeBegin()
        {
            IsBusy = false;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.position);
            sensor.AddObservation(_obstacleTarget != null ? _obstacleTarget.position : Vector3.zero);
            sensor.AddObservation(body != null ? body.linearVelocity : Vector2.zero);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!IsBusy || body == null) return;

            float moveX = actions.ContinuousActions[0];
            float moveY = actions.ContinuousActions[1];
            body.linearVelocity = new Vector2(moveX, moveY) * moveSpeed;

            AddReward(-0.001f);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
            continuousActions[0] = Input.GetAxisRaw("Horizontal");
            continuousActions[1] = Input.GetAxisRaw("Vertical");
        }

        public void CommandClearObstacle(Transform obstacleTarget)
        {
            _obstacleTarget = obstacleTarget;
            IsBusy = true;
            RequestDecision();
        }

        /// <summary>Engel aşıldığında tetiklenecek trigger/collision koduyla çağrılmalıdır.</summary>
        public void NotifyObstacleCleared()
        {
            IsBusy = false;
            SetReward(1f);
            Core.GameEvents.RaiseCompanionClearedObstacle();
            EndEpisode();
        }
    }
}
