using UnityEngine;

public class BoomerangProjectile : Projectile
{
    [Header("부메랑 설정")]
    [SerializeField] private float returnSpeed = 20f;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float pickupRange = 0.5f;
    [SerializeField] private LayerMask ignoreLayers; // 무시할 레이어 (Forcefield 등)
    // lifeTime은 부모 클래스의 필드를 사용 (Inspector에서는 Projectile 기본값 수정)

    private Vector3 startPosition;
    private bool isReturning = false;
    private bool hasDeactivated = false;
    private Transform playerTransform;

    // Kinematic 모드 이동을 위한 변수
    private float currentSpeed;
    private Vector2 currentDirection;

    // 부메랑의 수명은 10초
    protected override float LifeTime => 10f;

    void Awake()
    {
        // 컴포넌트 설정
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Collider를 Trigger로 설정
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }

        // 플레이어 캐싱
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    // 부메랑 초기화
    public void Init(float damage, float speed, Vector2 direction)
    {
        base.Init(damage, speed, direction);
        startPosition = transform.position;

        InitializeComponents();
        SetupRigidbody();
        FindPlayer();

        // Kinematic 모드에서 초기 속도 설정
        currentSpeed = speed;
        currentDirection = direction;

            }

    // 컴포넌트 초기화 분리
    private void InitializeComponents()
    {
        // Rigidbody 설정
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // Collider를 Trigger로 설정
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    // Rigidbody 설정 분리
    private void SetupRigidbody()
    {
        // Rigidbody 설정 - 원래대로 Kinematic으로 복원
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;  // 중력은 사용하지 않음
            // Kinematic 모드에서는 velocity를 직접 사용하지 않고 Transform으로 이동
        }
    }

    // 플레이어 찾기 분리
    private void FindPlayer()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    protected override void Update()
    {
        // 수명 체크
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            DeactivateProjectile();
            return;
        }

        if (isReturning)
        {
            // 돌아오는 처리
            ReturnToPlayer();
        }
        else
        {
            // 발사 단계 - Kinematic 이동
            MoveForward();

            // 최대 거리 체크
            if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
            {
                isReturning = true;
            }
        }
    }

    // 전진 이동 (Kinematic 모드)
    private void MoveForward()
    {
        transform.position += (Vector3)currentDirection * currentSpeed * Time.deltaTime;

        // 방향 전환
        if (currentDirection != Vector2.zero)
        {
            transform.right = currentDirection;
        }
    }

    // 플레이어에게 돌아오기
    void ReturnToPlayer()
    {
        if (!ValidatePlayerReference()) return;

        Vector3 direction = CalculateReturnDirection();
        MoveTowardsPlayer(direction);
        UpdateRotation(direction);
        CheckPlayerProximity();
    }

    // 플레이어 참조 유효성 검사
    private bool ValidatePlayerReference()
    {
        if (playerTransform == null)
        {
            // 플레이어 재찾기
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    // 복귀 방향 계산
    private Vector3 CalculateReturnDirection()
    {
        return (playerTransform.position - transform.position).normalized;
    }

    // 플레이어 방향으로 이동
    private void MoveTowardsPlayer(Vector3 direction)
    {
        // Kinematic 모드에서는 Transform으로 직접 이동
        transform.position += direction * returnSpeed * Time.deltaTime;
    }

    // 회전 업데이트
    private void UpdateRotation(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            transform.right = direction;
        }
    }

    // 플레이어 근접 체크
    private void CheckPlayerProximity()
    {
        if (Vector3.Distance(transform.position, playerTransform.position) < pickupRange)
        {
            DeactivateProjectile();
        }
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어 충돌
        if (collision.CompareTag("Player") && isReturning)
        {
            DeactivateProjectile();
            return;
        }
        
        // 적 충돌 - 데미지
        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(Damage);
            }
        }
    }

    // 투사체 비활성화
    private void DeactivateProjectile()
    {
        // 상태 초기화
        isReturning = false;
        currentSpeed = 0;
        currentDirection = Vector2.zero;
        hasDeactivated = false;
        
        // Rigidbody 초기화
        if (rb != null) {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        if (hasDeactivated) return;

        hasDeactivated = true;

        // 오브젝트 풀로 반환
        ObjectPoolManager.Instance.ReturnBoomerang(this);
    }

    protected override void ReturnToPool()
    {
        DeactivateProjectile();
    }
}