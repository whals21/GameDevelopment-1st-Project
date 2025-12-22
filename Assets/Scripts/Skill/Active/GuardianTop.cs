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
            Debug.Log($"[GuardianTop] Rigidbody2D 설정: Dynamic, GravityScale=0, CollisionDetection=Continuous");
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
            Debug.Log($"[GuardianTop] 콜라이더 활성화: {topCollider.GetType().Name}, IsTrigger: {topCollider.isTrigger}");
        }

        // 초기 위치 설정
        UpdateOrbitPosition();

        // 초기화 정보 로그
        Debug.Log($"[GuardianTop] 초기화 완료 - 데미지: {damage}, 배수: {damageMultiplier}, 콜라이더: {(topCollider != null ? topCollider.GetType().Name : "None")}");
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
        Debug.Log($"[GuardianTop] OnCollisionEnter2D 호출됨! 충돌한 객체: {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");

        if (lastCollisionTime > 0)
        {
            Debug.Log("[GuardianTop] 충돌 쿨다운으로 무시됨");
            return; // 충돌 쿨다운 체크
        }

        // 레이어 마스크 정보 로그
        Debug.Log($"[GuardianTop] LayerMask 정보 - EnemyLayer: {enemyLayer.value}, ProjectileLayer: {projectileLayer.value}, GroundLayer: {groundLayer.value}");

        // 적 처리
        if (IsInLayer(collision.gameObject, enemyLayer))
        {
            Debug.Log("[GuardianTop] 적 레이어 감지됨!");
            HandleEnemyCollision(collision);
        }
        // 적 투사체 차단
        else if (IsInLayer(collision.gameObject, projectileLayer))
        {
            Debug.Log("[GuardianTop] 투사체 레이어 감지됨!");
            HandleProjectileCollision(collision);
        }
        // 지형 충돌
        else if (IsInLayer(collision.gameObject, groundLayer))
        {
            Debug.Log("[GuardianTop] 지형 레이어 감지됨 - 충돌 무시");
            // 지형에는 반응하지 않고 통과
            Physics2D.IgnoreCollision(collision.collider, topCollider);
        }
        else
        {
            Debug.Log($"[GuardianTop] 알 수 없는 레이어: {collision.gameObject.layer} - 처리되지 않음");
        }

        lastCollisionTime = COLLISION_COOLDOWN;
    }

    // Trigger 기반 충돌 감지 (콜라이더가 Trigger인 경우)
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[GuardianTop] OnTriggerEnter2D 호출됨! 객체: {other.gameObject.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");

        // 중복 충돌 체크
        if (alreadyCollidedWith.Contains(other))
        {
            Debug.Log("[GuardianTop] 이미 충돌했던 객체 - 무시");
            return;
        }

        // 적 처리
        if (IsInLayer(other.gameObject, enemyLayer))
        {
            Debug.Log("[GuardianTop] Trigger로 적 레이어 감지됨!");
            HandleEnemyTriggerCollision(other);
        }
        // 적 투사체 차단
        else if (IsInLayer(other.gameObject, projectileLayer))
        {
            Debug.Log("[GuardianTop] Trigger로 투사체 레이어 감지됨!");
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
        Debug.Log($"[GuardianTop] HandleEnemyCollision 호출 - 충돌 객체: {collision.gameObject.name}");

        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError($"[GuardianTop] Enemy 컴포넌트를 찾을 수 없습니다: {collision.gameObject.name}");
            return;
        }

        // 데미지 계산
        float finalDamage = damage * damageMultiplier;
        Debug.Log($"[GuardianTop] 데미지 적용 시도 - 기본 데미지: {damage}, 배수: {damageMultiplier}, 최종 데미지: {finalDamage}");
        Debug.Log($"[GuardianTop] 적 현재 HP: {enemy.CurrentHP}");

        // 데미지 적용
        enemy.TakeDamage(finalDamage);
        Debug.Log($"[GuardianTop] 데미지 적용 후 적 HP: {enemy.CurrentHP}");

        // 넉백 적용
        Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
        if (enemyRb != null)
        {
            Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
            enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
            Debug.Log($"[GuardianTop] 넉백 적용 - 방향: {knockbackDirection}, 힘: {knockbackForce}");
        }

        // 히트 이펙트
        CreateHitEffect(collision.contacts[0].point);
    }

    // Trigger 기반 적 충돌 처리
    private void HandleEnemyTriggerCollision(Collider2D other)
    {
        Debug.Log($"[GuardianTop] HandleEnemyTriggerCollision 호출 - 객체: {other.gameObject.name}");

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError($"[GuardianTop] Enemy 컴포넌트를 찾을 수 없습니다: {other.gameObject.name}");
            return;
        }

        // 데미지 계산
        float finalDamage = damage * damageMultiplier;
        Debug.Log($"[GuardianTop] Trigger 데미지 적용 - 기본 데미지: {damage}, 배수: {damageMultiplier}, 최종 데미지: {finalDamage}");
        Debug.Log($"[GuardianTop] 적 현재 HP: {enemy.CurrentHP}");

        // 데미지 적용
        enemy.TakeDamage(finalDamage);
        Debug.Log($"[GuardianTop] Trigger 데미지 적용 후 적 HP: {enemy.CurrentHP}");

        // 넉백 적용
        Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
        if (enemyRb != null)
        {
            Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
            enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
            Debug.Log($"[GuardianTop] Trigger 넉백 적용 - 방향: {knockbackDirection}, 힘: {knockbackForce}");
        }

        // 히트 이펙트
        CreateHitEffect(other.transform.position);
    }

    // Trigger 기반 투사체 충돌 처리
    private void HandleProjectileTriggerCollision(Collider2D other)
    {
        Debug.Log($"[GuardianTop] HandleProjectileTriggerCollision 호출 - 객체: {other.gameObject.name}");

        // 적 투사체 파괴
        EnemyBullet bullet = other.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            Destroy(other.gameObject);
            CreateBlockEffect(other.transform.position);
            Debug.Log("[GuardianTop] 투사체 파괴 완료");
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
        string layerName = LayerMask.LayerToName(objLayer);
        bool isInLayer = (layerMask.value & (1 << objLayer)) != 0;
        Debug.Log($"[GuardianTop] IsInLayer 체크 - 객체: {obj.name}, 레이어: {objLayer}({layerName}), LayerMask: {layerMask.value}, 결과: {isInLayer}");
        return isInLayer;
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

                Debug.Log($"[GuardianTop] 오버랩으로 적 감지: {hit.gameObject.name}");
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