using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WesternAdventure.Companion
{
    /// <summary>
    /// Pekiştirmeli öğrenme (RL) ile eğitilecek otonom 3D companion (CharacterController
    /// tabanlı oyuncu hareketiyle aynı "Environment" layer'ını ve yerçekimi mantığını
    /// kullanır — bkz. arkadaşın PlayerMovement.cs'i). Gözlem olarak kendi konumunu,
    /// hedefin konumunu ve hızını kullanır; buna ek olarak sahnede bir
    /// RayPerceptionSensorComponent3D eklenmesi beklenir (Inspector üzerinden, kod
    /// gerektirmez) — bu, gereksinimdeki "sensör tabanlı gözlem (Raycast)" maddesini
    /// karşılar. Ödül: hedefe ulaşınca +1, her adımda küçük zaman cezası, ayrıca
    /// hedefe yaklaştıkça küçük bir mesafe ödülü (eğitimi hızlandırır).
    /// obstacleTarget Inspector'dan doğrudan atanırsa Agent eğitim sırasında bağımsız
    /// çalışır; oyun içinde CommandClearObstacle ile de tetiklenebilir.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CompanionAgent : Agent, ICompanionAgent
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private Transform obstacleTarget;
        [Tooltip("Eğitim/test sahnelerinde true: her episode başında başlangıç noktasına döner ve " +
                 "görevi tekrar dener. Gerçek oyun sahnesinde false: engeli bir kez aştıktan sonra " +
                 "olduğu yerde kalır (sürekli başa ışınlanıp tekrar yürümez).")]
        [SerializeField] private bool returnToStartOnEpisodeBegin = true;

        private CharacterController _controller;
        private Vector3 _startPosition;
        private float _verticalSpeed;
        private float _previousDistance;

        public bool IsBusy { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _startPosition = transform.position;
        }

        public override void OnEpisodeBegin()
        {
            if (!returnToStartOnEpisodeBegin)
            {
                IsBusy = false;
                return;
            }

            _controller.enabled = false;
            transform.position = _startPosition;
            _controller.enabled = true;
            _verticalSpeed = 0f;

            IsBusy = obstacleTarget != null;
            _previousDistance = obstacleTarget != null
                ? Vector3.Distance(transform.position, obstacleTarget.position)
                : 0f;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.position);
            sensor.AddObservation(obstacleTarget != null ? obstacleTarget.position : Vector3.zero);
            sensor.AddObservation(_controller.velocity);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (!IsBusy) return;

            float moveX = actions.ContinuousActions[0];
            float moveZ = actions.ContinuousActions[1];
            var moveInput = new Vector3(moveX, 0f, moveZ);
            if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

            if (_controller.isGrounded && _verticalSpeed < 0f)
            {
                _verticalSpeed = -2f;
            }
            _verticalSpeed += gravity * Time.deltaTime;

            Vector3 velocity = moveInput * moveSpeed;
            velocity.y = _verticalSpeed;
            _controller.Move(velocity * Time.deltaTime);

            if (moveInput.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(moveInput);
            }

            AddReward(-0.001f);

            if (obstacleTarget != null)
            {
                float distance = Vector3.Distance(transform.position, obstacleTarget.position);
                AddReward((_previousDistance - distance) * 0.1f);
                _previousDistance = distance;
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
            float moveX = 0f;
            float moveZ = 0f;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveZ -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveZ += 1f;
            }

            continuousActions[0] = moveX;
            continuousActions[1] = moveZ;
        }

        public void CommandClearObstacle(Transform target)
        {
            obstacleTarget = target;
            IsBusy = true;
            RequestDecision();
        }

        /// <summary>Engel/hedef aşıldığında tetiklenecek trigger/collision koduyla çağrılmalıdır.</summary>
        public void NotifyObstacleCleared()
        {
            IsBusy = false;
            SetReward(1f);
            Core.GameEvents.RaiseCompanionClearedObstacle();
            EndEpisode();
        }
    }
}
