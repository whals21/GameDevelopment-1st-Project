using UnityEngine;

public class RPGProjectile : MonoBehaviour
{
    #region Serialized Fields
    [Header("프로젝타일 설정")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float lifetime = 5f;

    [Header("폭발 설정")]
    [SerializeField] private float explosionRadius = 2.5f;

    [Header("레벨별 스탯 설정 (Inspector에서 조정 가능)")]
    [SerializeField] private float[] damageLevels = {50f, 60f, 70f, 80f, 90f};
    [SerializeField] private float[] speedLevels = {10f, 12f, 14f, 16f, 18f};
    [SerializeField] private float[] explosionRadiusLevels = {2.5f, 2.8f, 3.1f, 3.4f, 3.7f};
    [SerializeField] private int[] projectileCountLevels = {1, 2, 3, 3, 4};

    [Header("시각 효과")]
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private ParticleSystem launchParticles;

    [Header("오디오")]
    [SerializeField] private AudioClip launchSound;
    [SerializeField] private float soundVolume = 0.7f;
    #endregion

    #region Private Fields
    private Rigidbody2D rb;
    private Collider2D projectileCollider;
    private AudioSource audioSource;
    private float damage;
    private Vector3 moveDirection;
    private float moveSpeed;
    private bool hasExploded = false;
    private float timeAlive = 0f;
    private bool isMoving = false;

    // 레벨 관련
    private int currentLevel = 1;
    private float currentExplosionRadius;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        projectileCollider = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();

        // 컴포넌트가 없으면 추가
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // 중력 없음 - 직선 이동
            rb.bodyType = RigidbodyType2D.Kinematic; // 물리적 영향 없이 직접 제어
        }

        if (projectileCollider == null)
        {
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = 0.3f;
            circleCollider.isTrigger = true;
            projectileCollider = circleCollider;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D 사운드
        }
    }

    private void OnEnable()
    {
        ResetProjectile();
    }

    private void Update()
    {
        timeAlive += Time.deltaTime;

        // 수명 체크
        if (timeAlive >= lifetime)
        {
            if (true) // 항상 타임아웃 시 폭발
            {
                Explode();
            }
            else
            {
                ReturnToPool();
            }
        }

        // 직선 이동 (transform 직접 제어)
        if (isMoving)
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;

            // 이동 방향으로 회전
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded) return;

        // 적과 충돌 시 폭발
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            Explode();
        }
        // 지형과 충돌 시도 폭발
        else if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            Explode();
        }
    }

    private void OnDisable()
    {
        ResetProjectile();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 프로젝타일 매개변수 설정
    /// </summary>
    public void SetParameters(Vector3 startPosition, Vector3 direction, float damage, float speed)
    {
        // 레벨 1 기본값 설정 (호환성 유지)
        SetLevel(1);

        // 개별 값 설정 (레벨 시스템과 호환)
        if (damage > 0) this.damage = damage;
        if (speed > 0) this.moveSpeed = speed;

        this.moveDirection = direction.normalized;
        hasExploded = false;
        timeAlive = 0f;
        isMoving = true;

        // 위치 설정
        transform.position = startPosition;

        // Rigidbody2D를 Kinematic으로 설정하여 직접 제어
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // 발사 이펙트
        PlayLaunchEffects();
    }

    // 레벨별 파라미터 설정 (권장)
    public void SetParameters(Vector3 startPosition, Vector3 direction, int level)
    {
        SetLevel(level);
        this.moveDirection = direction.normalized;
        hasExploded = false;
        timeAlive = 0f;
        isMoving = true;

        // 위치 설정
        transform.position = startPosition;

        // Rigidbody2D를 Kinematic으로 설정하여 직접 제어
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // 발사 이펙트
        PlayLaunchEffects();
    }

    // 레벨 설정 메서드
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, damageLevels.Length);
        UpdateStatsForLevel();
    }

    private void UpdateStatsForLevel()
    {
        int levelIndex = currentLevel - 1;

        // 스탯 업데이트
        damage = damageLevels[levelIndex];
        moveSpeed = speedLevels[levelIndex];
        currentExplosionRadius = explosionRadiusLevels[levelIndex];
    }
    #endregion

    #region Private Methods
    // CalculateLaunchVelocity 메서드는 더 이상 사용하지 않음 (직선 이동 방식으로 변경)

    /// <summary>
    /// 폭발 실행
    /// </summary>
    private void Explode()
    {
        if (hasExploded) return;

        hasExploded = true;

        // 폭발 이펙트 생성
        CreateExplosion();

        // 사운드 재생
        PlayExplosionSound();

        // 오브젝트 비활성화
        ReturnToPool();
    }

    /// <summary>
    /// 폭발 이펙트 생성
    /// </summary>
    private void CreateExplosion()
    {
        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogWarning("RPGProjectile: ObjectPoolManager를 찾을 수 없습니다!");
            return;
        }

        // 폭발 오브젝트 가져오기
        RPGExplosion explosion = ObjectPoolManager.Instance.GetRPGExplosion();
        if (explosion != null)
        {
            // 폭발 위치와 데미지 설정
            explosion.SetExplosion(transform.position, damage, currentExplosionRadius, enemyLayer);
        }
    }

    /// <summary>
    /// 발사 효과 재생
    /// </summary>
    private void PlayLaunchEffects()
    {
        // Trail 초기화
        if (trail != null)
        {
            trail.Clear();
        }

        // 파티클 재생
        if (launchParticles != null)
        {
            launchParticles.Play();
        }

        // 사운드 재생
        if (launchSound != null && audioSource != null)
        {
            audioSource.clip = launchSound;
            audioSource.volume = soundVolume;
            audioSource.Play();
        }
    }

    /// <summary>
    /// 폭발 사운드 재생
    /// </summary>
    private void PlayExplosionSound()
    {
        // 폭발 사운드는 RPGExplosion에서 처리하거나 여기서 처리
    }

    /// <summary>
    /// 프로젝타일 리셋
    /// </summary>
    private void ResetProjectile()
    {
        hasExploded = false;
        timeAlive = 0f;
        isMoving = false;
        moveDirection = Vector3.zero;
        moveSpeed = 0f;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (trail != null)
        {
            trail.Clear();
        }
    }

    /// <summary>
    /// 풀에 반환
    /// </summary>
    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnRPG(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    #endregion

    #region Debug
    private void OnDrawGizmos()
    {
        // 속도 벡터 표시
        if (isMoving && moveSpeed > 0.1f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)moveDirection * 2f);
        }
    }
    #endregion
}