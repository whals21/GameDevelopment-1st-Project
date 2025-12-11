using UnityEngine;
using System.Collections.Generic;

public class RangedShooter : MonoBehaviour
{
    [Header("원거리 적들 (직접 드래그)")]
    [SerializeField] private EnemyCore[] rangedEnemies;  // 여기다가 원거리 적들 드래그!

    [Header("공유 총알 풀 컨테이너")]
    [SerializeField] private Transform bulletPoolContainer; // EnemyBullet 오브젝트

    private Queue<GameObject> bulletPool = new Queue<GameObject>();
    private Dictionary<EnemyCore, float> lastShootTime = new Dictionary<EnemyCore, float>();

    private void Awake()
    {
        // 풀 초기화
        foreach (Transform child in bulletPoolContainer)
        {
            child.gameObject.SetActive(false);
            bulletPool.Enqueue(child.gameObject);
        }
    }

    private void Update()
    {
        foreach (EnemyCore core in rangedEnemies)
        {
            if (core == null || !core.IsRanged || core.Target == null) continue;

            // 쿨타임 체크
            if (!CanShootNow(core)) continue;

            // 레이 체크 (EnemyCore에서 제공하는 방식 그대로)
            if (CanSeePlayer(core))
            {
                ShootFrom(core);
                lastShootTime[core] = Time.time;
            }
        }
    }

    private bool CanShootNow(EnemyCore core)
    {
        if (!lastShootTime.ContainsKey(core))
            lastShootTime[core] = 0f;
        return Time.time - lastShootTime[core] >= core.AttackDelay;
    }

    private bool CanSeePlayer(EnemyCore core)
    {
        Vector2 origin = core.FirePoint.position;
        Vector2 dir = (core.Target.position - (Vector3)origin).normalized;
        float dist = Vector2.Distance(origin, core.Target.position);

        if (dist > core.AttackRange) return false;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, core.AttackRange);
        Debug.DrawRay(origin, dir * core.AttackRange,
                     hit && hit.transform == core.Target ? Color.green : Color.red);

        return hit && hit.transform == core.Target;
    }

    private void ShootFrom(EnemyCore core)
    {
        if (bulletPool.Count == 0) return;

        GameObject bullet = bulletPool.Dequeue();

        // 각 적의 자식 Gun 아래 Bullet 템플릿에서 외형 복사 (선택사항)
        Transform gun = core.transform.Find("Gun");
        Transform templateBullet = gun?.Find("Bullet");
        if (templateBullet != null)
        {
            // 외형만 복사 (스프라이트 등)
            var sr = bullet.GetComponent<SpriteRenderer>();
            if (sr) sr.sprite = templateBullet.GetComponent<SpriteRenderer>().sprite;
            // 필요하면 색상, 크기 등도 복사
        }

        Vector3 dir = (core.Target.position - core.FirePoint.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        bullet.transform.position = core.FirePoint.position;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
        bullet.SetActive(true);
    }

    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        bulletPool.Enqueue(bullet);
    }
}