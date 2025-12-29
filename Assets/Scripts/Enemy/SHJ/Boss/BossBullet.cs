using UnityEngine;

public class BossBullet : MonoBehaviour
{
    private BossFirePool pool;
    private BossController boss;

    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody2D rb;

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

    // 발사
    public void Fire(Vector2 dir)
    {
        rb.velocity = dir.normalized * speed;
        CancelInvoke();
        Invoke(nameof(ReturnToPool), lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어 맞으면 데미지 처리
        if (collision.CompareTag("Player"))
        {
            PlayerStats player = collision.GetComponent<PlayerStats>();
            if (player != null)
            {
                player.TakeDamage(boss.CurrentDamage); // ★ 실제 데미지 받음
            }

            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        rb.velocity = Vector2.zero;
        pool.ReturnBullet(gameObject);
    }
}
