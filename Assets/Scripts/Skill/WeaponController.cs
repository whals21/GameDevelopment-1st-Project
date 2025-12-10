using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("무기 스탯")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 0.5f;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float attackRange = 10f;

    [Header("컴포넌트")]
    private PlayerStats playerStats;
    private float cooldownTimer = 0f;

    void Awake()
    {
        playerStats = GetComponentInParent<PlayerStats>();
    }

    void Update()
    {
        // 쿨다운 진행
        cooldownTimer += Time.deltaTime;

        // 쿨다운 완료 시 자동 공격
        if (cooldownTimer >= GetActualCooldown())
        {
            Fire();
            cooldownTimer = 0f;
        }
    }

    private void Fire()
    {
        // 1. 가장 가까운 적 찾기
        Enemy target = FindNearestEnemy();
        if (target == null) return;

        // 2. 방향 계산
        Vector3 direction = (target.transform.position - transform.position).normalized;

        // 3. 오브젝트 풀에서 총알 가져오기
        Projectile projectile = ObjectPoolManager.Instance
            .GetPool<Projectile>()
            .Get();

        // 4. 총알 초기화
        float actualDamage = damage; 
        //float actualDamage = damage * playerStats.DamageMultiplier;
        projectile.transform.position = transform.position;
        projectile.Init(actualDamage, projectileSpeed, direction);
    }

    private Enemy FindNearestEnemy()
    {
        // 모든 적 가져오기
        Enemy[] enemies = FindObjectsOfType<Enemy>();

        Enemy nearest = null;
        float minDistance = attackRange;

        foreach (Enemy enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private float GetActualCooldown()
    {
        // 플레이어 쿨다운 감소 반영
        //return cooldown * (1f - playerStats.CooldownReduction);
        return cooldown;
    }
}