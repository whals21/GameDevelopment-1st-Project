using UnityEngine;
using System.Collections;

public class SoccerBallProjectile : Projectile
{
    [Header("축구공 특성")]
    [SerializeField] private int maxBounces = 5;
    [SerializeField] private float bounceSpeed = 15f;
    [SerializeField] private float spawnRadius = 3f;
    [SerializeField] private float outOfBoundsMargin = 0.2f;
    [SerializeField] private int maxRelaunchCount = 3;
    [SerializeField] private float minBounceInterval = 0.1f;
    [SerializeField] private LayerMask enemyLayer;

    // 상수 정의
    private const float OUT_OF_BOUNDS_CHECK_INTERVAL = 0.1f;
    private const float RANDOM_ANGLE_RANGE = 15f;
    private const float BOUNCE_SPEED_DECAY = 0.9f;

    // 변수
    private int currentBounces = 0;
    private int currentRelaunchCount = 0;
    private Vector3 currentDirection;
    private float currentSpeed;
    private Camera mainCamera;
    private Transform playerTransform;
    private bool isRecalculating = false;
    private float lastBounceTime = 0f;
    private bool hasDeactivated = false;

    protected void Awake()
    {
        // 컴포넌트 캐싱 - 부모의 rb 필드 사용
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = GetComponentInParent<Rigidbody2D>();

        // 메인 카메라 캐싱
        mainCamera = Camera.main;
        if (mainCamera == null) mainCamera = FindObjectOfType<Camera>();

        // Collider를 Trigger로 설정
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    // 축구공 초기화
    public void InitSoccerBall(float damage, float speed, Vector3 direction)
    {
        // 부모 Init 호출
        Init(damage, speed, direction);

        // 축구공 특수 초기화 - speed가 0이면 기본값 사용
        currentDirection = direction.normalized;
        currentSpeed = speed > 0 ? speed : bounceSpeed; // speed가 0이면 기본 bounceSpeed 사용
        currentBounces = 0;
        currentRelaunchCount = 0;
        isRecalculating = false;
        lastBounceTime = 0f;
        hasDeactivated = false;

        // 플레이어 찾기
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // Rigidbody 설정
        SetupRigidbody();

        // 이전 코루틴 정리 - 풀링에서 재사용될 때 남아있는 코루틴 방지
        StopAllCoroutines();

        // SpriteRenderer 알파 강제 초기화 - 풀링 문제 방지
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            spriteRenderer.color = new Color(color.r, color.g, color.b, 1f);
        }

        // 초기 회전 설정
        if (currentDirection != Vector3.zero)
        {
            transform.right = currentDirection;
        }

            }

    // Rigidbody 설정
    private void SetupRigidbody()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    protected override void Update()
    {
        if (hasDeactivated || isRecalculating)
        {
            return;
        }

        // 수명 체크
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= LifeTime)
        {
            DeactivateProjectile();
            return;
        }

        // 화면 밖 체크 (주기적으로)
        if (Time.time % OUT_OF_BOUNDS_CHECK_INTERVAL < Time.deltaTime)
        {
            CheckOutOfBounds();
        }

