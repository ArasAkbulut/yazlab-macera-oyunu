using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private int punchDamage = 10;
    [SerializeField] private float punchRange = 2.2f;
    [SerializeField] private float punchCooldown = 1.2f;
    [SerializeField] private float hitDelay = 0.35f;

    [Tooltip("Oyuncunun önündeki toplam vuruþ açýsý.")]
    [SerializeField] private float punchAngle = 120f;

    private Animator animator;
    private PlayerHealth playerHealth;

    private float nextPunchTime;
    private bool isPunching;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (playerHealth.IsDead || animator == null || isPunching)
        {
            return;
        }

        Mouse mouse = Mouse.current;

        if (mouse != null &&
            mouse.leftButton.wasPressedThisFrame &&
            Time.time >= nextPunchTime)
        {
            nextPunchTime = Time.time + punchCooldown;
            StartCoroutine(Punch());
        }
    }

    private IEnumerator Punch()
    {
        isPunching = true;
        animator.SetTrigger("Attack");

        // Yumruðun ileri uzandýðý ana yaklaþmak için bekle.
        yield return new WaitForSeconds(hitDelay);

        if (!playerHealth.IsDead)
        {
            HitClosestEnemy();
        }

        float remainingTime = Mathf.Max(0f, punchCooldown - hitDelay);
        yield return new WaitForSeconds(remainingTime);

        isPunching = false;
    }

    private void HitClosestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            punchRange,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        EnemyHealth closestEnemy = null;
        float closestDistance = float.PositiveInfinity;

        float minimumDot = Mathf.Cos(
            punchAngle * 0.5f * Mathf.Deg2Rad
        );

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();

            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            Vector3 offset = enemy.transform.position - transform.position;
            float distance = offset.magnitude;

            if (distance > punchRange)
            {
                continue;
            }

            Vector3 flatDirection = offset;
            flatDirection.y = 0f;

            // Oyuncunun arkasýndaki düþmanlara vurma.
            if (flatDirection.sqrMagnitude > 0.001f &&
                Vector3.Dot(transform.forward, flatDirection.normalized)
                    < minimumDot)
            {
                continue;
            }

            // Duvar veya kaya arkasýndaki düþmana vurma.
            if (distance > 0.001f)
            {
                RaycastHit[] blockers = Physics.RaycastAll(
                    transform.position,
                    offset.normalized,
                    distance,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );

                bool blocked = false;

                foreach (RaycastHit blocker in blockers)
                {
                    Transform hitTransform = blocker.collider.transform;

                    if (hitTransform.IsChildOf(transform) ||
                        hitTransform.IsChildOf(enemy.transform))
                    {
                        continue;
                    }

                    blocked = true;
                    break;
                }

                if (blocked)
                {
                    continue;
                }
            }

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy != null)
        {
            closestEnemy.TakeDamage(punchDamage);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isPunching = false;
    }
}