using UnityEngine;
using UnityEngine.AI;
using WesternAdventure.Core;

namespace WesternAdventure.Companion
{
    // Gameplay driver. The original ML-Agents implementation remains available for training.
    [RequireComponent(typeof(CharacterController))]
    public class CompanionGuide : MonoBehaviour, ICompanionAgent
    {
        public Transform player;
        public Transform entrance;
        public Transform leverTarget;
        public Transform leverHandle;
        public float speed = 4.5f;
        public bool IsBusy => phase != 0;
        public bool LeverOpened { get; private set; }
        private CharacterController controller;
        private NavMeshPath path;
        private int corner;
        private float repathAt, verticalSpeed, waitUntil;
        private int phase; // follow, approach, enter, operate, exit
        private Animator animator;
        private int speedParameter;
        private bool hasSpeed;
        private bool hasAnimationId;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            // Companion must not body-block combatants; world collision still restricts the passage.
            foreach (var collider in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if (collider != controller && (collider.GetComponentInParent<PlayerHealth>() != null || collider.GetComponentInParent<EnemyHealth>() != null))
                    Physics.IgnoreCollision(controller, collider);
            path = new NavMeshPath();
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                hasAnimationId = System.Array.Exists(animator.parameters, p => p.name == "AnimationID" && p.type == AnimatorControllerParameterType.Int);
                foreach (var p in animator.parameters)
                    if (p.type == AnimatorControllerParameterType.Float && p.name.ToLower().Contains("speed"))
                    { speedParameter = p.nameHash; hasSpeed = true; break; }
                animator.applyRootMotion = false;
            }
        }

        public void CommandClearObstacle(Transform target)
        {
            if (IsBusy || LeverOpened || target == null || entrance == null) return;
            leverTarget = target;
            phase = 1;
            repathAt = 0;
        }

        private void Update()
        {
            if (Time.timeScale == 0 || player == null) return;
            Vector3 destination = player.position - player.forward * 1.8f + player.right * 1.2f;
            bool direct = false;
            if (phase == 1) destination = entrance.position;
            if (phase == 2 || phase == 3) { destination = leverTarget.position; direct = true; }
            if (phase == 4) { destination = entrance.position; direct = true; }
            float flatDistance = FlatDistance(transform.position, destination);
            float stop = phase == 0 ? 1.1f : phase == 1 ? 0.45f : phase == 2 ? .7f : 0.25f;
            Vector3 direction = Vector3.zero;
            if (phase == 3)
            {
                if (leverHandle != null)
                    leverHandle.localRotation = Quaternion.RotateTowards(leverHandle.localRotation, Quaternion.Euler(0, 0, -45), 70 * Time.deltaTime);
                if (Time.time >= waitUntil)
                {
                    LeverOpened = true;
                    GameEvents.RaiseCompanionClearedObstacle();
                    phase = 4;
                }
            }
            else if (flatDistance > stop)
            {
                if (direct) direction = destination - transform.position;
                else
                {
                    if (Time.time >= repathAt)
                    {
                        repathAt = Time.time + 0.5f;
                        corner = 1;
                        path.ClearCorners();
                        if (NavMesh.SamplePosition(transform.position, out var from, 3, NavMesh.AllAreas) &&
                            NavMesh.SamplePosition(destination, out var to, 3, NavMesh.AllAreas))
                            NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path);
                    }
                    if (corner < path.corners.Length)
                    {
                        if (FlatDistance(transform.position, path.corners[corner]) < 0.35f) corner++;
                        if (corner < path.corners.Length) direction = path.corners[corner] - transform.position;
                    }
                }
            }
            else if (phase == 1) phase = 2;
            else if (phase == 2) { phase = 3; waitUntil = Time.time + 1; }
            else if (phase == 4) { phase = 0; repathAt = 0; }

            direction.y = 0;
            direction = direction.normalized;
            // A short sensor prevents pushing blindly into obstacles; the controller handles contact.
            if (direction.sqrMagnitude > 0.01f && Physics.SphereCast(transform.position + Vector3.up * 0.55f,
                0.2f, direction, out var obstruction, 0.45f, 1 << LayerMask.NameToLayer("Environment"), QueryTriggerInteraction.Ignore))
            {
                if (obstruction.normal.y < 0.5f) { direction = Vector3.ProjectOnPlane(direction, obstruction.normal); direction.y = 0; }
            }
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed = Mathf.Max(-30, verticalSpeed - 20 * Time.deltaTime);
            controller.Move((direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 360 * Time.deltaTime);
            if (hasSpeed) animator.SetFloat(speedParameter, direction.magnitude * speed);
            if (hasAnimationId) animator.SetInteger("AnimationID", direction.sqrMagnitude > .01f ? 4 : 0);
        }

        private static float FlatDistance(Vector3 a, Vector3 b) { a.y = b.y; return Vector3.Distance(a, b); }
    }
}
