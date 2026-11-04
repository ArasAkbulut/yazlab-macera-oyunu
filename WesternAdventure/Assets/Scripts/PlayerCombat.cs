using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private int punchDamage = 10;
    [SerializeField] private float punchRange = 2.2f;
    [SerializeField] private float punchCooldown = 0.7f;
    [SerializeField] private float hitDelay = 0.2f;

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

        yield return new WaitForSeconds(hitDelay);

        if (!playerHealth.IsDead)
        {
            HitClosestEnemy();
        }

        float remainingTime = Mathf.Max(
            0f,
            punchCooldown - hitDelay
        );

        yield return new WaitForSeconds(remainingTime);

        isPunching = false;
    }

    private void HitClosestEnemy()
    {
        Vector3 origin = transform.position;

        Collider[] hits = Physics.OverlapSphere(
            origin,
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
            // BossHealth de EnemyHealth'ten türediði için bulunur.
            EnemyHealth enemy =
                hit.GetComponentInParent<EnemyHealth>();

            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            Vector3 targetPoint = hit.ClosestPoint(origin);
            Vector3 offset = targetPoint - origin;
            float distance = offset.magnitude;

            if (distance > punchRange)
            {
                continue;
            }

            Vector3 flatDirection = offset;
            flatDirection.y = 0f;

            // Collider'ýn içindeysek yönü merkezinden belirle.
            if (flatDirection.sqrMagnitude < 0.001f)
            {
                flatDirection = hit.bounds.center - origin;
                flatDirection.y = 0f;
            }

            if (flatDirection.sqrMagnitude > 0.001f &&
                Vector3.Dot(
                    transform.forward,
                    flatDirection.normalized
                ) < minimumDot)
            {
                continue;
            }

            if (IsBlocked(origin, targetPoint, enemy))
            {
                continue;
            }

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        // Bir yumruk, bir hedefe yalnýzca bir kez hasar verir.
        if (closestEnemy != null)
        {
            closestEnemy.TakeDamage(punchDamage);
        }
    }

    private bool IsBlocked(
        Vector3 origin,
        Vector3 targetPoint,
        EnemyHealth enemy)
    {
        Vector3 offset = targetPoint - origin;
        float distance = offset.magnitude;

        if (distance < 0.001f)
        {
            return false;
        }

        RaycastHit[] blockers = Physics.RaycastAll(
            origin,
            offset.normalized,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit blocker in blockers)
        {
            Transform hitTransform = blocker.collider.transform;

            // Oyuncunun ve hedefin kendi collider'larýný engel sayma.
            if (hitTransform.IsChildOf(transform) ||
                hitTransform.IsChildOf(enemy.transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isPunching = false;
    }
}