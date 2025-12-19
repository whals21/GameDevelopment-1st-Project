using UnityEngine;

public class BoomerangProjectile : MonoBehaviour
{
    [Header("Boomerang Settings")]
    [SerializeField] private float throwSpeed = 12f;
    [SerializeField] private float returnSpeed = 15f;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private float pickupRange = 0.5f;
    [SerializeField] private int damage = 20;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Visual Settings")]
    [SerializeField] private float rotationSpeed = 720f; // 회전 속도 (도/초)
    [SerializeField] private TrailRenderer trailRenderer;

    private Rigidbody2D rb;
    private CircleCollider2D boomerangCollider;
    private Transform playerTransform;
    private Vector2 startPosition;
    private Vector2 throwDirection;
    private Vector2 returnDirection; // 반대 방향 저장
    private float lifeTimer = 0f;
    private bool isReturning = false;
    private const float ENEMY_DAMAGE_INTERVAL = 0.5f; // 같은 적에게 데미지를 주는 간격

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boomerangCollider = GetComponent<CircleCollider2D>();

        // 필수 컴포넌트 확인 및 설정
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        if (boomerangCollider == null)
        {
            boomerangCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        boomerangCollider.isTrigger = true;
        boomerangCollider.radius = 0.3f;
    }

    private void OnEnable()
    {
        ResetBoomerang();
    }

    private void Update()
    {
        // 수명 체크
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            DeactivateBoomerang();
            return;
        }

        if (isReturning)
        {
            // 돌아오는 단계 - 반대 방향으로 이동
            MoveInReverseDirection();
        }
        else
        {
            // 전진 단계
            MoveForward();

            // 최대 거리 도달 시 돌아오기 시작
            if (Vector2.Distance(startPosition, transform.position) >= maxDistance)
            {
                StartReturning();
            }
        }

        // 회전 애니메이션
        UpdateRotation();
    }

    /// <summary>
    /// 초기화 메서드 (SkillManager 호출용)
    /// </summary>
    public void Init(float damage, float speed, Vector2 direction)
    {
        // 값 설정
        this.damage = (int)damage;
        throwSpeed = speed;
        throwDirection = direction.normalized;

        // 반대 방향 저장 (핵심 변경!)
        returnDirection = -throwDirection;

        // 플레이어 찾기 (SkillManager는 player를 넘기지 않음)
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        startPosition = transform.position;

        // Rigidbody 초기 속도 설정
        if (rb != null)
        {
            rb.velocity = throwDirection * throwSpeed;
        }

        // 트레일 효과 시작
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }

        Debug.Log($"Boomerang Init: damage={damage}, speed={speed}, direction={direction}");
    }

    /// <summary>
    /// 부메랑 발사
    /// </summary>
    public void Throw(Vector2 direction, Transform player)
    {
        throwDirection = direction.normalized;
        returnDirection = -throwDirection; // 반대 방향 저장
        playerTransform = player;
        startPosition = transform.position;

        // Rigidbody 초기 속도 설정
        rb.velocity = throwDirection * throwSpeed;

        // 트레일 효과 시작
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }

    /// <summary>
    /// 전진 이동
    /// </summary>
    private void MoveForward()
    {
        // Rigidbody를 사용한 물리적 이동
        if (rb.bodyType != RigidbodyType2D.Dynamic)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        rb.velocity = throwDirection * throwSpeed;
    }

    /// <summary>
    /// 반대 방향으로 이동
    /// </summary>
    private void MoveInReverseDirection()
    {
        // 반대 방향으로 이동
        if (rb.bodyType != RigidbodyType2D.Dynamic)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        rb.velocity = returnDirection * returnSpeed;
    }

    /// <summary>
    /// 돌아오기 시작
    /// </summary>
    private void StartReturning()
    {
        isReturning = true;

        // 속도 즉시 변경
        if (rb != null)
        {
            rb.velocity = returnDirection * returnSpeed;
        }
    }

    /// <summary>
    /// 회전 애니메이션
    /// </summary>
    private void UpdateRotation()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 적 충돌 체크
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            if (other.TryGetComponent<Enemy>(out Enemy enemy))
            {
                enemy.TakeDamage(damage);
                Debug.Log($"부메랑이 적 {enemy.name}에게 {damage} 데미지");
            }
        }
    }

    private void ResetBoomerang()
    {
        lifeTimer = 0f;
        isReturning = false;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }

    private void DeactivateBoomerang()
    {
        // 오브젝트 풀에 반환
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnBoomerang(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 발사 방향 표시
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, throwDirection * 3f);

        // 반대 방향 표시
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, returnDirection * 3f);

        // 최대 거리 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(startPosition, maxDistance);
    }

    #region Properties (for compatibility with existing code)
    public float ReturnSpeed => returnSpeed;
    public float MaxDistance => maxDistance;
    public float PickupRange => pickupRange;
    public bool IsReturning => isReturning;
    #endregion
}