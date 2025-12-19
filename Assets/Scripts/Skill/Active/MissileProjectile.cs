using UnityEngine;

public class MissileProjectile : Projectile
{
    [Header("미사일 설정")]
    [SerializeField] private GameObject explosionEffect;

    private float currentLifeTime;
    private bool hasDeactivated = false;
    private Vector2 moveDirection;  // 미사일 이동 방향 저장

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
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    protected override void Update()
    {
        if (hasDeactivated) return;

        // 수명 체크
        currentLifeTime += Time.deltaTime;
        if (currentLifeTime >= lifeTime)
        {
            DeactivateProjectile();
            return;
        }

        // 이동
        MoveProjectile();

        // 화면 밖 체크
        CheckOffscreen();
    }

    // 부모의 FixedUpdate를 오버라이드하여 사용하지 않음
    protected override void FixedUpdate()
    {
        // MissileProjectile는 FixedUpdate를 사용하지 않고 Update에서 직접 이동 관리
        // 부모의 FixedUpdate를 호출하지 않음
    }

    private void MoveProjectile()
    {
        // 전진 이동
        transform.Translate(transform.right * speed * Time.deltaTime);

        // 미사일이 이동 방향으로 회전
        //UpdateMissileRotation();
    }

    private void UpdateMissileRotation()
    {
        // 이동 방향으로 회전 각도 계산
        if (moveDirection.magnitude > 0.01f)
        {
            // 방향 벡터를 각도로 변환 (Mathf.Atan2는 y, x 순서)
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;

            // 미사일 스프라이트가 위를 향하도록 90도 보정
            transform.rotation = Quaternion.Euler(0, 0, angle + 90f);
        }
    }

    private void CheckOffscreen()
    {
        Vector3 screenPos = Camera.main.WorldToViewportPoint(transform.position);
        if (screenPos.x < -0.1f || screenPos.x > 1.1f ||
            screenPos.y < -0.1f || screenPos.y > 1.1f)
        {
            DeactivateProjectile();
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDeactivated) return;

        // 적 충돌
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // 폭발 이펙트 생성
            CreateExplosionEffect();

            DeactivateProjectile();
        }

        // 지형 충돌
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            CreateExplosionEffect();
            DeactivateProjectile();
        }
    }

    private void CreateExplosionEffect()
    {
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    private void DeactivateProjectile()
    {
        if (hasDeactivated) return;

        hasDeactivated = true;
        gameObject.SetActive(false);

        // 오브젝트 풀로 반환
        ObjectPoolManager.Instance.ReturnMissile(this);
    }

    // 오브젝트 풀에서 재사용될 때 호출할 초기화 메서드
    public void ResetForReuse()
    {
        hasDeactivated = false;
        currentLifeTime = 0f;
        moveDirection = Vector2.zero;  // 이동 방향 초기화

        // 필요한 경우 다른 상태도 초기화
        // Rigidbody2D 속도 리셋
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    // 미사일 방향 설정 메서드
    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;
        transform.right = moveDirection;
    }
}