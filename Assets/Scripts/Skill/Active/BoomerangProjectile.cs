using UnityEngine;

public class BoomerangProjectile : Projectile
{
    [Header("부메랑 설정")]
    [SerializeField] private float returnSpeed = 20f;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float pickupRange = 0.5f;
    [SerializeField] private float lifeTime = 10f;

    private Vector3 startPosition;
    private bool isReturning = false;
    private Transform playerTransform;

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
    public new void Init(float damage, float speed, Vector2 direction)
    {
        base.Init(damage, speed, direction);
        startPosition = transform.position;
        rb.velocity = direction * speed;
    }

    protected override void Update()
    {
        // 수명 체크
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            ReturnToPool();
            return;
        }

        // 최대 거리 체크
        if (!isReturning && Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            isReturning = true;
        }

        // 돌아오는 처리
        if (isReturning)
        {
            ReturnToPlayer();
        }
    }

    // 플레이어에게 돌아오기
    void ReturnToPlayer()
    {
        if (playerTransform == null)
        {
            // 플레이어 재찾기
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            else return;
        }

        // Transform으로 직접 이동 (Trigger이므로)
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.position += direction * returnSpeed * Time.deltaTime;

        // 방향 전환
        if (direction != Vector3.zero)
        {
            transform.right = direction;
        }

        // 플레이어 도달 체크
        if (Vector3.Distance(transform.position, playerTransform.position) < pickupRange)
        {
            ReturnToPool();
        }
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        // 돌아올 때 플레이어 충돌
        if (collision.CompareTag("Player") && isReturning)
        {
            ReturnToPool();
        }
        // 나갈 때 적 충돌
        else if (collision.CompareTag("Enemy") && !isReturning)
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(Damage);
            }
        }
    }

    protected override void ReturnToPool()
    {
        // Instantiate로 생성되었으므로 Destroy
        Destroy(gameObject);
    }
}