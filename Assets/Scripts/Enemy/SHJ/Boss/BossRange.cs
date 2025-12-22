using UnityEngine;

public class BossRange : MonoBehaviour
{
    [SerializeField] private BossController boss;
    [SerializeField] private LayerMask targetLayer;

    public bool HasTarget { get; private set; }
    public Collider2D CurrentTarget { get; private set; }

    private void Update()
    {
        float range = boss.CurrentAttackRange;

        Collider2D hit = Physics2D.OverlapCircle(
            boss.transform.position,
            range,
            targetLayer
        );

        HasTarget = hit != null;
        CurrentTarget = hit;
    }

    private void OnDrawGizmosSelected()
    {
        if (boss == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            boss.transform.position,
            boss.CurrentAttackRange
        );
    }
}
