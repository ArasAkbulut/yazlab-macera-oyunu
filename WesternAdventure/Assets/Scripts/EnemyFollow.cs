using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyFollow : MonoBehaviour
{
    [Header("Baðlantýlar")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator enemyAnimator;

    [Header("Takip")]
    [SerializeField] private float detectionRange = 20f;

    [Header("Saldýrý")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float hitDelay = 0.45f;
    [SerializeField] private float attackDuration = 1.2f;

    private NavMeshAgent agent;
    private PlayerHealth playerHealth;
    private float nextPathUpdate;
    private float nextAttackTime;
    private bool isAttacking;

    private static readonly int IsRunning =
        Animator.StringToHash("IsRunning");

    private static readonly int Attack =
        Animator.StringToHash("Attack");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogWarning(
                $"{name}: Player baðlantýsýný ve PlayerHealth bileþenini kontrol et.",
                this);
        }
    }

    private void Update()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            SetRunning(false);
            return;
        }

        if (player == null || playerHealth == null || playerHealth.IsDead)
        {
            StopMoving();
            return;
        }

        // Saldýrý bitmeden tekrar yürümeye baþlama.
        if (isAttacking)
        {
            StopMoving();
            return;
        }

        float distance = Vector3.Distance(
            transform.position, player.position);

        if (distance > detectionRange)
        {
            StopMoving();
            return;
        }

        if (distance <= attackRange)
        {
            StopMoving();
            FacePlayer();

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                StartCoroutine(PerformAttack());
            }

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

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        SetRunning(false);

        if (enemyAnimator != null)
        {
            enemyAnimator.SetTrigger(Attack);
        }

        // Yumruðun hedefe ulaþmasý için kýsa süre bekle.
        yield return new WaitForSeconds(hitDelay);

        if (player != null && playerHealth != null && !playerHealth.IsDead)
        {
            float distance = Vector3.Distance(
                transform.position, player.position);

            // Oyuncu uzaklaþtýysa bu vuruþ hasar vermez.
            if (distance <= attackRange)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

        float remainingTime = Mathf.Max(0f, attackDuration - hitDelay);
        yield return new WaitForSeconds(remainingTime);

        isAttacking = false;
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

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                agent.angularSpeed * Time.deltaTime);
        }
    }

    private void SetRunning(bool running)
    {
        if (enemyAnimator != null)
        {
            enemyAnimator.SetBool(IsRunning, running);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isAttacking = false;
        SetRunning(false);
    }
}