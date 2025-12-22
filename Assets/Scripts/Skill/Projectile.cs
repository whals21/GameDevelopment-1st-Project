using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("총알 기본 설정")]
    [SerializeField] protected float lifeTime = 3f;

    [Header("레이어 설정")]
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected LayerMask obstacleLayer;

    [Header("레벨별 스탯 설정 (Inspector에서 조정 가능)")]
    [SerializeField] protected float[] damageLevels = {10f, 15f, 20f, 25f, 30f};
    [SerializeField] protected float[] speedLevels = {5f, 6f, 7f, 8f, 9f};
    [SerializeField] protected float[] sizeLevels = {1f, 1.2f, 1.4f, 1.6f, 1.8f};

    // 자식 클래스에서 재정의 가능한 수명 속성
    protected virtual float LifeTime => lifeTime;

    // 런타임 데이터
    protected float currentDamage;
    protected float currentSpeed;
    protected float currentSize;
    protected Vector3 direction;
    protected float lifeTimer;
    protected int currentLevel = 1;

    // 프로퍼티
    public float Damage => currentDamage;
    public float Speed { get => currentSpeed; protected set => currentSpeed = value; }
    public int Level => currentLevel;

    protected Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

      // 레벨 설정 메서드
    public virtual void SetLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, damageLevels.Length);
        UpdateStats();
    }

    // 스탯 업데이트
    protected virtual void UpdateStats()
    {
        int levelIndex = currentLevel - 1;
        currentDamage = damageLevels[levelIndex];
        currentSpeed = speedLevels[levelIndex];
        currentSize = sizeLevels[levelIndex];

        // 크기 적용
        transform.localScale = Vector3.one * currentSize;
    }

    // 오브젝트 풀에서 가져올 때 호출 (필수!)
    public void Init(float damage, float speed, Vector3 direction)
    {
        // 레벨 1 기본값 설정 (호환성 유지)
        SetLevel(1);

        // 개별 값 설정 (레벨 시스템과 호환)
        if (damage > 0) currentDamage = damage;
        if (speed > 0) currentSpeed = speed;

        this.direction = direction.normalized;
        this.lifeTimer = 0f;  // 타이머 리셋!

        // 회전 설정
        transform.right = direction;
    }

    // 레벨별 Init 메서드 (권장)
    public void Init(int level, Vector3 direction)
    {
        SetLevel(level);
        this.direction = direction.normalized;
        this.lifeTimer = 0f;

        // 회전 설정
        transform.right = direction;
    }

    protected virtual void Update()
    {
        // 수명 체크
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= LifeTime)  // 가상 속성 사용
        {
            // 풀로 반환
            ReturnToPool();
        }
    }

    protected virtual void FixedUpdate()
    {
        // 물리 기반 이동
        Vector2 nextPos = rb.position + (Vector2)direction * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(nextPos);
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        // 레이어 마스크로 적 확인
        if ((enemyLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            // 데미지 처리
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(currentDamage);
            }

            // 풀로 반환
            ReturnToPool();
        }
        // 장애물 충돌 시에도 풀로 반환
        else if ((obstacleLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            ReturnToPool();
        }
    }

    protected virtual void ReturnToPool()
    {
        ObjectPoolManager.Instance.ReturnProjectile(this);
    }
}
