using UnityEngine;

/// <summary>
/// 플레이어 주변을 공전하며 가장 가까운 적에게 투사체를 발사하는 드론
/// </summary>
public class Drone : MonoBehaviour
{
    #region Private Fields
    // 데이터 (외부에서 주입)
    private Transform _playerTransform;
    private float _damage;
    private float _projectileSpeed;
    private float _cooldown;
    private float _orbitRadius;
    private float _orbitSpeed;
    private int _enemyLayerMask;

    // 상태
    private float _currentAngle;
    private float _attackTimer;
    private bool _isActive;
    private int _level = 1;  // 현재 레벨 (크기 계산용)

    // 컴포넌트
    private Rigidbody2D _rb;
    private Collider2D _collider;

    // SkillData 참조 (투사체 발사용)
    private SkillData _data;
    #endregion

    #region Properties
    public bool IsActive => _isActive;
    #endregion

    #region Initialization
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();

        // Kinematic 모드 설정 (GuardianTop과 통일)
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.gravityScale = 0f;
        }

        if (_collider != null)
        {
            _collider.isTrigger = true;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 비활성화 시 자동 리셋 (캡슐화)
    /// ObjectPool.Return()에서 SetActive(false) 호출 시 자동으로 실행됨
    /// </summary>
    private void OnDisable()
    {
        ResetForReuse();
    }

    /// <summary>
    /// Drone 초기화 (데이터 주도 설계)
    /// </summary>
    public void Initialize(Transform player, float damage, float cooldown, float radius, float speed, float projectileSpeed, int enemyLayerMask, SkillData data, int level = 1)
    {
        _playerTransform = player;
        _damage = damage;
        _cooldown = cooldown;
        _orbitRadius = radius;
        _orbitSpeed = speed;
        _projectileSpeed = projectileSpeed;
        _enemyLayerMask = enemyLayerMask;
        _data = data;
        _level = level;

        _currentAngle = 0f;
        _attackTimer = 0f;
        _isActive = true;

        gameObject.SetActive(true);
    }
    #endregion

    #region Core Loop
    /// <summary>
    /// 1. Update/FixedUpdate 통합 (지터 방지)
    /// 드론은 물리와 강하게 상호작용하지 않으므로 Update만 사용
    /// </summary>
    private void Update()
    {
        if (!_isActive || _playerTransform == null) return;

        // 공전 이동
        UpdateOrbitMovement();

        // 공격 시도
        UpdateAttack();
    }

    /// <summary>
    /// 2. 공전 효과 복원 (이전 DroneBehavior)
    /// </summary>
    private void UpdateOrbitMovement()
    {
        // 각도 업데이트
        _currentAngle += _orbitSpeed * Time.deltaTime;
        _currentAngle %= 360f;

        // 타원형 공전 (y축을 0.3배로 납작하게)
        float radians = _currentAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(radians) * _orbitRadius,
            Mathf.Sin(radians) * _orbitRadius * 0.3f,
            0
        );

        transform.position = _playerTransform.position + orbitOffset;
    }

    /// <summary>
    /// 3. 공격 로직 추가 (TargetingHelper 사용)
    /// </summary>
    private void UpdateAttack()
    {
        _attackTimer += Time.deltaTime;

        if (_attackTimer >= _cooldown)
        {
            _attackTimer = 0f;
            FireAtNearestEnemy();
        }
    }
    #endregion

    #region Combat
    /// <summary>
    /// 가장 가까운 적에게 미사일 발사
    /// </summary>
    private void FireAtNearestEnemy()
    {
        if (_data == null || _data.prefab == null) return;

        float searchRadius = _data.enemySearchRadius > 0 ? _data.enemySearchRadius : 20f;
        Vector3 origin = transform.position;

        // TargetingHelper 사용
        Enemy nearest = TargetingHelper.FindNearestEnemy(origin, searchRadius, _enemyLayerMask);

        if (nearest != null)
        {
            Vector3 direction = (nearest.transform.position - origin).normalized;

            // 투사체 발사 (ObjectPool 사용)
            Projectile projectile = null;
            if (ObjectPoolManager.Instance != null)
            {
                projectile = ObjectPoolManager.Instance.GetProjectile();
            }

            if (projectile == null)
            {
                GameObject projObj = Instantiate(_data.prefab, origin, Quaternion.identity);
                projectile = projObj.GetComponent<Projectile>();

                if (projectile == null)
                {
                    projectile = projObj.AddComponent<Projectile>();
                }
            }
            else
            {
                projectile.gameObject.SetActive(true);
                projectile.gameObject.transform.position = origin;
            }

            // 투사체 설정
            projectile.Setup(direction, _damage, _projectileSpeed, _data.movementType);

            // 시각 효과 설정 (v2) - 레벨별 크기 배수 적용
            float finalScale = _data.projectileScale;
            if (_data.levels != null && _level > 0 && _level <= _data.levels.Length)
            {
                int levelIndex = _level - 1;
                if (_data.levels[levelIndex] != null)
                {
                    finalScale *= _data.levels[levelIndex].projectileSizeMultiplier;
                }
            }
            projectile.SetSprite(_data.projectileSprite, _data.projectileColor, finalScale);
        }
    }

    /// <summary>
    /// 적 투사체 방어
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isActive) return;

        if (other.CompareTag("EnemyProjectile"))
        {
            // TODO: ObjectPoolManager에 EnemyBullet 풀 추가 후 사용
            Destroy(other.gameObject);
        }
    }
    #endregion

    #region Public Methods
    public void Activate()
    {
        _isActive = true;
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        _isActive = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 풀링 재사용을 위한 리셋
    /// OnDisable에서 자동 호출되므로 SetActive(false) 제거 (중복 방지)
    /// </summary>
    public void ResetForReuse()
    {
        _isActive = false;
        _currentAngle = 0f;
        _attackTimer = 0f;
        _playerTransform = null;
    }

    /// <summary>
    /// 스탯 업데이트
    /// </summary>
    public void UpdateStats(float damage, float cooldown, float speed, int level = 1)
    {
        _damage = damage;
        _cooldown = cooldown;
        _orbitSpeed = speed;
        _level = level;
    }
    #endregion
}
