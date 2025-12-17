using UnityEngine;

public class Drone : MonoBehaviour
{
    [Header("드론 설정")]
    [SerializeField] private float baseFollowSpeed = 5f;
    [SerializeField] private float followDistance = 2f;
    [SerializeField] private Vector3 followOffset = Vector3.up;

    // 상태 변수
    private Transform playerTransform;
    private Vector3 targetPosition;
    private float followSpeed;
    private bool isActive = false;

    // 스탯
    private float damage;
    private float speed;
    private int missileCount;
    private float cooldown;

    // 비행 관련
    private Vector3 currentVelocity;
    private float hoverTime = 0f;
    private Vector3 hoverOffset;

    private void Awake()
    {
        SetupComponents();
    }

    private void SetupComponents()
    {
        // 필요한 컴포넌트 설정
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
        }
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    public void Initialize(Transform player, float distance, float speed)
    {
        playerTransform = player;
        followDistance = distance;
        followSpeed = speed;
        followOffset = Vector3.up * distance;

        // 초기 위치 설정
        UpdateTargetPosition();
        transform.position = targetPosition;

        // 호버링 초기화
        hoverTime = Random.Range(0f, Mathf.PI * 2f);
    }

    public void SetStats(float dmg, float spd, int missileCnt, float cd)
    {
        damage = dmg;
        speed = spd;
        missileCount = missileCnt;
        cooldown = cd;
    }

    public void Activate()
    {
        isActive = true;
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        isActive = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isActive || playerTransform == null) return;

        // 목표 위치 업데이트
        UpdateTargetPosition();

        // 부드러운 이동
        MoveToTarget();

        // 호버링 효과
        UpdateHover();

        // 플레이어 방향으로 회전
        RotateToPlayer();
    }

    private void UpdateTargetPosition()
    {
        // 플레이어 기준으로 고정된 위치 계산
        Vector3 basePosition = playerTransform.position + followOffset;

        // 거리 제한: 드론이 followDistance 이상 멀어지지 않도록 함
        Vector3 directionFromPlayer = basePosition - playerTransform.position;
        if (directionFromPlayer.magnitude > followDistance)
        {
            // 최대 거리로 제한
            directionFromPlayer = directionFromPlayer.normalized * followDistance;
            targetPosition = playerTransform.position + directionFromPlayer;
        }
        else
        {
            targetPosition = basePosition;
        }
    }

    private void MoveToTarget()
    {
        // 부드러운 추적 이동
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance > 0.1f)
        {
            Vector3 newPosition = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                followSpeed * Time.deltaTime
            );
            transform.position = newPosition + hoverOffset;
        }
        else
        {
            transform.position = targetPosition + hoverOffset;
        }
    }

    private void UpdateHover()
    {
        // 작은 호버링 움직임 (범위 축소)
        hoverTime += Time.deltaTime * 2f;
        hoverOffset = new Vector3(
            Mathf.Sin(hoverTime) * 0.05f,  // 0.1f → 0.05f로 축소
            Mathf.Cos(hoverTime * 1.5f) * 0.05f,  // 0.1f → 0.05f로 축소
            0
        );
    }

    private void RotateToPlayer()
    {
        // 플레이어 방향으로 드론 회전
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + 90f);
    }

    // 충돌 처리 (필요 시)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

        // 적 투사체 방어 또는 기타 충돌 처리
        if (other.CompareTag("EnemyProjectile"))
        {
            // 투사체 파괴
            Destroy(other.gameObject);
        }
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public bool IsActive()
    {
        return isActive;
    }
}