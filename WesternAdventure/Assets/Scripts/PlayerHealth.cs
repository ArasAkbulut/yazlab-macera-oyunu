using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Can")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Can yenileme")]
    [SerializeField] private float regenerationDelay = 3f;
    [SerializeField] private float regenerationPerSecond = 10f;

    private Animator animator;
    private float lastDamageTime = float.NegativeInfinity;
    private float regenerationProgress;

    private static readonly int IsRunning =
        Animator.StringToHash("IsRunning");

    private static readonly int IsDeadParameter =
        Animator.StringToHash("IsDead");

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (IsDead || currentHealth >= maxHealth)
        {
            return;
        }

        if (Time.time - lastDamageTime < regenerationDelay)
        {
            return;
        }

        regenerationProgress += regenerationPerSecond * Time.deltaTime;

        int amountToHeal = Mathf.FloorToInt(regenerationProgress);

        if (amountToHeal > 0)
        {
            currentHealth = Mathf.Min(
                maxHealth,
                currentHealth + amountToHeal
            );

            regenerationProgress -= amountToHeal;

            if (currentHealth >= maxHealth)
            {
                regenerationProgress = 0f;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);

        lastDamageTime = Time.time;
        regenerationProgress = 0f;

        Debug.Log($"Oyuncu caný: {currentHealth}/{maxHealth}", this);

        if (IsDead)
        {
            Die();
        }
    }

    private void Die()
    {
        if (animator != null)
        {
            animator.SetBool(IsRunning, false);
            animator.SetBool(IsDeadParameter, true);
        }

        Debug.Log("Oyuncu öldü.", this);
    }
}