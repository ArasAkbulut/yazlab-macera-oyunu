using UnityEngine;

[DisallowMultipleComponent]
public class BossHealth : EnemyHealth
{
    private BossController bossController;

    private void Reset()
    {
        maxHealth = 150;
    }

    protected override void Awake()
    {
        base.Awake();
        bossController = GetComponent<BossController>();
    }

    protected override void PlayHitReaction()
    {
        // Boss hasar alýr fakat her yumrukta saldýrý animasyonu kesilmez.
        // Böylece saldýrý ve dinlenme döngüsü devam eder.
    }

    protected override void Die()
    {
        // Bekleyen vuruþun hasarýný ve takibi durdur.
        if (bossController != null)
        {
            bossController.enabled = false;
        }

        StopAgent();

        Debug.Log("Boss yenildi!", this);

        // Ölüm animasyonu baðlanana kadar boss'u gizle.
        gameObject.SetActive(false);
    }
}