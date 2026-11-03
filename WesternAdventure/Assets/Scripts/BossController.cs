using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BossController : MonoBehaviour
{
    private enum BossState
    {
        Waiting,
        Fighting,
        Attacking,
        Resting
    }

    [Header("Baglantilar")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator bossAnimator;

    [Header("Takip")]
    [SerializeField] private float detectionRange = 18f;

    [Header("Dovus dongusu")]
    [SerializeField] private float combatDuration = 15f;
    [SerializeField] private float restDuration = 4f;

    [Header("Saldiri")]
    [SerializeField] private float attackRange = 3.5f;
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float hitDelay = 0.55f;
    [SerializeField] private float attackDuration = 1.5f;

    [Range(1f, 180f)]
    [SerializeField] private float attackAngle = 120f;

    [Header("Durum - oyun sirasinda takip etmek icin")]
    [SerializeField] private BossState currentState = BossState.Waiting;

    private NavMeshAgent agent;
    private PlayerHealth playerHealth;

    private float combatEndTime;
    private float restEndTime;
    private float attackEndTime;
    private float hitTime;
    private float nextPathUpdate;
    private bool hitProcessed;

    private static readonly int IsRunning =
        Animator.StringToHash("IsRunning");

    private static readonly int Attack =
        Animator.StringToHash("Attack");

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

        if (playerHealth == null || bossAnimator == null)
        {
            Debug.LogWarning(
                "BossController: Player, PlayerHealth ve Animator baðlantýlarýný kontrol et.",
                this
            );
        }
    }

    private void Update()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            ResetEncounter();
            return;
        }

        if (player == null ||
            playerHealth == null ||
            bossAnimator == null ||
            playerHealth.IsDead)
        {
            ResetEncounter();
            return;
        }

        switch (currentState)
        {
            case BossState.Waiting:
                StopMoving();

                if (DistanceToPlayer() <= detectionRange)
                {
                    BeginCombat();
                }
                break;

            case BossState.Fighting:
                UpdateCombat();
                break;

            case BossState.Attacking:
                UpdateAttack();
                break;

            case BossState.Resting:
                StopMoving();

                if (Time.time >= restEndTime)
                {
                    if (DistanceToPlayer() <= detectionRange)
                    {
                        BeginCombat();
                    }
                    else
                    {
                        currentState = BossState.Waiting;
                    }
                }
                break;
        }
    }

    private void BeginCombat()
    {
        currentState = BossState.Fighting;
        combatEndTime = Time.time + combatDuration;
        nextPathUpdate = 0f;
    }

    private void UpdateCombat()
    {
        if (Time.time >= combatEndTime)
        {
            BeginRest();
            return;
        }

        float distance = DistanceToPlayer();

        if (distance > detectionRange)
        {
            // Oyuncu uzaklaþsa da mevcut 15 saniyelik süre sýfýrlanmaz.
            StopMoving();
            return;
        }

        if (distance <= attackRange)
        {
            StopMoving();
            FacePlayer();
            BeginAttack();
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

    private void BeginAttack()
    {
        currentState = BossState.Attacking;
        hitProcessed = false;

        float delay = Mathf.Max(0f, hitDelay);

        hitTime = Time.time + delay;
        attackEndTime = Time.time +
            Mathf.Max(attackDuration, delay + 0.1f);

        bossAnimator.ResetTrigger(Attack);
        bossAnimator.SetTrigger(Attack);
    }

    private void UpdateAttack()
    {
        StopMoving();

        // Her saldýrýda yalnýzca bir kez hasar kontrolü yap.
        if (!hitProcessed && Time.time >= hitTime)
        {
            hitProcessed = true;

            if (CanHitPlayer())
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

        if (Time.time >= attackEndTime)
        {
            if (Time.time >= combatEndTime)
            {
                BeginRest();
            }
            else
            {
                currentState = BossState.Fighting;
            }
        }
    }

    private bool CanHitPlayer()
    {
        if (playerHealth.IsDead || DistanceToPlayer() > attackRange)
        {
            return false;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return true;
        }

        float minimumDot = Mathf.Cos(
            attackAngle * 0.5f * Mathf.Deg2Rad
        );

        return Vector3.Dot(
            transform.forward,
            direction.normalized
        ) >= minimumDot;
    }

    private void BeginRest()
    {
        currentState = BossState.Resting;
        restEndTime = Time.time + restDuration;

        StopMoving();
        bossAnimator.ResetTrigger(Attack);
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private float DistanceToPlayer()
    {
        return Vector3.Distance(
            transform.position,
            player.position
        );
    }

    private void StopMoving()
    {
        if (agent != null &&
            agent.isActiveAndEnabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;

            if (agent.hasPath)
            {
                agent.ResetPath();
            }
        }

        SetRunning(false);
    }

    private void ResetEncounter()
    {
        currentState = BossState.Waiting;
        hitProcessed = true;
        StopMoving();

        if (bossAnimator != null)
        {
            bossAnimator.ResetTrigger(Attack);
        }
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
        ResetEncounter();
    }
}