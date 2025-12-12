using System.Collections;
using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        // 근거리 몬스터면 작동 안 함
        if (!enemy.IsRange)
        {
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (enemy.Target == null) return;

        // 범위 체크: enemy.point 기준으로 거리 계산
        float distance = Vector2.Distance(enemy.point.position, enemy.Target.position);

        if (distance <= enemy.AttackRange)
        {
            StopAllCoroutines();
            StartCoroutine(ShootRoutine());
        }
    }

    private IEnumerator ShootRoutine()
    {
        while (true)
        {
            if (!enemy.IsRange)
            {
                yield break;
            }

            // 풀에서 EnemyBullet 가져와서 발사
            EnemyBullet bullet = ObjectPoolManager.Instance.GetEnemyBullet();

            if (bullet != null)
            {
                bullet.transform.position = enemy.point.position;

                // ★ 타겟 방향으로 회전시키기
                Vector2 dir = (enemy.Target.position - enemy.point.position).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

                bullet.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(enemy.AttackDelay);

            // 딜레이 후에도 enemy.point 기준으로 범위 체크
            if (enemy.Target == null ||
                !enemy.IsRange ||
                Vector2.Distance(enemy.point.position, enemy.Target.position) > enemy.AttackRange)
            {
                yield break;
            }
        }
    }
}