using UnityEngine;
using System.Collections;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float speed = 12f;     // 이동 속도
    [SerializeField] private float lifeTime = 3f;   // ★ 몇 초 후 자동 반환 (인스펙터에서 설정)

    private bool isReturned = false;
    private Coroutine lifeCoroutine;

    private void OnEnable()
    {
        isReturned = false;

        // ★ 자동 반환 타이머 시작
        lifeCoroutine = StartCoroutine(AutoReturnAfterSeconds());
    }

    private void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isReturned && other.CompareTag("Player"))
        {
            // Player와 부딪히면 즉시 사라짐
            ReturnToPool();
        }
    }

    private void OnBecameInvisible()
    {
        if (gameObject.activeInHierarchy && !isReturned)
        {
            ReturnToPool();
        }
    }

    // ★ 지정된 시간 후 자동으로 사라지는 코루틴
    private IEnumerator AutoReturnAfterSeconds()
    {
        yield return new WaitForSeconds(lifeTime);

        if (!isReturned)
            ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (isReturned) return;

        isReturned = true;

        // 자동타이머 중지
        if (lifeCoroutine != null)
            StopCoroutine(lifeCoroutine);

        gameObject.SetActive(false);

        ObjectPoolManager.Instance.ReturnEnemyBullet(this);
    }
}
