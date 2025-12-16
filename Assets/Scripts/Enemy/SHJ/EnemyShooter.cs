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

            Debug.Log($"[EnemyShooter] 타겟 발견: {enemy.Target.name}");

            // 거리 계산
            float distance = Vector2.Distance(enemy.point.position, enemy.Target.position);
            Debug.Log($"[EnemyShooter] 현재 거리: {distance:F2} / 공격 범위: {enemy.Range}");  // ← Range → AttackRange

            // 범위 안에 없으면 계속 기다림
            if (distance > enemy.Range)  // ← Range → AttackRange
            {
                enemy.FindPlayer();
                yield return null;
                continue;
            }

            Debug.Log("[EnemyShooter] 공격 범위 안에 들어옴! AttackDelay 기다리는 중...");

            // 여기까지 왔으면 = 타겟 있고, 범위 안에 있음
            // AttackDelay(10초) 기다림 → 첫 발도 딜레이 적용
            yield return new WaitForSeconds(enemy.AttackDelay);

            Debug.Log("[EnemyShooter] AttackDelay 끝! 최종 확인 시작");

            // 딜레이 끝난 후 다시 한 번 최종 확인
            // (플레이어가 중간에 범위 밖으로 나갔거나 죽었을 수 있으니까)
            if (enemy.Target == null ||
                !enemy.IsRange ||
                Vector2.Distance(enemy.point.position, enemy.Target.position) > enemy.Range)  // ← Range → AttackRange
            {
                Debug.Log("[EnemyShooter] 최종 확인 실패 → 이번 발사 스킵");
                continue; // 조건 안 맞으면 이번 발사는 스킵하고 루프 처음으로
            }

            Debug.Log("[EnemyShooter] 모든 조건 만족! 총알 발사!!");

            Shoot();

            // 발사 후 바로 다음 발사 준비 (연사 위해)
            enemy.FindPlayer(); // 발사 후 재탐색
            Debug.Log("[EnemyShooter] 발사 후 재탐색 완료 → 다음 발사 준비");
            continue; // 바로 루프 처음으로
        }
    }
    private void Shoot()
    {
        Debug.Log("[EnemyShooter] Shoot() 호출 → 총알 풀에서 꺼내기 시도");

        EnemyBullet bullet = ObjectPoolManager.Instance.GetEnemyBullet();  // 나 꺼내게
        if (bullet != null)
        {
            // 위치 설정
            bullet.transform.position = enemy.point.position;  // 나 여기서 총발사할게

            // 방향 설정
            Vector2 dir = (enemy.Target.position - enemy.point.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);  // 나 거기 회전한다

            // 활성화 → EnemyBullet.OnEnable() 실행 → 스스로 날아가고 관리됨
            bullet.gameObject.SetActive(true);  // ★ 여기서만 활성화 → OnEnable() 호출됨 (EnemyBullet이 알아서 코루틴 시작)

            Debug.Log("[EnemyShooter] 총알 발사 성공! (EnemyBullet이 알아서 처리)");
        }
        else
        {
            Debug.LogWarning("[EnemyShooter] 총알 풀 비었음! (풀 크기 늘려주세요)");
        }
    }
    public void TriggerAttack()
    {
        Debug.Log("[EnemyShooter] Move에서 공격 신호 받음 → 코루틴 재시작");
        StopAllCoroutines();
        StartCoroutine(ShootRoutine());
    }
    private void OnDisable()
    {
        StopAllCoroutines(); // 몬스터 비활성화/파괴될 때 정리
    }
}