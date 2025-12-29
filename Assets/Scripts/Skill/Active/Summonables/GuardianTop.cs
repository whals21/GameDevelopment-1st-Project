using UnityEngine;

/// <summary>
/// v2 GuardianTop - 리팩토링 버전
///
/// 전문가 피드백 반영:
/// 1. Kinematic 모드 (스크립트 제어와 물리 엔진 충돌 방지)
/// 2. 간소화된 충돌 감지 (OnTriggerEnter2D 하나만 사용)
/// 3. Resources.Load 제거 (풀링 시스템 사용 권장)
/// 4. 데이터 주도 설계 (Initialize 메서드로 주입)
/// 5. 불안정한 타이머 로직 제거
///
/// 본질: "회전하며 적에게 데미지 + 넉백"
/// </summary>
public class GuardianTop : MonoBehaviour
{
    #region Private Fields
    // 상태 (데이터는 외부에서 주입)
    private float _damage;
    private float _knockbackForce;
    private float _orbitRadius;
    private float _rotationSpeed;
    private Transform _centerTransform;
    private int _enemyLayerMask;

    // 궤도 상태
    private float _currentAngle;
    private bool _isActive;

    // 컴포넌트
    private Rigidbody2D _rb;
    private Collider2D _collider;
    private TrailRenderer _trail;
    private ParticleSystem _spinEffect;
    #endregion

    #region Properties
    public float CurrentAngle => _currentAngle;
    public bool IsActive => _isActive;
    #endregion

    #region Initialization
    private void Awake()
    {
        // 컴포넌트 캐싱
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _trail = GetComponent<TrailRenderer>();
        _spinEffect = GetComponent<ParticleSystem>();

        // 1. Kinematic 모드로 설정 (스크립트 제어 + 물리와 충돌 방지)
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 부드러운 이동
        }

        // Collider는 Trigger로 설정 (물리적 튕김 없이 통과하며 타격)
        if (_collider != null)
        {
            _collider.isTrigger = true;
        }

        // 초기 상태 비활성화
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 비활성화 시 자동 리셋 (캡슐화 - 전문가 피드백)
    /// ObjectPool.Return()에서 SetActive(false) 호출 시 자동으로 실행됨
    /// </summary>
    private void OnDisable()
    {
        ResetForReuse();
    }

    /// <summary>
    /// GuardianTop 초기화 (데이터 주도 설계)
    /// </summary>
    public void Initialize(float damage, float knockbackForce, float radius, float speed, Transform center, int enemyLayerMask)
    {
        _damage = damage;
        _knockbackForce = knockbackForce;
        _orbitRadius = radius;
        _rotationSpeed = speed;
        _centerTransform = center;
        _enemyLayerMask = enemyLayerMask;
        _currentAngle = 0f;
        _isActive = true;

        // 활성화
        gameObject.SetActive(true);

        // 시각 효과 활성화
        if (_trail != null)
        {
            _trail.enabled = true;
            _trail.Clear();
        }

        if (_spinEffect != null)
        {
            _spinEffect.Play();
        }

        if (_collider != null)
        {
            _collider.enabled = true;
        }

        // 초기 위치 설정
        UpdateOrbitPosition();
    }
    #endregion

    #region Core Loop
    private void Update()
    {
        if (!_isActive || _centerTransform == null) return;

        // 궤도 업데이트
        UpdateOrbitPosition();
    }

    /// <summary>
    /// 원형 궤도 위치 업데이트
    /// </summary>
    private void UpdateOrbitPosition()
    {
        // 각도 업데이트
        _currentAngle += _rotationSpeed * Time.deltaTime;
        _currentAngle %= 360f;

        // 원형 궤도 위치 계산
        float radians = _currentAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(radians) * _orbitRadius,
            Mathf.Sin(radians) * _orbitRadius,
            0
        );

        // 플레이어 위치 기준으로 최종 위치 설정
        transform.position = _centerTransform.position + orbitOffset;

        // 항상 플레이어를 바라보도록 설정 (톱날 모양 유지)
        Vector3 directionToCenter = (_centerTransform.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(directionToCenter.y, directionToCenter.x) * Mathf.Rad2Deg + 90f;
        transform.rotation = Quaternion.Euler(0, 0, targetAngle);
    }
    #endregion

    #region Collision (간소화)
    /// <summary>
    /// 2. 단순화된 충돌 감지 (Trigger 하나만 사용)
    /// 적 내부 쿨타임(I-Frame)에 의존하여 중복 타격 방지
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 레이어 체크
        if ((_enemyLayerMask & (1 << other.gameObject.layer)) == 0) return;

        if (other.TryGetComponent<Enemy>(out var enemy))
        {
            if (enemy.CurrentHP <= 0) return;

            // 데미지 (적 내부 쿨타임에 의존)
            enemy.TakeDamage(_damage);

            // 넉백
            if (enemy.TryGetComponent<Rigidbody2D>(out var enemyRb))
            {
                Vector2 knockbackDir = (enemy.transform.position - transform.position).normalized;
                enemyRb.AddForce(knockbackDir * _knockbackForce, ForceMode2D.Impulse);
            }

            // 3. 히트 이펙트 (풀링 사용 권장)
            SpawnHitEffect(transform.position);
        }
    }

    /// <summary>
    /// 히트 이펙트 생성
    /// TODO: ObjectPoolManager에 HitEffect 풀 추가 후 사용
    /// </summary>
    private void SpawnHitEffect(Vector3 position)
    {
        // 현재는 풀링 미구현 상태 - 필요시 ObjectPoolManager 연동
        // 예: ObjectPoolManager.Instance.GetHitEffect();

        // 임시: 파티클 시스템이 있다면 재생
        if (_spinEffect != null)
        {
            // 톱날 자체의 이펙트로 충분하므로 별도 이펙트는 생략 가능
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 비활성화
    /// </summary>
    public void Deactivate()
    {
        _isActive = false;

        if (_trail != null) _trail.enabled = false;
        if (_spinEffect != null) _spinEffect.Stop();
        if (_collider != null) _collider.enabled = false;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 재활성화
    /// </summary>
    public void Reactivate()
    {
        _isActive = true;
        gameObject.SetActive(true);

        if (_trail != null)
        {
            _trail.enabled = true;
            _trail.Clear();
        }

        if (_spinEffect != null)
        {
            _spinEffect.Play();
        }

        if (_collider != null)
        {
            _collider.enabled = true;
        }
    }

    /// <summary>
    /// 스탯 업데이트
    /// </summary>
    public void UpdateStats(float damage, float knockbackForce, float speed)
    {
        _damage = damage;
        _knockbackForce = knockbackForce;
        _rotationSpeed = speed;
    }

    /// <summary>
    /// 풀링 재사용을 위한 리셋
    /// OnDisable에서 자동 호출되므로 SetActive(false) 제거 (중복 방지)
    /// </summary>
    public void ResetForReuse()
    {
        _isActive = false;
        _currentAngle = 0f;
        _orbitRadius = 0f;
        _rotationSpeed = 0f;
        _centerTransform = null;

        if (_trail != null)
        {
            _trail.enabled = false;
            _trail.Clear();
        }

        if (_spinEffect != null)
        {
            _spinEffect.Stop();
            _spinEffect.Clear();
        }

        if (_collider != null)
        {
            _collider.enabled = false;
        }
    }
    #endregion
}
