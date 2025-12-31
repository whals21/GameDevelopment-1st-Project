using UnityEngine;
using System.Collections;

/// <summary>
/// 특정 위치에 화염 지대를 생성하여 범위 내 적에게 지속 데미지를 입히는 이펙트
/// </summary>
public class FireGround : MonoBehaviour
{
    #region Serialized Fields
    [Header("Settings - 설정")]
    [SerializeField] private float _defaultRadius = 2f;
    [SerializeField] private float _damageInterval = 0.5f;

    [Header("Visuals - 시각 효과")]
    [SerializeField] private ParticleSystem _fireParticles;
    #endregion

    #region Private Fields
    // 데이터 (외부에서 주입)
    private float _damage;
    private float _duration;
    private float _radius;
    private int _enemyLayerMask;
    private SkillBase _source;  // 통계 기록용 source

    // 상태
    private Coroutine _lifeCycleCoroutine;

    // 컴포넌트
    private CircleCollider2D _collider;
    private SpriteRenderer _spriteRenderer;
    #endregion

    #region Properties
    public bool IsActive => _lifeCycleCoroutine != null;
    #endregion

    #region Initialization
    private void Awake()
    {
        // 컴포넌트 캐싱
        _collider = GetComponent<CircleCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // CircleCollider2D 설정
        if (_collider == null)
        {
            _collider = gameObject.AddComponent<CircleCollider2D>();
        }
        _collider.isTrigger = true;

        // SpriteRenderer 자동 추가 (선택적)
        if (_spriteRenderer == null)
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sortingLayerName = "Effects";
            _spriteRenderer.sortingOrder = 100;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 비활성화 시 자동 리셋 (캡슐화 - 전문가 피드백)
    /// ObjectPool.Return()에서 SetActive(false) 호출 시 자동으로 실행됨
    /// </summary>
    private void OnDisable()
    {
        ResetForReuse();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 화염 지대 초기화
    /// </summary>
    public void Init(float damage, float duration, float radiusMultiplier = 1f, SkillBase source = null)
    {
        _damage = damage;
        _duration = duration;
        _radius = _defaultRadius * radiusMultiplier;
        _source = source;  // 통계 기록용 source 저장

        // 콜라이더 반경 설정
        if (_collider != null)
        {
            _collider.radius = _radius;
        }

        // 시각적 스케일 설정
        if (_spriteRenderer != null)
        {
            _spriteRenderer.transform.localScale = Vector3.one * (_radius * 2f);
        }

        // 파티클 재생
        if (_fireParticles != null)
        {
            _fireParticles.Play();
        }

        gameObject.SetActive(true);

        // 생명주기 코루틴 시작
        if (_lifeCycleCoroutine != null)
        {
            StopCoroutine(_lifeCycleCoroutine);
        }
        _lifeCycleCoroutine = StartCoroutine(LifeCycle());
    }

    /// <summary>
    /// 위치 설정 (풀링 재사용 시)
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    /// <summary>
    /// Enemy LayerMask 설정 (선택적)
    /// </summary>
    public void SetEnemyLayerMask(int layerMask)
    {
        _enemyLayerMask = layerMask;
    }
    #endregion

    #region Life Cycle
    /// <summary>
    /// 생명주기 코루틴 (Invoke 대체)
    /// 전문가 피드백: 하나의 코루틴으로 모든 생명주기 관리
    /// </summary>
    private IEnumerator LifeCycle()
    {
        float timer = 0f;

        // 지속 시간 동안 주기적 데미지 처리
        while (timer < _duration)
        {
            DealDamage();
            yield return new WaitForSeconds(_damageInterval);
            timer += _damageInterval;
        }

        // 시간 종료 후 정리 및 풀 반환
        Deactivate();
    }

    /// <summary>
    /// AOE 데미지 처리
    /// </summary>
    private void DealDamage()
    {
        // LayerMask가 설정되지 않았으면 자동 감지
        if (_enemyLayerMask == 0)
        {
            _enemyLayerMask = LayerMask.GetMask("Enemy");
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, _radius, _enemyLayerMask);

        foreach (Collider2D enemyCol in hitEnemies)
        {
            if (enemyCol.TryGetComponent<Enemy>(out Enemy enemy))
            {
                enemy.TakeDamage(_damage, _source);  // source 전달로 통계 기록
            }
        }
    }
    #endregion

    #region Cleanup
    /// <summary>
    /// 비활성화 및 풀 반환
    /// </summary>
    private void Deactivate()
    {
        // 파티클 정지
        if (_fireParticles != null)
        {
            _fireParticles.Stop();
        }

        _lifeCycleCoroutine = null;

        // 풀에 반환
        ReturnToPool();
    }

    /// <summary>
    /// 풀 반납
    /// </summary>
    private void ReturnToPool()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnFireGround(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 풀링 재사용을 위한 리셋
    /// </summary>
    public void ResetForReuse()
    {
        if (_lifeCycleCoroutine != null)
        {
            StopCoroutine(_lifeCycleCoroutine);
            _lifeCycleCoroutine = null;
        }

        _damage = 0f;
        _duration = 0f;
        _radius = 0f;
        _source = null;  // source 리셋

        if (_fireParticles != null)
        {
            _fireParticles.Stop();
        }
    }
    #endregion

    #region Debug
    /// <summary>
    /// 화염 지대 범위 표시 (Gizmos)
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
        float displayRadius = _radius > 0 ? _radius : _defaultRadius;
        Gizmos.DrawWireSphere(transform.position, displayRadius);

        // 내부 채움
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, displayRadius);
    }
    #endregion
}
