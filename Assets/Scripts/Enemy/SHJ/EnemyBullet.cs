using UnityEngine;
using System.Collections;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float speed = 5f;           // 총알 이동 속도 (인스펙터에서 조절 가능)
    [SerializeField] private float lifeTime = 3f;         // 화면에 존재할 수 있는 최대 시간 (초 단위)
    [SerializeField] private float damage = 20f;          // ★★★ 새로 추가: 이 총알이 플레이어에게 주는 데미지 양
                                                          // 인스펙터에서 몬스터별로 다르게 설정 가능 (예: 강한 몬스터는 50)

    private bool isReturned = false;                      // 이미 풀로 반환됐는지 체크 (중복 반환 방지)
    private Coroutine lifeCoroutine;                      // lifeTime 코루틴 참조 (중간에 멈추기 위해)

    // ★ 총알이 풀에서 꺼내져서 활성화될 때 호출됨 (EnemyShooter에서 gameObject.SetActive(true) 할 때)
    private void OnEnable()
    {
        isReturned = false;                               // 반환 상태 초기화

        // lifeTime 초 후에 자동으로 풀로 돌아가게 코루틴 시작
        // → 화면 밖으로 안 나가고 플레이어도 안 맞아도 강제 소멸
        lifeCoroutine = StartCoroutine(AutoReturnAfterSeconds());
    }

    // 매 프레임 호출: 총알을 앞으로 날아가게 함
    // EnemyShooter에서 rotation을 타겟 방향으로 맞췄기 때문에, local right 방향이 타겟 방향임
    private void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    // ★★★ 가장 중요한 변화 부분: 2D 트리거 충돌 감지
    // 총알이 어떤 콜라이더와 처음 닿았을 때 한 번만 호출됨
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 반환된 상태면 무시 (안전장치)
        if (isReturned) return;

        // 충돌한 오브젝트의 태그가 "Player"인지 확인
        if (other.CompareTag("Player"))
        {
            // ★★★ 여기서 데미지가 실제로 적용되는 부분!
            // 플레이어 오브젝트에서 PlayerStats 컴포넌트 가져오기
            PlayerStats playerStats = other.GetComponent<PlayerStats>();

            // 컴포넌트가 있으면 데미지 주기
            if (playerStats != null)
            {
                playerStats.TakeDamage(damage);  // ← 플레이어의 TakeDamage(float damage) 메서드 호출
                                                 // 플레이어 체력 깎이고, HUD 업데이트까지 자동으로 됨
            }

            // 데미지 준 후 (또는 관통 안 하게 하려면) 즉시 총알을 풀로 반환
            // → 총알이 플레이어 맞고 바로 사라짐 (한 번만 데미지)
            ReturnToPool();
        }
    }


    // 화면 밖으로 날아간 총알 정리용
    private void OnBecameInvisible()
    {
        // 아직 활성화 상태이고 반환 안 됐으면 풀로 돌려보냄
        if (gameObject.activeInHierarchy && !isReturned)
        {
            ReturnToPool();
        }
    }

    // lifeTime 초 후에 자동으로 총알을 풀로 돌려보내는 코루틴
    private IEnumerator AutoReturnAfterSeconds()
    {
        yield return new WaitForSeconds(lifeTime);

        // 지정된 시간이 지나면 강제로 반환
        if (!isReturned)
            ReturnToPool();
    }

    // 실제로 오브젝트 풀에 총알 돌려주는 메서드
    private void ReturnToPool()
    {
        // 중복 반환 방지
        if (isReturned) return;
        isReturned = true;

        // 자동 반환 코루틴이 돌고 있으면 강제 중지
        if (lifeCoroutine != null)
            StopCoroutine(lifeCoroutine);

        // 총알 비활성화 (화면에서 사라짐)
        gameObject.SetActive(false);

        // ObjectPoolManager에게 이 총알 돌려준다고 알림
        ObjectPoolManager.Instance.ReturnEnemyBullet(this);
    }
}