using UnityEngine;

public class ExpGem : MonoBehaviour
{
    [Header("경험치 설정")]
    [SerializeField] private int expValue = 10;
    [SerializeField] private float magnetSpeed = 10f;

    private Rigidbody2D rb;
    private Transform player;
    private float magnetRange;
    private bool isBeingAttracted = false;

    // 오브젝트 풀링용 Init() 메서드
    public void Init(int expValue, Vector3 spawnPosition, float magnetRange)
    {
        this.expValue = expValue;
        this.magnetRange = magnetRange;
        transform.position = spawnPosition;
        isBeingAttracted = false;

        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            this.player = player.transform;
        }
    }

    // 자석 효과로 이동
    private void FixedUpdate()
    {
        if (isBeingAttracted && player != null)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.MovePosition(rb.position + direction * magnetSpeed * Time.fixedDeltaTime);
        }
    }

    // 플레이어와 충돌 처리
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //GiveExpToPlayer();
            ReturnToPool();
        }
    }

    // 경험치 지급
    // private void GiveExpToPlayer()
    // {
    //     PlayerStats playerStats = collision.GetComponent<PlayerStats>();
    //     if (playerStats != null)
    //     {
    //         playerStats.GainExp(expValue);
    //     }
    // }

    // 오브젝트 풀로 반환
    private void ReturnToPool()
    {
        ObjectPoolManager.Instance.ReturnExpGem(this);
    }
}
