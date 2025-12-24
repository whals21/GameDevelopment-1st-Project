using UnityEngine;

public class ExpGem : MonoBehaviour
{
    [Header("경험치 설정")]
    [SerializeField] private int expValue = 10;
    [SerializeField] private float magnetSpeed = 10f;
    [SerializeField] private float magnetItemBoostSpeed = 40f;  // Magnet 아이템 획득 시 속도
    [SerializeField] private float baseMagnetRange = 3f;  // 기본 마그넷 범위

    private Rigidbody2D rb;
    private Transform playerTransform;
    private PlayerStats playerStats;
    private bool isBeingAttracted = false;
    private bool forceMagnetized = false;  // Magnet 아이템으로 강제 활성화된 상태

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 플레이어 참조 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerStats = player.GetComponent<PlayerStats>();
        }
    }

    // 오브젝트 풀링용 Init() 메서드
    public void Init(int expValue, Vector3 spawnPosition)
    {
        this.expValue = expValue;
        transform.position = spawnPosition;
        isBeingAttracted = false;
        forceMagnetized = false;  // 강제 자석 모드 초기화

        // 플레이어 참조가 없으면 다시 찾기
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerStats = player.GetComponent<PlayerStats>();
            }
        }
    }

    // 현재 유효한 마그넷 범위 가져오기 (PlayerStats의 값 사용)
    private float GetCurrentMagnetRange()
    {
        if (playerStats != null)
        {
            return playerStats.MagnetRange;
        }
        return baseMagnetRange;
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 강제 자석 모드가 활성화된 경우는 범위 체크 건너뜀
        if (forceMagnetized) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        float currentMagnetRange = GetCurrentMagnetRange();

        // 플레이어가 자석 범위 안에 들어오면 끌어당기기 시작
        if (distance <= currentMagnetRange)
        {
            isBeingAttracted = true;
        }
        // 범위를 벗어나면 더 이상 끌어당기지 않음
        else if (isBeingAttracted && distance > currentMagnetRange * 1.5f)
        {
            isBeingAttracted = false;
        }
    }

    // 자석 효과로 이동
    void FixedUpdate()
    {
        if (isBeingAttracted && playerTransform != null)
        {
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            // Magnet 아이템으로 활성화된 경우 더 빠른 속도 사용
            float currentSpeed = forceMagnetized ? magnetItemBoostSpeed : magnetSpeed;
            rb.MovePosition(rb.position + direction * currentSpeed * Time.fixedDeltaTime);
        }
    }

    // 플레이어와 충돌 처리
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GiveExpToPlayer();
            ReturnToPool();
        }
    }

    // 경험치 지급
    void GiveExpToPlayer()
    {
        if (playerStats != null)
        {
            playerStats.GainExp(expValue);
        }
    }

    // 오브젝트 풀로 반환
    private void ReturnToPool()
    {
        ObjectPoolManager.Instance.ReturnExpGem(this);
    }

    // 강제로 자석 모드 활성화 (Magnet 아이템 획득 시 호출)
    public void Magnetize()
    {
        isBeingAttracted = true;
        forceMagnetized = true;  // 강제 모드 플래그 설정
    }
}
