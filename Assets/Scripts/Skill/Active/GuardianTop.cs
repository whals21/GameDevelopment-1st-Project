using UnityEngine;

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

        // Rigidbody 설정 (Kinematic 모드)
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
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
        if (lastCollisionTime > 0) return; // 충돌 쿨다운 체크

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

    // 적 충돌 처리
    private void HandleEnemyCollision(Collision2D collision)
    {
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy == null) return;

        // 데미지 적용
        float finalDamage = damage * damageMultiplier;
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
        return (layerMask.value & (1 << obj.layer)) != 0;
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

    // 초기화 상태 리셋 (풀링 재사용 시)
    public void ResetForReuse()
    {
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