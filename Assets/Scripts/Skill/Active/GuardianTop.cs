using UnityEngine;
using System.Collections.Generic;

public class GuardianTop : MonoBehaviour
{
    [Header("톱날 설정")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask projectileLayer;
    [SerializeField] private LayerMask groundLayer;

    // 회전 관련
    private float currentAngle;
    private float orbitRadius;
    private float rotationSpeed;
    private Transform centerTransform;
    private float damageMultiplier = 1f;

    // 상태
    private bool isActive = false;
    private bool isInitialized = false;

    // 효과 관련
    private TrailRenderer trail;
    private ParticleSystem spinEffect;
    private Collider2D topCollider;
    private Rigidbody2D rb;

    // 상수
    private const float COLLISION_COOLDOWN = 0.1f;
    private float lastCollisionTime = 0f;

    // 대체 충돌 감지용
    private HashSet<Collider2D> alreadyCollidedWith = new HashSet<Collider2D>();

    // 프로퍼티
    public float CurrentAngle => currentAngle;
    public bool IsActive => isActive;

    private void Awake()
    {
        // 컴포넌트 초기화
        trail = GetComponent<TrailRenderer>();
        spinEffect = GetComponent<ParticleSystem>();
        topCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        // Rigidbody 설정 (Dynamic 모드로 변경 - 충돌을 위해)
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.gravityScale = 0f; // 중력 비활성화
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 회전은 직접 제어
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 연속 충돌 감지
        }
        else
        {
            Debug.LogError("[GuardianTop] Rigidbody2D 컴포넌트를 찾을 수 없습니다!");
        }

        // 초기 상태 비활성화
        if (trail != null) trail.enabled = false;
        if (spinEffect != null) spinEffect.Stop();
        if (topCollider != null) topCollider.enabled = false;

        gameObject.SetActive(false);
    }

    // 톱날 초기화
    public void Initialize(float initialAngle, float radius, float speed, Transform center, float dmgMultiplier = 1f)
    {
        currentAngle = initialAngle;
        orbitRadius = radius;
        rotationSpeed = speed;
        centerTransform = center;
        damageMultiplier = dmgMultiplier;
        isInitialized = true;
        isActive = true;
        lastCollisionTime = 0f;

        // 활성화
        gameObject.SetActive(true);

        // 효과 활성화
        if (trail != null)
        {
            trail.enabled = true;
            trail.Clear();
        }

        if (spinEffect != null)
        {
            spinEffect.Play();
        }

        if (topCollider != null)
        {
            topCollider.enabled = true;
        }

        // 초기 위치 설정
        UpdateOrbitPosition();
    }

    private void Update()
    {
        if (!isActive || !isInitialized || centerTransform == null) return;

        // 궤도 위치 업데이트
        UpdateOrbitPosition();

        // 충돌 쿨다운 감소
        if (lastCollisionTime > 0)
        {
            lastCollisionTime -= Time.deltaTime;
        }

        // 대체 충돌 감지 (필요 시 사용)
        if (isActive && topCollider != null && topCollider.enabled)
        {
            // Physics2D 기반 충돌 감지 테스트
            CheckOverlapCollisions();
        }
    }

    // 원형 궤도 위치 업데이트
    private void UpdateOrbitPosition()
    {
        // 각도 업데이트
        currentAngle += rotationSpeed * Time.deltaTime;
        currentAngle = currentAngle % 360f; // 0-360도로 정규화

        // 원형 궤도 위치 계산
        float radians = currentAngle * Mathf.Deg2Rad;
        Vector3 orbitPosition = new Vector3(
            Mathf.Cos(radians) * orbitRadius,
            Mathf.Sin(radians) * orbitRadius,
            0
        );

        // 플레이어 위치 기준으로 최종 위치 설정
        transform.position = centerTransform.position + orbitPosition;

        // 항상 플레이어를 바라보도록 설정 (톱날 모양 유지)
        Vector3 directionToCenter = (centerTransform.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg + 90f;
        transform.rotation = Quaternion.Euler(0, 0, targetAngle);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (lastCollisionTime > 0)
        {
            return; // 충돌 쿨다운 체크
        }

        // 적 처리
        if (IsInLayer(collision.gameObject, enemyLayer))
        {
            HandleEnemyCollision(collision);
        }
        // 적 투사체 차단
        else if (IsInLayer(collision.gameObject, projectileLayer))
        {
            HandleProjectileCollision(collision);
        }
        // 지형 충돌
        else if (IsInLayer(collision.gameObject, groundLayer))
        {
            // 지형에는 반응하지 않고 통과
            Physics2D.IgnoreCollision(collision.collider, topCollider);
        }

        lastCollisionTime = COLLISION_COOLDOWN;
    }

    // Trigger 기반 충돌 감지 (콜라이더가 Trigger인 경우)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 중복 충돌 체크
        if (alreadyCollidedWith.Contains(other))
        {
            return;
        }

        // 적 처리
        if (IsInLayer(other.gameObject, enemyLayer))
        {
            HandleEnemyTriggerCollision(other);
        }
        // 적 투사체 차단
        else if (IsInLayer(other.gameObject, projectileLayer))
        {
            HandleProjectileTriggerCollision(other);
        }

        // 충돌 목록에 추가
        alreadyCollidedWith.Add(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 충돌 목록에서 제거
        if (alreadyCollidedWith.Contains(other))
        {
            alreadyCollidedWith.Remove(other);
        }
    }

    // 적 충돌 처리
    private void HandleEnemyCollision(Collision2D collision)
    {
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy == null)
        {
            return;
        }

        // 데미지 계산
        float finalDamage = damage * damageMultiplier;

        // 데미지 적용
        enemy.TakeDamage(finalDamage);

        // 넉백 적용
        Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
        if (enemyRb != null)
        {
            Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
            enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }

        // 히트 이펙트
        CreateHitEffect(collision.contacts[0].point);
    }