        // 이동 처리
        HandleMovement();
    }

    // FixedUpdate 오버라이드 - 부모의 Rigidbody 이동 방지
    protected override void FixedUpdate()
    {
        // 아무 작업도 하지 않음 - Update()에서 Transform 직접 제어
    }

    // 이동 처리
    private void HandleMovement()
    {
        if (currentSpeed <= 0f)
        {
            return;
        }

        transform.position += currentDirection * currentSpeed * Time.deltaTime;

        // 회전 효과
        if (currentDirection != Vector3.zero)
        {
            transform.right = currentDirection;
            transform.Rotate(0f, 0f, currentSpeed * Time.deltaTime * 50f);
        }
    }

    // 화면 밖 체크
    private void CheckOutOfBounds()
    {
        if (mainCamera == null) return;

        // 초기화 후 0.5초 동안은 화면 밖 체크 안함
        if (lifeTimer < 0.5f) return;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);

        bool outOfBounds = viewportPos.x < -outOfBoundsMargin ||
                          viewportPos.x > 1f + outOfBoundsMargin ||
                          viewportPos.y < -outOfBoundsMargin ||
                          viewportPos.y > 1f + outOfBoundsMargin;

        if (outOfBounds)
        {
            StartCoroutine(RecalculateLaunch());
        }
    }

    // 재발사 처리
    private IEnumerator RecalculateLaunch()
    {
        if (currentRelaunchCount >= maxRelaunchCount)
        {
            DeactivateProjectile();
            yield break;
        }

        isRecalculating = true;
        currentSpeed = 0f;

        // 시각적 효과 (선택적)
        CreateRecalculateEffect();

        yield return new WaitForSeconds(0.1f);

        // 새로운 발사 계산
        PerformRelaunch();

        isRecalculating = false;
    }

    // 재발사 실행
    private void PerformRelaunch()
    {
        currentRelaunchCount++;

        // 플레이어 주변 랜덤 위치로 이동
        if (playerTransform != null)
        {
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            transform.position = playerTransform.position + (Vector3)randomOffset;
        }

        // 새로운 방향 계산
        GameObject nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            Vector3 targetDirection = (nearestEnemy.transform.position - transform.position).normalized;
            currentDirection = targetDirection;
        }
        else
        {
            // 적이 없으면 랜덤 방향
            float randomAngle = Random.Range(0f, 360f);
            currentDirection = Quaternion.Euler(0, 0, randomAngle) * Vector3.right;
        }

        // 속도 설정 (재발사할 때마다 약간 감소)
        currentSpeed = bounceSpeed * Mathf.Pow(BOUNCE_SPEED_DECAY, currentRelaunchCount - 1);

        // 튕김 횟수 초기화
        currentBounces = 0;
    }

    // 가장 가까운 적 찾기
    private GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearest = null;
        float minDistance = float.MaxValue;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    // 튕김 방향 계산
    private Vector3 CalculateBounceDirection(Vector3 incomingDirection, Vector3 surfaceNormal)
    {
        // 입사각 = 반사각 공식
        Vector3 reflected = Vector3.Reflect(incomingDirection, surfaceNormal);

        // 약간의 랜덤성 추가
        float randomAngle = Random.Range(-RANDOM_ANGLE_RANGE, RANDOM_ANGLE_RANGE);
        Quaternion randomRotation = Quaternion.Euler(0, 0, randomAngle);

        return randomRotation * reflected;
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        // 튕김 간격 체크
        if (Time.time - lastBounceTime < minBounceInterval) return;

        // 적 충돌 처리
        if (collision.CompareTag("Enemy"))
        {
            HandleEnemyCollision(collision);
            return;
        }

        // 플레이어 충돌 (무시)
        if (collision.CompareTag("Player"))
        {
            return;
        }

        // 환경 충돌 (벽, 장애물 등)
        HandleEnvironmentCollision(collision);
    }

    // 적 충돌 처리
    private void HandleEnemyCollision(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(Damage);
            currentBounces++;
            lastBounceTime = Time.time;

            // 튕김 방향 계산
            Vector3 collisionNormal = (transform.position - collision.transform.position).normalized;
            currentDirection = CalculateBounceDirection(currentDirection, collisionNormal);

            // 최대 튕김 횟수 체크
            if (currentBounces >= maxBounces)
            {
                DeactivateProjectile();
            }
        }
    }

    // 환경 충돌 처리
    private void HandleEnvironmentCollision(Collider2D collision)
    {
        currentBounces++;
        lastBounceTime = Time.time;

        // 충돌 지점의 법선 벡터 계산 (단순화)
        Vector3 collisionNormal = (transform.position - collision.transform.position).normalized;
        if (collisionNormal == Vector3.zero)
        {
            collisionNormal = -currentDirection;
        }

        currentDirection = CalculateBounceDirection(currentDirection, collisionNormal);

        // 최대 튕김 횟수 체크
        if (currentBounces >= maxBounces)
        {
            DeactivateProjectile();
        }
    }

    // 재발사 시각적 효과
    private void CreateRecalculateEffect()
    {
        // 이미 비활성화된 오브젝트에서는 실행하지 않음
        if (hasDeactivated) return;

        // 파티클 효과 생성
        if (Resources.Load("Effects/TeleportEffect") != null)
        {
            GameObject effect = Instantiate(Resources.Load("Effects/TeleportEffect") as GameObject);
            effect.transform.position = transform.position;
            Destroy(effect, 1f);
        }

        // 간단한 시각적 효과 (스프라이트 투명도 변화)
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && !hasDeactivated)
        {
            StartCoroutine(FlashEffect(spriteRenderer));
        }
    }

    // 깜빡임 효과
    private IEnumerator FlashEffect(SpriteRenderer renderer)
    {
        // 시작 시 상태 체크
        if (hasDeactivated || renderer == null) yield break;

        Color originalColor = renderer.color;

        for (int i = 0; i < 3; i++)
        {
            // 루프 시작마다 상태 체크
            if (hasDeactivated || renderer == null) break;

            renderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f);
            yield return new WaitForSeconds(0.1f);

            // 대기 후 상태 체크
            if (hasDeactivated || renderer == null) break;

            renderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }

        // 최종적으로 원본 색상으로 복구 보장
        if (renderer != null && !hasDeactivated)
        {
            renderer.color = originalColor;
        }
    }

    // 투사체 비활성화
    private void DeactivateProjectile()
    {
        if (hasDeactivated) return;
        hasDeactivated = true;

        // 모든 코루틴 중지 - FlashEffect가 중간에 멈출 경우 대비
        StopAllCoroutines();

        // SpriteRenderer 알파 강제 복구 - 반투명 상태 방지
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            spriteRenderer.color = new Color(color.r, color.g, color.b, 1f);
        }

        // Rigidbody 초기화
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 오브젝트 풀로 반환
        ObjectPoolManager.Instance.ReturnSoccerBall(this);
    }

    protected override void ReturnToPool()
    {
        DeactivateProjectile();
    }
}