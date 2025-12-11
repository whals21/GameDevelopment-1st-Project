using UnityEngine;

[RequireComponent(typeof(EnemyCore))]
public class RangedShooter : MonoBehaviour
{
    private EnemyCore core;
    private float lastShootTime;

    private void Awake()
    {
        core = GetComponent<EnemyCore>();
    }

    private void Update()
    {
        if (!core.IsRanged || core.Target == null) return;
        if (core.ProjectilePrefab == null) return;

        // 쿨타임 체크
        if (Time.time - lastShootTime < core.AttackDelay) return;

        // 여기서 직접 레이 체크!
        if (CanSeeTarget())
        {
            Shoot();
            lastShootTime = Time.time;
        }
    }

    private bool CanSeeTarget()
    {
        Vector2 origin = core.FirePoint.position;
        Vector2 direction = (core.Target.position - (Vector3)origin).normalized;
        float distance = Vector2.Distance(origin, core.Target.position);

        if (distance > core.AttackRange) return false;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, core.AttackRange);

        // 디버그용 레이
        Debug.DrawRay(origin, direction * core.AttackRange,
                      hit && hit.transform == core.Target ? Color.green : Color.red);

        return hit.collider != null && hit.transform == core.Target;
    }

    private void Shoot()
    {
        Vector3 spawnPos = core.FirePoint.position;
        Vector3 direction = (core.Target.position - spawnPos).normalized;

        GameObject projectile = Instantiate(core.ProjectilePrefab, spawnPos, Quaternion.identity);

        // 방향만 전달 (나중에 Projectile 스크립트에서 처리)
        // 예: projectile.GetComponent<Projectile>().Setup(direction);
    }
}