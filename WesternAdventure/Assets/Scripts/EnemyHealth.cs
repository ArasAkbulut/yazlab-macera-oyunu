using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] protected int maxHealth = 50;
    [SerializeField] private int currentHealth;

    protected Animator animator;
    protected NavMeshAgent agent;

    private EnemyFollow enemyFollow;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;

        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        enemyFollow = GetComponent<EnemyFollow>();
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log($"{name} caný: {currentHealth}/{maxHealth}", this);

        if (IsDead)
        {
            Die();
        }
        else
        {
            PlayHitReaction();
        }
    }

    protected virtual void PlayHitReaction()
    {
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }

    protected virtual void Die()
    {
        if (enemyFollow != null)
        {
            enemyFollow.enabled = false;
        }

        StopAgent();

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Hit");
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsDead", true);
        }

        Debug.Log($"{name} öldü.", this);
    }

    protected void StopAgent()
    {
        if (agent != null &&
            agent.isActiveAndEnabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }
}