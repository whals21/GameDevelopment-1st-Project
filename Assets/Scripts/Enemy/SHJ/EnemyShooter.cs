using System.Collections;
using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        if (!enemy.IsRange)
        {
            enabled = false; // 근거리 몬스터면 이 스크립트 자체 비활성화
            return;
        }
    }

    private void Start()
    {
        // 게임 시작하자마자 (또는 몬스터 활성화되자마자) 반복 슈팅 코루틴 시작
        StartCoroutine(ShootRoutine());
    }

    // 모든 로직이 여기 안에 있음. Update() 필요 없음!
    private IEnumerator ShootRoutine()
    {
        while (true)
        {
            // 1. 타겟 있는지 확인 (없으면 계속 기다림)
            if (enemy.Target == null)
            {
                yield return null; // 다음 프레임까지 기다림
                continue;
            }

            // 2. 거리 계산
            float distance = Vector2.Distance(enemy.point.position, enemy.Target.position);

            // 3. 범위 안에 없으면 계속 기다림
            if (distance > enemy.AttackRange)
            {
                yield return null;
                continue;
            }

            // 여기까지 왔으면 = 타겟 있고, 범위 안에 있음

            // 4. AttackDelay(10초) 기다림 → 첫 발도 딜레이 적용
            yield return new WaitForSeconds(enemy.AttackDelay);

            // 5. 딜레이 끝난 후 다시 한 번 최종 확인
            // (플레이어가 중간에 범위 밖으로 나갔거나 죽었을 수 있으니까)
            if (enemy.Target == null ||
                !enemy.IsRange ||
                Vector2.Distance(enemy.point.position, enemy.Target.position) > enemy.AttackRange)
            {
                continue; // 조건 안 맞으면 이번 발사는 스킵하고 루프 처음으로
            }

            // 6. 총알 발사
            EnemyBullet bullet = ObjectPoolManager.Instance.GetEnemyBullet();
            if (bullet != null)
            {
                bullet.transform.position = enemy.point.position;

                Vector2 dir = (enemy.Target.position - enemy.point.position).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

                bullet.gameObject.SetActive(true);
            }

            // 루프 계속 → 다시 타겟/범위 체크부터 시작
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines(); // 몬스터 비활성화/파괴될 때 정리
    }
}