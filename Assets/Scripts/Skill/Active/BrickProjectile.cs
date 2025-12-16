using UnityEngine;

public class BrickProjectile : Projectile
{
    [Header("벽돌 특성")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private int maxHits = 3; // 내구도 (최대 충돌 횟수)
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private LayerMask enemyLayer;

    private int currentHits = 0;
    private bool hasDeactivated = false;
    private bool hasHitGround = false;

    // 탕탕특공대 스타일 물리 변수
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private readonly float gravity = 15f;
    private bool isRising = true;
    private bool hasActivatedGravity = false;

    // 상수 정의
    private const float DEFAULT_INITIAL_SPEED = 12f;
    private const float HORIZONTAL_SPEED_RATIO = 0.3f;
    private const float VERTICAL_SPEED_RATIO = 0.9f;
    private const float MIN_GROUND_HEIGHT = 0f;
    private const float MAX_OFFSCREEN_Y = -10f;
    private const float MAX_OFFSCREEN_X = 20f;

    // 튕김 관련 상수
    private const float BOUNCE_DAMPING = 0.7f;  // 튕길 때 속도 감소율
    private const float BOUNCE_MIN_SPEED = 3f;  // 최소 튕김 속도

    protected void Awake()
    {
        // 부모의 Awake()가 private이므로 직접 호출 불가능
        // Rigidbody2D 설정을 직접 수행
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    // 벽돌 초기화
    public void InitBrick(float damage, float speed, Vector3 direction)
    {
        // 부모 Init 호출
        Init(damage, speed, direction);

        // 탕탕특공대 스타일 수직 발사 설정
        Vector3 launchDirection = direction.normalized;

        // 발사 방향에 따른 수평/수직 속도 분배
        // 기본적으로 위쪽으로 발사하지만, 방향에 따라 수평 속도도 반영
        horizontalVelocity = new Vector3(launchDirection.x * speed * 0.5f, 0, 0);
        verticalVelocity = speed * 0.8f; // 대부분의 속도는 수직 상승에 사용

        isRising = true;
        hasActivatedGravity = false;

        SetupComponents();
        ResetState();
    }

    // 컴포넌트 설정 분리
    private void SetupComponents()
    {
        // 콜라이더 설정
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // 시각적 진단 코드
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    // 상태 초기화 분리
    private void ResetState()
    {
        currentHits = 0;
        hasDeactivated = false;
        hasHitGround = false;
    }

    
    // 부모의 Update를 오버라이드하여 중력 기반 물리 로직 사용
    protected override void Update()
    {
        // 중력 기반 물리 이동
        if (isRising)
        {
            // 상승 단계: 수직 속도 감소
            verticalVelocity -= gravity * Time.deltaTime;

            // 위치 업데이트
            Vector3 movement = (horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime;
            transform.position += movement;

            // 회전 (날아가는 방향으로)
            if (movement != Vector3.zero)
            {
                transform.right = movement.normalized;
            }

            // 하강 시작 체크
            if (verticalVelocity <= 0)
            {
                isRising = false;
                hasActivatedGravity = true;
            }
        }
        else
        {
            // 하강 단계: 중력으로 가속
            verticalVelocity -= gravity * Time.deltaTime;

            // 위치 업데이트
            Vector3 movement = (horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime;
            transform.position += movement;

            // 회전 (낙하 방향으로)
            if (movement != Vector3.zero)
            {
                transform.right = movement.normalized;
            }
        }

        // 수명 체크
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= LifeTime)
        {
            DeactivateProjectile();
        }

        // 스크린 밖으로 나가면 비활성화
        if (transform.position.y < MAX_OFFSCREEN_Y || Mathf.Abs(transform.position.x) > MAX_OFFSCREEN_X)
        {
            DeactivateProjectile();
        }
    }

    // 지면 충돌 처리 - 튕김 효과로 변경
    private void OnHitGround()
    {
        // 튕김 횟수 제한이 있다면 여기서 체크
        isRising = false;
        hasActivatedGravity = true;

        // 지면 충돌 이펙트
        CreateGroundImpactEffect();

        // 수직 속도를 반사하여 튕김 효과 구현
        verticalVelocity = -verticalVelocity * BOUNCE_DAMPING;

        // 지면에 닿았을 때 약간의 수직 속도 추가로 튕어오름
        verticalVelocity = Mathf.Max(verticalVelocity, BOUNCE_MIN_SPEED);
    }

    // 부모의 OnTriggerEnter2D를 오버라이드
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDeactivated) return;

        // 적과 충돌
        if (enemyLayer == (enemyLayer | (1 << other.gameObject.layer)))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                // 데미지 적용
                enemy.TakeDamage(damage);
                currentHits++;

                
                // 넉백 효과 적용
                ApplyKnockback(enemy);

                // 충돌 횟수가 최대치에 도달하면 파괴
                if (currentHits >= maxHits)
                {
                                        DeactivateProjectile();
                }
            }
        }
    }

    // 넉백 효과 적용
    private void ApplyKnockback(Enemy enemy)
    {
        Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
        if (enemyRb != null)
        {
            // 플레이어에서 멀어지는 방향으로 넉백
            Vector3 knockbackDirection = (enemy.transform.position - transform.position).normalized;
            knockbackDirection.z = 0;

            // 즉각적인 넉백 적용
            enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

                    }
    }

    // 지면 충돌 이펙트
    private void CreateGroundImpactEffect()
    {
        // 이펙트 생성 (Particle System 등)
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // 사운드 재생 (선택적)
        // AudioSystem.PlaySound("brick_impact");
    }

    // 투사체 비활성화
    private void DeactivateProjectile()
    {
        if (hasDeactivated) return;

        hasDeactivated = true;
        hasHitGround = true; // 추가적인 업데이트 방지

        // 오브젝트 풀로 반환
        ObjectPoolManager.Instance.ReturnBrick(this);
    }

    // 벽돌 비활성화 (외부 호출용)
    public void Deactivate()
    {
        DeactivateProjectile();
    }
}