    // Trigger 기반 적 충돌 처리
    private void HandleEnemyTriggerCollision(Collider2D other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null)
        {
            return;
        }

        // 데미지 계산
        float finalDamage = damage * damageMultiplier;

        // 데미지 적용
        enemy.TakeDamage(finalDamage);

        // 넉백 적용
        Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
        if (enemyRb != null)
        {
            Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
            enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }

        // 히트 이펙트
        CreateHitEffect(other.transform.position);
    }

    // Trigger 기반 투사체 충돌 처리
    private void HandleProjectileTriggerCollision(Collider2D other)
    {
        // 적 투사체 파괴
        EnemyBullet bullet = other.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            Destroy(other.gameObject);
            CreateBlockEffect(other.transform.position);
        }
    }

    // 투사체 충돌 처리
    private void HandleProjectileCollision(Collision2D collision)
    {
        // 적 투사체 파괴
        EnemyBullet bullet = collision.gameObject.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            Destroy(collision.gameObject);
            CreateBlockEffect(collision.contacts[0].point);
        }
    }

    // 히트 이펙트 생성
    private void CreateHitEffect(Vector3 position)
    {
        if (Resources.Load("Effects/HitEffect") != null)
        {
            GameObject effect = Instantiate(Resources.Load("Effects/HitEffect") as GameObject);
            effect.transform.position = position;
            Destroy(effect, 1f);
        }
    }

    // 차단 이펙트 생성
    private void CreateBlockEffect(Vector3 position)
    {
        if (Resources.Load("Effects/BlockEffect") != null)
        {
            GameObject effect = Instantiate(Resources.Load("Effects/BlockEffect") as GameObject);
            effect.transform.position = position;
            Destroy(effect, 1f);
        }
    }

    // 레이어 체크 헬퍼 메서드
    private bool IsInLayer(GameObject obj, LayerMask layerMask)
    {
        int objLayer = obj.layer;
        return (layerMask.value & (1 << objLayer)) != 0;
    }

    // 비활성화
    public void Deactivate()
    {
        isActive = false;

        // 효과 비활성화
        if (trail != null) trail.enabled = false;
        if (spinEffect != null) spinEffect.Stop();
        if (topCollider != null) topCollider.enabled = false;

        gameObject.SetActive(false);
    }

    // 재활성화
    public void Reactivate()
    {
        if (!isInitialized) return;

        isActive = true;
        lastCollisionTime = 0f;

        gameObject.SetActive(true);

        // 효과 재활성화
        if (trail != null)
        {
            trail.enabled = true;
            trail.Clear();
        }

        if (spinEffect != null)
        {
            spinEffect.Play();
        }

        if (topCollider != null)
        {
            topCollider.enabled = true;
        }
    }

    // 스탯 업데이트
    public void UpdateStats(float newDamage, float newKnockback, float newSpeed)
    {
        damage = newDamage;
        knockbackForce = newKnockback;
        rotationSpeed = newSpeed;
    }

    // 데미지 배수 설정
    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = multiplier;
    }

    // 위치 초기화 (재소환 시 사용)
    public void ResetPosition()
    {
        if (isInitialized)
        {
            UpdateOrbitPosition();
        }
    }

    // 오버랩 기반 충돌 체크 (대체 방법)
    private void CheckOverlapCollisions()
    {
        // 주기적인 체크를 위한 시간 간격 (0.1초)
        if (Time.time % 0.1f > Time.deltaTime) return;

        // Collider2D 타입에 따라 다른 체크 방식 사용
        Collider2D[] hits = null;

        if (topCollider is CircleCollider2D circleCollider)
        {
            hits = Physics2D.OverlapCircleAll(transform.position, circleCollider.radius, enemyLayer);
        }
        else if (topCollider is BoxCollider2D boxCollider)
        {
            hits = Physics2D.OverlapBoxAll(transform.position, boxCollider.size, transform.rotation.eulerAngles.z, enemyLayer);
        }

        if (hits != null && hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                // 이미 충돌 처리한 객체는 건너뛰기
                if (alreadyCollidedWith.Contains(hit))
                {
                    continue;
                }

                HandleEnemyTriggerCollision(hit);
                alreadyCollidedWith.Add(hit);
            }
        }
    }

    // 초기화 상태 리셋 (풀링 재사용 시)
    public void ResetForReuse()
    {
        alreadyCollidedWith.Clear();
        isInitialized = false;
        isActive = false;
        currentAngle = 0f;
        orbitRadius = 0f;
        rotationSpeed = 0f;
        centerTransform = null;
        damageMultiplier = 1f;
        lastCollisionTime = 0f;

        // 모든 효과 정지
        if (trail != null)
        {
            trail.enabled = false;
            trail.Clear();
        }

        if (spinEffect != null)
        {
            spinEffect.Stop();
            spinEffect.Clear();
        }

        if (topCollider != null)
        {
            topCollider.enabled = false;
        }

        gameObject.SetActive(false);
    }
}