using System.Collections;
using UnityEngine;

public class FireGround : MonoBehaviour
{
    [Header("화염 지대 설정")]
    [SerializeField] private float defaultRadius = 2f;
    [SerializeField] private float damageInterval = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    private float damage;
    private float duration;
    private float radius;
    private float timer;
    private bool isActive = false;

    // 런타임 컴포넌트
    private CircleCollider2D col;
    private SpriteRenderer sr;

    private void Awake()
    {
        col = GetComponent<CircleCollider2D>();
        sr = GetComponent<SpriteRenderer>();

        // 초기 설정
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    // 초기화
    public void Init(float damage, float duration, float radiusMultiplier = 1f)
    {
        this.damage = damage;
        this.duration = duration;
        this.radius = defaultRadius * radiusMultiplier;
        this.timer = 0f;
        this.isActive = true;

        // 콜라이더 반경 설정
        if (col != null)
        {
            col.radius = this.radius;
        }

        // 시각적 효과 설정
        if (sr != null)
        {
            sr.transform.localScale = Vector3.one * (this.radius * 2f);
        }

        // 데미지 코루틴 시작
        StartCoroutine(DamageCoroutine());
    }

    private IEnumerator DamageCoroutine()
    {
        while (isActive && timer < duration)
        {
            // 현재 범위 내의 모든 적에게 데미지
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);

            foreach (var enemyCol in hitEnemies)
            {
                Enemy enemy = enemyCol.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
            }

            // 데미지 간격만큼 대기
            yield return new WaitForSeconds(damageInterval);
        }

        // 지속 시간 종료 후 비활성화
        Deactivate();
    }

    private void Update()
    {
        if (isActive)
        {
            timer += Time.deltaTime;
            if (timer >= duration)
            {
                Deactivate();
            }
        }
    }

    // 비활성화
    private void Deactivate()
    {
        isActive = false;

        
        // 짧은 딜레이 후 풀로 반환
        Invoke(nameof(ReturnToPool), 1f);
    }

    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnFireGround(this);
        }
    }

    // 디버그용 Gizmo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}