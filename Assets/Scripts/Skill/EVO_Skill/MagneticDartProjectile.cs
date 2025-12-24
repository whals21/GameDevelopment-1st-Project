using UnityEngine;

/// <summary>
/// 마그네틱 다트 진화 스킬의 개별 투사체
/// 플레이어 주변을 공전하며 적에게 데미지를 입히고 적 투사체를 차단함
/// </summary>
public class MagneticDartProjectile : MonoBehaviour
{
    #region Serialized Fields
    [Header("공전 설정")]
    [SerializeField] private float baseOrbitRadius = 1f;     // 기본 공전 반경
    [SerializeField] private float maxOrbitRadius = 5f;      // 최대 공전 반경
    [SerializeField] private float expansionSpeed = 1f;      // 반경 확장 속도 (m/s)
    [SerializeField] private float baseRotationSpeed = 180f; // 기본 공전 속도 (도/초)

    [Header("전투")]
    [SerializeField] private int damage = 15;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask enemyProjectileLayer;

    [Header("시각적 효과")]
    [SerializeField] private TrailRenderer trailRenderer;
    [SerializeField] private SpriteRenderer spriteRenderer;
    #endregion

    #region Private Fields
    private Transform player;
    private float currentRadius;     // 현재 공전 반경
    private float currentAngle;      // 현재 공전 각도
    private bool isClockwise;        // 시계 방향 여부 (2개의 투사체가 반대)
    private bool isActive = false;

    // 레벨별 스탯
    private int currentLevel = 1;
    private float currentDamage;
    #endregion

    #region Properties
    public bool IsActive => isActive;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 컴포넌트 캐싱
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
        }
    }

    private void OnEnable()
    {
        // 초기화
        ResetOrbit();
    }

    private void Update()
    {
        if (!isActive || player == null) return;

        // 반경 확장
        currentRadius += expansionSpeed * Time.deltaTime;
        if (currentRadius >= maxOrbitRadius)
        {
            ResetOrbit(); // 최대 범위 도달 시 초기 위치로 리셋
        }

        // 각도 업데이트 (반경에 비례하여 속도 증가)
        float speedMultiplier = 1f + (currentRadius / maxOrbitRadius); // 1x ~ 2x 속도
        float angleDelta = baseRotationSpeed * speedMultiplier * Time.deltaTime;
        currentAngle += isClockwise ? angleDelta : -angleDelta;

        // 위치 계산
        Vector3 offset = new Vector3(
            Mathf.Cos(currentAngle * Mathf.Deg2Rad) * currentRadius,
            Mathf.Sin(currentAngle * Mathf.Deg2Rad) * currentRadius,
            0
        );
        transform.position = player.position + offset;

        // 스프라이트 회전 (이동 방향으로)
        if (spriteRenderer != null)
        {
            spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, currentAngle * (isClockwise ? 1 : -1));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

        // 적 충돌 → 데미지
        if (IsInLayer(other.gameObject, enemyLayer))
        {
            if (other.TryGetComponent<Enemy>(out Enemy enemy))
            {
                enemy.TakeDamage(currentDamage);
            }
        }

        // 적 투사체 충돌 → 차단 (파괴)
        if (IsInLayer(other.gameObject, enemyProjectileLayer))
        {
            if (other.TryGetComponent<EnemyBullet>(out var bullet))
            {
                // 풀에 반환하거나 파괴
                ObjectPoolManager.Instance?.ReturnEnemyBullet(bullet);
            }
            else
            {
                Destroy(other.gameObject);
            }
        }
    }

    private void OnDisable()
    {
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 투사체 초기화
    /// </summary>
    /// <param name="playerTransform">플레이어 Transform</param>
    /// <param name="clockwise">시계 방향 여부 (true: 시계, false: 반시계)</param>
    public void Initialize(Transform playerTransform, bool clockwise)
    {
        player = playerTransform;
        isClockwise = clockwise;
        isActive = true;
        currentDamage = damage;

        // 시작 각도 설정 (두 투사체가 반대 방향에서 시작)
        currentAngle = clockwise ? 0f : 180f;

        ResetOrbit();

        // 시각적 효과 초기화
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.enabled = true;
        }
    }

    /// <summary>
    /// 레벨 설정
    /// </summary>
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, 5);
        UpdateStatsForLevel();
    }

    /// <summary>
    /// 비활성화
    /// </summary>
    public void Deactivate()
    {
        isActive = false;

        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 재활성화
    /// </summary>
    public void Reactivate()
    {
        if (!isActive)
        {
            isActive = true;
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.enabled = true;
            }
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 공전 궤도 리셋 (최대 범위 도달 시 호출)
    /// </summary>
    private void ResetOrbit()
    {
        currentRadius = baseOrbitRadius;
        // 각도는 유지하거나 초기화 가능
    }

    /// <summary>
    /// 레벨에 따른 스탯 업데이트
    /// </summary>
    private void UpdateStatsForLevel()
    {
        // 레벨별 데미지 증가
        float[] damageLevels = { 15f, 20f, 25f, 35f, 50f };
        int levelIndex = currentLevel - 1;
        if (levelIndex < damageLevels.Length)
        {
            currentDamage = damageLevels[levelIndex];
        }
    }

    /// <summary>
    /// 레이어 체크 헬퍼 메서드
    /// </summary>
    private bool IsInLayer(GameObject obj, LayerMask layerMask)
    {
        return ((1 << obj.layer) & layerMask) != 0;
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        if (!isActive) return;

        // 현재 공전 궤도 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player != null ? player.position : transform.position, currentRadius);

        // 최대 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player != null ? player.position : transform.position, maxOrbitRadius);
    }
    #endregion
}
