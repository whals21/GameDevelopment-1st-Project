using UnityEngine;
using System.Collections;

/// <summary>
/// 다양한 이동 방식(직선, 부메랑, 유도, 공전, 포물선 등)을 지원하는 통합 투사체
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    #region Serialized Fields
    [Header("공통 설정")]
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private float _maxLifetime = 5f;

    [Header("Boomerang 설정")]
    [SerializeField] private float _boomerangMaxDistance = 8f;
    [SerializeField] private float _boomerangReturnSpeedMult = 1.2f;
    [SerializeField] private float _boomerangRotationSpeed = 720f;

    [Header("Orbit 설정")]
    [SerializeField] private float _orbitBaseRadius = 1f;
    [SerializeField] private float _orbitMaxRadius = 5f;
    [SerializeField] private float _orbitExpansionSpeed = 1f;
    [SerializeField] private float _orbitBaseRotationSpeed = 180f;

    [Header("Molotov 설정")]
    [SerializeField] private float _molotovArcHeight = 3f;
    [SerializeField] private GameObject _fireGroundPrefab;

    [Tooltip("화염지대 크기 증가 비율 (0~1). 0이면 크기가 고정됩니다.")]
    [SerializeField] private float _fireGroundScaleFactor = 0.3f;

    [Tooltip("화염지대 최대 크기 배율 제한")]
    [SerializeField] private float _fireGroundMaxMultiplier = 1.5f;

    [Header("Brick 설정")]
    [SerializeField] private float _brickGravity = 15f;
    [SerializeField] private float _brickBounceDamping = 0.7f;
    [SerializeField] private float _brickBounceMinSpeed = 3f;
    [SerializeField] private int _brickMaxHits = 3;

    [Header("Soccer 설정")]
    [SerializeField] private int _soccerMaxBounces = 5;
    [SerializeField] private float _soccerSpawnRadius = 3f;
    [SerializeField] private int _soccerMaxRelaunchCount = 3;
    #endregion

    #region Private Fields
    private float _damage;
    private float _speed;
    private Vector3 _direction;
    private Vector3 _velocity;
    private ProjectileMovementType _moveType;
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private Transform _owner;
    private Transform _target;
    private float _lifetime;
    private SkillBase _source;  // 통계 기록용 source (발사한 스킬)

    // Boomerang용
    private Vector3 _boomerangStartPosition;
    private bool _boomerangIsReturning;

    // Orbit용
    private float _orbitCurrentRadius;
    private float _orbitCurrentAngle;
    private bool _orbitIsClockwise;
    private int _orbitIndex;

    // Molotov용
    private Vector3 _molotovStartPosition;
    private Vector3 _molotovTargetPosition;
    private float _molotovTravelTime;
    private float _molotovElapsedTime;
    private bool _molotovHasHitGround;

    // Brick용
    private Vector3 _brickHorizontalVelocity;
    private float _brickVerticalVelocity;
    private bool _brickIsRising;
    private int _brickCurrentHits;
    private bool _brickHasDeactivated;  // [v1 참조] 충돌 중복 방지 플래그

    // Soccer용
    private int _soccerCurrentBounces;
    private int _soccerRelaunchCount;
    private Camera _mainCamera;
    private Transform _playerTransform;
    private float _soccerLastBounceTime;
    private bool _soccerIsRecalculating;

    // 관통 효과 (v2)
    private int _penetrationCount = 0;

    // 생존 시간 오버라이드 (v2)
    private float _maxLifetimeOverride = 0f;

    // 이펙트 크기 배율 (v2) - Molotov의 FireGround 크기 등에 사용
    private float _effectSizeMultiplier = 1f;
    #endregion

    #region Properties
    public float Damage => _damage;
    public Transform Owner => _owner;
    public Transform Target => _target;
    #endregion

    #region Initialization
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.bodyType = RigidbodyType2D.Kinematic;

        // 시각 효과 (v2) - SpriteRenderer 참조
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        _mainCamera = Camera.main;
        if (_mainCamera == null) _mainCamera = FindObjectOfType<Camera>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerTransform = player.transform;
    }

    private void OnEnable()
    {
        ResetState();
    }

    private void ResetState()
    {
        _lifetime = 0f;
        _boomerangIsReturning = false;
        _orbitCurrentRadius = _orbitBaseRadius;
        _molotovElapsedTime = 0f;
        _molotovHasHitGround = false;
        _brickIsRising = true;
        _brickCurrentHits = 0;
        _brickHasDeactivated = false;  // [v1 참조] 비활성화 플래그 초기화
        _soccerCurrentBounces = 0;
        _soccerRelaunchCount = 0;
        _soccerIsRecalculating = false;
        _penetrationCount = 0;
        _maxLifetimeOverride = 0f;

        if (_rb != null)
        {
            _rb.velocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    /// <summary>
    /// 투사체 설정
    /// </summary>
    public void Setup(Vector3 direction, float damage, float speed, ProjectileMovementType moveType = ProjectileMovementType.Straight, SkillBase source = null)
    {
        _direction = direction.normalized;
        _damage = damage;
        _speed = speed;
        _moveType = moveType;
        _source = source;  // 통계 기록용 source 저장

        transform.right = _direction;

        // 타입별 초기화
        switch (_moveType)
        {
            case ProjectileMovementType.Straight:
                _velocity = _direction * _speed;
                break;

            case ProjectileMovementType.Boomerang:
                _boomerangStartPosition = transform.position;
                _velocity = _direction * _speed;
                if (_rb != null) _rb.bodyType = RigidbodyType2D.Dynamic;
                break;

            case ProjectileMovementType.Homing:
                FindHomingTarget();
                _velocity = _direction * _speed;
                break;

            case ProjectileMovementType.Orbit:
                _orbitCurrentAngle = _orbitIndex == 0 ? 0f : 180f;
                break;

            case ProjectileMovementType.Molotov:
                // [v1 참조] 콜라이더를 트리거로 설정하여 물리 충돌 방지
                Collider2D col = GetComponent<Collider2D>();
                if (col != null)
                {
                    col.isTrigger = true;
                }

                // [v1 참조] Rigidbody를 Kinematic으로 설정 (물리 엔진 제어)
                if (_rb != null)
                {
                    _rb.bodyType = RigidbodyType2D.Kinematic;
                    _rb.gravityScale = 0f;
                    _rb.velocity = Vector2.zero;
                }

                _molotovStartPosition = transform.position;
                _molotovTargetPosition = transform.position + _direction * 10f;
                float dist = Vector2.Distance(new Vector2(_molotovStartPosition.x, _molotovStartPosition.y),
                                              new Vector2(_molotovTargetPosition.x, _molotovTargetPosition.y));
                _molotovTravelTime = Mathf.Max(0.5f, dist / 5f);
                break;

            case ProjectileMovementType.Brick:
                // [v1 참조] 콜라이더를 트리거로 설정
                Collider2D brickCol = GetComponent<Collider2D>();
                if (brickCol != null)
                {
                    brickCol.isTrigger = true;
                }

                // [v1 참조] Rigidbody를 Kinematic으로 설정
                if (_rb != null)
                {
                    _rb.bodyType = RigidbodyType2D.Kinematic;
                    _rb.gravityScale = 0f;
                    _rb.velocity = Vector2.zero;
                }

                _brickHorizontalVelocity = new Vector3(_direction.x * _speed * 0.5f, 0, 0);
                _brickVerticalVelocity = _speed * 0.8f;
                break;

            case ProjectileMovementType.Soccer:
                _velocity = _direction * _speed;
                break;
        }
    }

    /// <summary>
    /// Orbit 타입의 공전 인덱스 설정 (0: 시계, 1: 반시계)
    /// </summary>
    public void SetOrbitIndex(int index)
    {
        _orbitIndex = index;
        _orbitIsClockwise = (index == 0);
        _orbitCurrentAngle = _orbitIsClockwise ? 0f : 180f;
    }

    /// <summary>
    /// 소유자 설정
    /// </summary>
    public void SetOwner(Transform owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 관통 횟수 설정 (v2)
    /// 0이면 관통 없음, 1이면 1명 관통, 999이면 무한 관통
    /// </summary>
    public void SetPenetration(int count)
    {
        _penetrationCount = count;
    }

    /// <summary>
    /// 생존 시간 오버라이드 설정 (v2)
    /// 0이면 Projectile 프리팹 기본값 사용, 0보다 크면 해당 값 사용
    /// </summary>
    public void SetLifetime(float lifetime)
    {
        _maxLifetimeOverride = lifetime;
    }

    /// <summary>
    /// 시각 효과 설정 (v2) - 스프라이트, 색상, 크기
    /// </summary>
    public void SetSprite(Sprite sprite, Color color, float scale = 1f)
    {
        if (_spriteRenderer != null)
        {
            if (sprite != null)
            {
                _spriteRenderer.sprite = sprite;
                Debug.Log($"[Projectile] 스프라이트 변경: {sprite.name}");
            }
            else
            {
                Debug.Log("[Projectile] sprite가 null입니다 - 기본 스프라이트 유지");
            }
            _spriteRenderer.color = color;
            transform.localScale = Vector3.one * scale;
        }
        else
        {
            Debug.LogError("[Projectile] _spriteRenderer가 null입니다!");
        }
    }

    /// <summary>
    /// 이펙트 크기 배율 설정 (v2)
    /// Molotov의 FireGround, 기타 투사체 도착 시 생성되는 이펙트의 크기에 적용
    /// </summary>
    public void SetEffectSizeMultiplier(float multiplier)
    {
        _effectSizeMultiplier = multiplier;
    }

    private void FindHomingTarget()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        if (enemies.Length > 0)
        {
            // 가장 가까운 적 찾기
            float minDist = float.MaxValue;
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.CurrentHP <= 0) continue;
                float dist = Vector3.SqrMagnitude(transform.position - enemy.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    _target = enemy.transform;
                }
            }
        }
    }
    #endregion

    #region Core Loop
    private void Update()
    {
        if (_soccerIsRecalculating) return;

        _lifetime += Time.deltaTime;

        // 최대 생존 시간 체크 (v2: 오버라이드 우선)
        float effectiveMaxLifetime = _maxLifetimeOverride > 0 ? _maxLifetimeOverride : _maxLifetime;
        if (_lifetime > effectiveMaxLifetime)
        {
            ReturnToPoolOrDestroy();
            return;
        }

        // 이동 타입별 업데이트
        switch (_moveType)
        {
            case ProjectileMovementType.Straight:
                UpdateStraight();
                break;
            case ProjectileMovementType.Boomerang:
                UpdateBoomerang();
                break;
            case ProjectileMovementType.Homing:
                UpdateHoming();
                break;
            case ProjectileMovementType.Orbit:
                UpdateOrbit();
                break;
            case ProjectileMovementType.Molotov:
                UpdateMolotov();
                break;
            case ProjectileMovementType.Brick:
                UpdateBrick();
                break;
            case ProjectileMovementType.Soccer:
                UpdateSoccer();
                break;
        }
    }

    private void FixedUpdate()
    {
        // Rigidbody 기반 이동이 필요한 타입들
        if (_moveType == ProjectileMovementType.Boomerang)
        {
            if (_rb.bodyType != RigidbodyType2D.Dynamic)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
            }

            Vector3 vel = _boomerangIsReturning ?
                (-_direction * _speed * _boomerangReturnSpeedMult) :
                (_direction * _speed);
            _rb.velocity = vel;
        }
    }
    #endregion

    #region Movement Types

    #region Straight
    private void UpdateStraight()
    {
        transform.position += _velocity * Time.deltaTime;
    }
    #endregion

    #region Boomerang
    private void UpdateBoomerang()
    {
        // 회전 애니메이션
        transform.Rotate(0, 0, _boomerangRotationSpeed * Time.deltaTime);

        // 최대 거리 체크
        if (!_boomerangIsReturning)
        {
            float dist = Vector3.Distance(_boomerangStartPosition, transform.position);
            if (dist >= _boomerangMaxDistance)
            {
                _boomerangIsReturning = true;
            }
        }
    }
    #endregion

    #region Homing
    private void UpdateHoming()
    {
        // 타겟이 없으면 재검색
        if (_target == null)
        {
            FindHomingTarget();
        }

        // 타겟 방향으로 이동
        if (_target != null)
        {
            Vector3 targetDir = (_target.position - transform.position).normalized;
            _velocity = targetDir * _speed;
            transform.right = targetDir;
        }

        transform.position += _velocity * Time.deltaTime;
    }
    #endregion

    #region Orbit
    private void UpdateOrbit()
    {
        if (_playerTransform == null) return;

        // 반경 확장
        _orbitCurrentRadius += _orbitExpansionSpeed * Time.deltaTime;
        if (_orbitCurrentRadius >= _orbitMaxRadius)
        {
            _orbitCurrentRadius = _orbitBaseRadius; // 리셋
        }

        // 각도 업데이트 (반경에 비례하여 속도 증가)
        float speedMult = 1f + (_orbitCurrentRadius / _orbitMaxRadius);
        float angleDelta = _orbitBaseRotationSpeed * speedMult * Time.deltaTime;
        _orbitCurrentAngle += _orbitIsClockwise ? angleDelta : -angleDelta;

        // 위치 계산
        Vector3 offset = new Vector3(
            Mathf.Cos(_orbitCurrentAngle * Mathf.Deg2Rad) * _orbitCurrentRadius,
            Mathf.Sin(_orbitCurrentAngle * Mathf.Deg2Rad) * _orbitCurrentRadius,
            0
        );
        transform.position = _playerTransform.position + offset;
        transform.rotation = Quaternion.Euler(0, 0, _orbitCurrentAngle * (_orbitIsClockwise ? 1 : -1));
    }
    #endregion

    #region Molotov
    private void UpdateMolotov()
    {
        if (_molotovHasHitGround) return;

        _molotovElapsedTime += Time.deltaTime;

        if (_molotovElapsedTime >= _molotovTravelTime)
        {
            transform.position = _molotovTargetPosition;
            OnMolotovHitGround();
        }
        else
        {
            // 포물선 보간
            float t = _molotovElapsedTime / _molotovTravelTime;

            // 수평 이동
            Vector3 horizontalPos = Vector3.Lerp(_molotovStartPosition, _molotovTargetPosition, t);

            // 수직 이동 (포물선)
            float arc = _molotovArcHeight * 4f * t * (1f - t);
            horizontalPos.y = Mathf.Lerp(_molotovStartPosition.y, _molotovTargetPosition.y, t) + arc;

            // 회전 (위치 변경 전에 계산 - 현재 위치에서 다음 위치로의 방향)
            Vector3 direction = (horizontalPos - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.right = direction;
            }

            transform.position = horizontalPos;
        }
    }

    private void OnMolotovHitGround()
    {
        if (_molotovHasHitGround) return;
        _molotovHasHitGround = true;

        // 화염지대 크기 증가율 보정
        // 투사체는 누적으로 커지지만, 화염지대는 그보다 덜 커지도록 보정
        // 예: _effectSizeMultiplier가 2.5라면 -> 1.0 + (1.5 * 0.3) = 1.45배만 적용
        float adjustedMultiplier = 1.0f + ((_effectSizeMultiplier - 1.0f) * _fireGroundScaleFactor);
        adjustedMultiplier = Mathf.Clamp(adjustedMultiplier, 0.8f, _fireGroundMaxMultiplier);

        // 화염 지대 생성 (풀링 적용 - 전문가 피드백)
        if (ObjectPoolManager.Instance != null)
        {
            FireGround fireGround = ObjectPoolManager.Instance.GetFireGround();
            if (fireGround != null)
            {
                fireGround.SetPosition(transform.position);
                // 보정된 크기 배율 적용
                fireGround.Init(_damage * 0.5f, 5f, adjustedMultiplier, _source);
            }
        }
        else if (_fireGroundPrefab != null)
        {
            // 폴백: 풀이 없으면 생성 (개발용)
            GameObject fire = Instantiate(_fireGroundPrefab, transform.position, Quaternion.identity);
            FireGround fg = fire.GetComponent<FireGround>();
            if (fg != null)
            {
                // 보정된 크기 배율 적용
                fg.Init(_damage * 0.5f, 5f, adjustedMultiplier, _source);
            }
            Destroy(fire, 6f);
        }

        ReturnToPoolOrDestroy();
    }
    #endregion

    #region Brick
    private void UpdateBrick()
    {
        // [v1 참조] 중력 기반 물리
        if (_brickIsRising)
        {
            // 상승 단계: 수직 속도 감소
            _brickVerticalVelocity -= _brickGravity * Time.deltaTime;

            // 위치 업데이트
            Vector3 movement = (_brickHorizontalVelocity + Vector3.up * _brickVerticalVelocity) * Time.deltaTime;
            transform.position += movement;

            // 회전 (날아가는 방향으로)
            if (movement != Vector3.zero)
            {
                transform.right = movement.normalized;
            }

            // 하강 시작 체크
            if (_brickVerticalVelocity <= 0)
            {
                _brickIsRising = false;
            }
        }
        else
        {
            // 하강 단계: 중력으로 가속
            _brickVerticalVelocity -= _brickGravity * Time.deltaTime;

            // 위치 업데이트
            Vector3 movement = (_brickHorizontalVelocity + Vector3.up * _brickVerticalVelocity) * Time.deltaTime;
            transform.position += movement;

            // 회전 (낙하 방향으로)
            if (movement != Vector3.zero)
            {
                transform.right = movement.normalized;
            }

            // [v1 참조] 지면 체크 (y < -10)
            if (transform.position.y < -10f)
            {
                OnBrickHitGround();
            }
        }

        // [v1 참조] 스크린 밖으로 나가면 비활성화
        if (transform.position.y < -10f || Mathf.Abs(transform.position.x) > 20f)
        {
            ReturnToPoolOrDestroy();
        }
    }

    private void OnBrickHitGround()
    {
        // 튕김 효과
        _brickIsRising = false;
        _brickVerticalVelocity = -_brickVerticalVelocity * _brickBounceDamping;
        _brickVerticalVelocity = Mathf.Max(_brickVerticalVelocity, _brickBounceMinSpeed);
    }
    #endregion

    #region Soccer
    private void UpdateSoccer()
    {
        transform.position += _direction * _speed * Time.deltaTime;

        // 회전 효과
        if (_direction != Vector3.zero)
        {
            transform.right = _direction;
            transform.Rotate(0, 0, _speed * Time.deltaTime * 50f);
        }

        // 화면 밖 체크
        if (_mainCamera != null && _lifetime > 0.5f)
        {
            Vector3 viewportPos = _mainCamera.WorldToViewportPoint(transform.position);
            if (viewportPos.x < -0.2f || viewportPos.x > 1.2f || viewportPos.y < -0.2f || viewportPos.y > 1.2f)
            {
                StartCoroutine(SoccerRelaunch());
            }
        }
    }

    private IEnumerator SoccerRelaunch()
    {
        if (_soccerRelaunchCount >= _soccerMaxRelaunchCount)
        {
            ReturnToPoolOrDestroy();
            yield break;
        }

        _soccerIsRecalculating = true;
        yield return new WaitForSeconds(0.1f);

        _soccerRelaunchCount++;
        _soccerCurrentBounces = 0;

        // 플레이어 주변 랜덤 위치
        if (_playerTransform != null)
        {
            Vector2 offset = Random.insideUnitCircle * _soccerSpawnRadius;
            transform.position = _playerTransform.position + (Vector3)offset;
        }

        // 새로운 방향
        FindHomingTarget();
        if (_target != null)
        {
            _direction = (_target.position - transform.position).normalized;
        }
        else
        {
            float angle = Random.Range(0f, 360f);
            _direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
        }

        _speed *= 0.9f; // 속도 감소
        _soccerIsRecalculating = false;
    }
    #endregion

    #endregion

    #region Collision
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // [v1 참조] Brick 충돌 중복 방지
        if (_moveType == ProjectileMovementType.Brick && _brickHasDeactivated)
        {
            return;
        }

        // Soccer 튕김 간격 체크
        if (_moveType == ProjectileMovementType.Soccer)
        {
            if (Time.time - _soccerLastBounceTime < 0.1f) return;
        }

        // 적 충돌 (Enemy 또는 Boss)
        if (((1 << collision.gameObject.layer) & _enemyLayer) != 0)
        {
            // IDamageable 인터페이스로 통합 처리 (Enemy, Boss 모두 지원)
            if (collision.TryGetComponent<IDamageable>(out var damageable) && damageable.CurrentHP > 0)
            {
                damageable.TakeDamage(_damage, _source);  // 통계 기록용 source 전달

                // [v2] 폭발 이펙트 생성 (RPG/Homing 타입)
                if (_moveType == ProjectileMovementType.Homing)
                {
                    SpawnExplosionEffect();
                }

                // [v2] 관통 효과 체크
                if (_penetrationCount > 0)
                {
                    _penetrationCount--;
                    return; // 관통 횟수가 남으면 제거하지 않음
                }

                // 타입별 추가 처리
                switch (_moveType)
                {
                    case ProjectileMovementType.Brick:
                        _brickCurrentHits++;
                        if (_brickCurrentHits >= _brickMaxHits)
                        {
                            _brickHasDeactivated = true;  // [v1 참조] 중복 방지 플래그 설정
                            ReturnToPoolOrDestroy();
                            return;
                        }
                        break;
                    case ProjectileMovementType.Soccer:
                        _soccerCurrentBounces++;
                        _soccerLastBounceTime = Time.time;
                        if (_soccerCurrentBounces >= _soccerMaxBounces)
                        {
                            ReturnToPoolOrDestroy();
                            return;
                        }
                        // 튕김
                        Vector3 normal = (transform.position - collision.transform.position).normalized;
                        _direction = Vector3.Reflect(_direction, normal);
                        float randomAngle = Random.Range(-15f, 15f);
                        _direction = Quaternion.Euler(0, 0, randomAngle) * _direction;
                        break;
                    default:
                        // 기본적으로 충돌 후 제거
                        if (_moveType != ProjectileMovementType.Orbit)
                        {
                            ReturnToPoolOrDestroy();
                        }
                        break;
                }
            }
        }

        // 장애물 충돌
        else if (((1 << collision.gameObject.layer) & _obstacleLayer) != 0)
        {
            switch (_moveType)
            {
                case ProjectileMovementType.Soccer:
                    _soccerCurrentBounces++;
                    _soccerLastBounceTime = Time.time;
                    if (_soccerCurrentBounces >= _soccerMaxBounces)
                    {
                        ReturnToPoolOrDestroy();
                        return;
                    }
                    Vector3 normal = -_direction;
                    _direction = Vector3.Reflect(_direction, normal);
                    break;
                case ProjectileMovementType.Orbit:
                    break; // 통과
                default:
                    ReturnToPoolOrDestroy();
                    break;
            }
        }
    }
    #endregion

    #region Effects
    /// <summary>
    /// 폭발 이펙트 생성 (RPG/Homing 타입 충돌 시)
    /// LightningStrike와 동일한 방식으로 ObjectPool에서 가져옵니다.
    /// </summary>
    private void SpawnExplosionEffect()
    {
        ObjectPoolManager.Instance?.GetExplosion()?.SetPosition(transform.position);
    }
    #endregion

    #region Cleanup
    private void ReturnToPoolOrDestroy()
    {
        gameObject.SetActive(false);

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnProjectile(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion
}
