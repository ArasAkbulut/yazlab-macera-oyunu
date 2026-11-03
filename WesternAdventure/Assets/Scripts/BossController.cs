using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BossController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private float detectionRange = 18f;

    private NavMeshAgent agent;
    private PlayerHealth playerHealth;
    private float nextPathUpdate;

    private static readonly int IsRunning =
        Animator.StringToHash("IsRunning");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (bossAnimator == null)
        {
            bossAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            SetRunning(false);
            return;
        }

        if (player == null ||
            (playerHealth != null && playerHealth.IsDead))
        {
            StopMoving();
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance > detectionRange)
        {
            StopMoving();
            return;
        }

        agent.isStopped = false;

        if (Time.time >= nextPathUpdate)
        {
            agent.SetDestination(player.position);
            nextPathUpdate = Time.time + 0.2f;
        }

        SetRunning(agent.velocity.sqrMagnitude > 0.01f);
    }

    private void StopMoving()
    {
        agent.isStopped = true;

        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        SetRunning(false);
    }

    private void SetRunning(bool running)
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetBool(IsRunning, running);
        }
    }

    private void OnDisable()
    {
        if (agent != null &&
            agent.isActiveAndEnabled &&
            agent.isOnNavMesh)
        {
            StopMoving();
        }
        else
        {
            SetRunning(false);
        }
    }
}