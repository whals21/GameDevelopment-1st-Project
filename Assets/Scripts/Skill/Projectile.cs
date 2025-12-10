using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("총알 설정")]
    [SerializeField] private float lifeTime = 3f;

    // 런타임 데이터 (Init에서 설정)
    protected float damage;
    protected float speed;
    protected Vector3 direction;
    protected float lifeTimer;

    // 프로퍼티 (자식 클래스에서 접근용)
    public float Damage => damage;

    protected Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    // 오브젝트 풀에서 가져올 때 호출 (필수!)
    public void Init(float damage, float speed, Vector3 direction)
    {
        this.damage = damage;
        this.speed = speed;
        this.direction = direction.normalized;
        this.lifeTimer = 0f;  // 타이머 리셋!

        // 회전 설정
        transform.right = direction;
    }

    protected virtual void Update()
    {
        // 수명 체크
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            // 풀로 반환
            ReturnToPool();
        }
    }

    void FixedUpdate()
    {
        // 물리 기반 이동
        Vector2 nextPos = rb.position + (Vector2)direction * speed * Time.fixedDeltaTime;
        rb.MovePosition(nextPos);
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            // 데미지 처리
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // 풀로 반환
            ReturnToPool();
        }
    }

    protected virtual void ReturnToPool()
    {
        ObjectPoolManager.Instance.ReturnProjectile(this);
    }
}
