using UnityEngine;

public class MolotovProjectile : Projectile
{
    [Header("화염병 설정")]
    [SerializeField] private float arcHeight = 3f;
    [SerializeField] private float groundCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float travelTime;
    private float elapsedTime;
    private bool hasHitGround = false;

    // 발사체 초기화 (목표 위치 지정)
    public void InitMolotov(float damage, Vector3 targetPos)
    {
        // 콜라이더를 트리거로 설정하여 물리 충돌 방지
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Rigidbody 설정 확인
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }

        Init(damage, 5f, (targetPos - transform.position).normalized);

        startPosition = transform.position;
        targetPosition = targetPos;

        // 포물선 비행 시간 계산
        float horizontalDistance = Vector2.Distance(new Vector2(startPosition.x, startPosition.y),
                                                   new Vector2(targetPos.x, targetPos.y));
        travelTime = Mathf.Max(0.5f, horizontalDistance / 5f); // 최소 0.5초 비행, 5는 수평 속도

        elapsedTime = 0f;
        hasHitGround = false;
    }

    protected override void Update()
    {
        if (hasHitGround)
        {
            // 지면에 도달했으면 아무것도 하지 않음 (튕김 방지)
            return;
        }

        // 포물선 이동
        elapsedTime += Time.deltaTime;

        if (elapsedTime >= travelTime)
        {
            // 목표 지점 도달
            transform.position = targetPosition;
            OnHitGround();
        }
        else
        {
            // 포물선 보간
            float t = elapsedTime / travelTime;

            // 수평 이동
            Vector3 horizontalPos = Vector3.Lerp(startPosition, targetPosition, t);

            // 수직 이동 (포물선)
            float arc = arcHeight * 4f * t * (1f - t);
            horizontalPos.y = Mathf.Lerp(startPosition.y, targetPosition.y, t) + arc;

            transform.position = horizontalPos;

            // 회전
            Vector3 direction = (horizontalPos - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.right = direction;
            }
        }

        // 수명 체크
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= LifeTime)
        {
            ReturnToPool();
        }
    }

    // 지면 충돌 처리
    private void OnHitGround()
    {
        if (hasHitGround) return;

        hasHitGround = true;

        // 화염 지대 생성
        CreateFireGround();

        // 즉시 비활성화 (튕김 방지)
        gameObject.SetActive(false);

        // 0.1초 후 풀로 반환 (오브젝트 풀링을 위한 최소 지연)
        Invoke(nameof(ReturnToPool), 0.1f);
    }

    // 화염 지대 생성
    private void CreateFireGround()
    {
        if (ObjectPoolManager.Instance != null)
        {
            FireGround fireGround = ObjectPoolManager.Instance.GetFireGround();
            if (fireGround != null)
            {
                fireGround.transform.position = transform.position;
                fireGround.Init(damage * 0.5f, 5f); // 지속 데미지는 50%
            }
        }
    }

    protected override void ReturnToPool()
    {
        CancelInvoke(); // Invoke 취소
        ObjectPoolManager.Instance.ReturnMolotov(this);
    }
}