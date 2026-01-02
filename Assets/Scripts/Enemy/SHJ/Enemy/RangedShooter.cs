using UnityEngine;
using System.Collections.Generic;

public class RangedShooter : MonoBehaviour
{
    [Header("적들 부모 오브젝트")]
    [SerializeField] private Transform enemiesParent; // Enemys 빈 오브젝트 드래그 (모든 적들의 부모)

    [Header("총알 풀 컨테이너")]
    [SerializeField] private Transform bulletPoolContainer; // EnemyBullet 오브젝트 (총알 풀)

    private Queue<GameObject> bulletPool = new Queue<GameObject>(); // 재사용할 총알 풀 (비활성화된 총알들)
    private Dictionary<EnemyCore, float> lastShootTime = new Dictionary<EnemyCore, float>(); // 각 적별 마지막 발사 시간 (쿨타임 관리용)

    // 실시간으로 원거리 적들 추적
    private List<EnemyCore> rangedEnemies = new List<EnemyCore>(); // 현재 원거리 적 목록

    private void Awake()
    {
        // 게임 시작 시 총알 풀 초기화
        // bulletPoolContainer의 모든 자식 총알을 비활성화하고 풀에 넣음
        foreach (Transform child in bulletPoolContainer)
        {
            child.gameObject.SetActive(false);
            bulletPool.Enqueue(child.gameObject);
        }
    }

    private void Update()
    {
        // 1. Enemys 아래 있는 원거리 적 목록 갱신
        UpdateRangedEnemyList();

        // 2. 각 원거리 적에 대해 발사 조건 확인 및 발사
        foreach (EnemyCore core in rangedEnemies)
        {
            if (core == null || !core.IsRanged || core.Target == null) continue;

            if (!CanShootNow(core)) continue; // 쿨타임 체크

            if (CanSeePlayer(core)) // 레이로 플레이어 시야 확인
            {
                ShootFrom(core); // 발사 실행
                lastShootTime[core] = Time.time; // 발사 시간 기록
            }
        }
    }

    // Enemys 아래 있는 모든 원거리 적을 주기적으로 갱신
    // (매 프레임이 아니라 0.2초마다 한 번만 체크하여 성능 최적화)
    private float updateInterval = 0.2f;
    private float lastUpdateTime = 0f;
    private void UpdateRangedEnemyList()
    {
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        rangedEnemies.Clear();

        if (enemiesParent == null) return;

        foreach (Transform child in enemiesParent)
        {
            EnemyCore core = child.GetComponent<EnemyCore>();
            if (core != null && core.IsRanged)
            {
                rangedEnemies.Add(core);
            }
        }
    }

    // 해당 적이 쿨타임이 끝났는지 확인
    private bool CanShootNow(EnemyCore core)
    {
        if (!lastShootTime.ContainsKey(core))
            lastShootTime[core] = 0f;
        return Time.time - lastShootTime[core] >= core.AttackDelay;
    }

    // 레이캐스트를 사용해 플레이어가 시야에 있는지 확인 (벽 등 장애물 체크 포함)
    private bool CanSeePlayer(EnemyCore core)
    {
        Vector2 origin = core.FirePoint.position;
        Vector2 dir = (core.Target.position - (Vector3)origin).normalized;
        float dist = Vector2.Distance(origin, core.Target.position);

        if (dist > core.AttackRange) return false;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, core.AttackRange);
        Debug.DrawRay(origin, dir * core.AttackRange,
                     hit && hit.transform == core.Target ? Color.green : Color.red);

        return hit.collider != null && hit.transform == core.Target;
    }

    // 실제 발사 로직
    // 풀에서 총알 하나 꺼내서 위치/방향 설정 후 활성화
    private void ShootFrom(EnemyCore core)
    {
        if (bulletPool.Count == 0) return;

        GameObject bullet = bulletPool.Dequeue();

        // 각 적의 Gun/Bullet 외형 반영 (있으면 스프라이트 복사)
        Transform gun = core.transform.Find("Gun");
        if (gun != null)
        {
            Transform template = gun.Find("Bullet");
            if (template != null)
            {
                var sr = bullet.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sprite = template.GetComponent<SpriteRenderer>().sprite;
            }
        }

        Vector3 dir = (core.Target.position - core.FirePoint.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        bullet.transform.position = core.FirePoint.position;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
        bullet.SetActive(true);
    }

    // 총알이 사라지거나 플레이어와 충돌했을 때 호출
    // 총알을 비활성화하고 풀에 다시 넣음 (재사용 준비)
    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        bulletPool.Enqueue(bullet);
    }
}