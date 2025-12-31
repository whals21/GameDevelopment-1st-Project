using UnityEngine;

public class BossBullet : MonoBehaviour
{
    private BossFirePool pool;
    private BossController boss;

    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody2D rb;

    private Vector2 targetPos;   // 목표 좌표 (패턴002용)
    private bool useTarget = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // 풀에서 생성될 때 한 번만 호출
    public void Initialize(BossFirePool pool, BossController boss)
    {
        this.pool = pool;
        this.boss = boss;
    }

    // -----------------------------
    // 1. 그냥 방향으로 쭉 가는 발사체 (패턴 일반)
    // -----------------------------
    public void FireNormal(Vector2 dir)
    {
        rb.velocity = dir.normalized * speed;
        useTarget = false; // 목표 체크 사용 안 함
        CancelInvoke();
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    // -----------------------------
    // 2. 목표 좌표까지 이동 후 반환 (패턴002용)
    // -----------------------------
    public void FireTargeted(Vector2 dir, Vector2 target)
    {
        rb.velocity = dir.normalized * speed;
        targetPos = target;
        useTarget = true;
        CancelInvoke();
        Invoke(nameof(ReturnToPool), lifeTime); // 안전망
    }

    // -----------------------------
    // 3. 제자리에서 깜빡임 후 반환 (패턴003용)
    // -----------------------------
    public void FireBlink(float duration)
    {
        rb.velocity = Vector2.zero; // 제자리 유지
        useTarget = false;
        CancelInvoke();
        Invoke(nameof(ReturnToPool), duration); // duration 후 자동 반환
    }

    private void FixedUpdate()
    {
        // 목표 좌표에 도달 시 반환 (패턴002)
        if (useTarget && Vector2.Distance(rb.position, targetPos) < 0.1f)
        {
            ReturnToPool();
            useTarget = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerStats player = collision.GetComponent<PlayerStats>();
            if (player != null)
            {
                player.TakeDamage(boss.CurrentDamage); // 실제 데미지 적용
            }

            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        rb.velocity = Vector2.zero;
        useTarget = false;
        pool.ReturnBullet(gameObject);
    }
}
