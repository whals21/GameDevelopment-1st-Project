using UnityEngine;

/// <summary>
/// 오라 스킬 (v2 리팩토링 - 코드리뷰 반영)
///
/// v2 변경사항:
/// - UpdateSkill 오버라이드로 연속 실행 구현 (플레이어 추적, 지속 데미지)
/// - new Material 제거, 공용 Material 사용 (GC 방지)
/// - 색상/두께 데이터 주도화
///
/// 특징:
/// - 유일하게 "지속적인(Continuous)" 속성을 가지는 스킬
/// - 쿨타임과 상관없이 매 프레임 실행되어 플레이어를 추적
/// - Forcefield 팔각형 시각 효과 (내부 + 외부 이중 오라 Lv3+)
/// </summary>
[SkillType(SkillType.Aura)]
public class AuraSkill : SkillBase
{
    #region Serialized Fields
    [Header("Visuals")]
    [Tooltip("라인 렌더러용 머티리얼 (GC 방지를 위해 할당 권장)")]
    [SerializeField] private Material lineRendererMaterial;

    [Header("Settings")]
    [Tooltip("내부 오라 두께")]
    [SerializeField] private float innerAuraWidth = 0.2f;

    [Tooltip("외부 오라 두께")]
    [SerializeField] private float outerAuraWidth = 0.15f;

    [Tooltip("기본 데미지 간격 (초) - 데이터에 없을 때 사용")]
    [SerializeField] private float baseDamageInterval = 0.5f;

    [Tooltip("외부 오라 반경 배율")]
    [SerializeField] private float outerRadiusMultiplier = 1.3f;
    #endregion

    #region Private Fields
    private Collider2D[] _hitBuffer = new Collider2D[128];
    private Transform _playerTransform;
    private int _enemyLayerMask;

    // 오라 오브젝트 (시각 효과용)
    private GameObject _auraObject;
    private LineRenderer _innerLineRenderer;
    private LineRenderer _outerLineRenderer;

    // 데미지 & 반경
    private float _auraRadius;
    private float _outerAuraRadius;
    private float _damageTimer;

    // 지속 시간 (v2)
    private float _auraDurationTimer;

    // 활성화 여부
    private bool _isAuraActive = false;

    // 쿨다운 대기 중 플래그 (v2: 지속시간 만료 후 쿨다운 완료까지 재활성화 방지)
    private bool _isWaitingForCooldown = false;
    #endregion

    #region Initialization
    protected override void OnInitialize()
    {
        base.OnInitialize();

        // 플레이어 참조
        if (PlayerController.Instance != null)
        {
            _playerTransform = PlayerController.Instance.transform;
        }

        // 레이어 마스크 캐싱
        _enemyLayerMask = LayerMask.GetMask("Enemy", "Boss");//12/31

        // 오라 시각 생성
        CreateAuraVisuals();

        // 초기에는 비활성화 (Execute가 호출될 때 활성화)
        _isAuraActive = false;
        if (_auraObject != null)
        {
            _auraObject.SetActive(false);
        }
    }
    #endregion

    #region Core Loop
    /// <summary>
    /// 오라 업데이트 루프 (v2: 지속시간 시스템 추가)
    /// Aura는 지속 효과이므로 매 프레임 업데이트를 수행합니다.
    /// activeDuration 동안 활성화된 후, 쿨타임을 기다리고 다시 활성화됩니다.
    /// </summary>
    public override void UpdateSkill()
    {
        // [v2] 쿨다운 대기 중이면 아무것도 하지 않음 (쿨다운만 진행)
        if (_isWaitingForCooldown)
        {
            base.UpdateSkill(); // 쿨다운만 체크
            return;
        }

        base.UpdateSkill(); // 쿨다운 체크 등 부모 로직 수행

        // 스킬이 꺼져 있으면 처리 안 함
        if (!_isAuraActive) return;

        // [v2] 지속 시간 체크 (0이면 상시 유지)
        if (_data.activeDuration > 0)
        {
            _auraDurationTimer += Time.deltaTime;
            if (_auraDurationTimer >= _data.activeDuration)
            {
                // 지속 시간 만료 - 오라 비활성화
                _isAuraActive = false;
                _isWaitingForCooldown = true; // [v2] 쿨다운 대기 시작
                if (_auraObject != null)
                {
                    _auraObject.SetActive(false);
                }
                return;
            }
        }

        // 1. 데미지 타이머 업데이트
        UpdateDamageTimer();

        // 2. 시각 업데이트 (팔각형 반경 재계산)
        UpdateForcefieldVisuals();
    }

    /// <summary>
    /// 오라 활성화 시작 (쿨다운 완료 후 호출)
    /// </summary>
    protected override void Execute()
    {
        // [v2] 쿨다운 완료 후 재활성화
        if (_isWaitingForCooldown)
        {
            _isWaitingForCooldown = false; // 쿨다운 대기 종료
        }

        // 오라 활성화
        _isAuraActive = true;
        _damageTimer = 0f;
        _auraDurationTimer = 0f; // [v2] 지속 시간 타이머 초기화

        // 시각 효과 켜기
        if (_auraObject != null)
        {
            _auraObject.SetActive(true);
            _innerLineRenderer.enabled = true;
            // 외부 오라는 OnLevelChanged에서 제어됨
        }
    }

    /// <summary>
    /// 데미지 타이머 업데이트
    /// </summary>
    private void UpdateDamageTimer()
    {
        _damageTimer += Time.deltaTime;

        // 데미지 간격 (데이터에 있으면 사용, 없으면 기본값)
        float interval = _data.damageInterval > 0 ? _data.damageInterval : baseDamageInterval;

        if (_damageTimer >= interval)
        {
            _damageTimer = 0f;
            ApplyAuraDamage();
        }
    }

    /// <summary>
    /// 오라 데미지 적용
    /// </summary>
    //private void ApplyAuraDamage()
    //{
    //    if (_playerTransform == null) return;

    //    Vector3 origin = _playerTransform.position;
    //    float range = _data.attackRange * GetSizeMultiplier();
    //    float damage = GetFinalDamage();

    //    // 주변 적 탐지 (NonAlloc: 가비지 컬렉션 최소화)
    //    int hitCount = Physics2D.OverlapCircleNonAlloc(origin, range, _hitBuffer, _enemyLayerMask);

    //    for (int i = 0; i < hitCount; i++)
    //    {
    //        if (_hitBuffer[i] != null && _hitBuffer[i].TryGetComponent<Enemy>(out var enemy))
    //        {
    //            // 살아있는 적만 피격
    //            if (enemy.CurrentHP > 0)
    //            {
    //                enemy.TakeDamage(damage);
    //            }
    //        }
    //    }
    //}

    private void ApplyAuraDamage()
    {
        if (_playerTransform == null) return;

        Vector3 origin = _playerTransform.position;
        float range = _data.attackRange * GetSizeMultiplier();
        float damage = GetFinalDamage();

        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, range, _hitBuffer, _enemyLayerMask);

        for (int i = 0; i < hitCount; i++)
        {
            if (_hitBuffer[i] != null && _hitBuffer[i].TryGetComponent<IDamageable>(out var target))
            {
                if (target.CurrentHP > 0)
                {
                    target.TakeDamage(damage);
                }
            }
        }
    }
    #endregion

    #region Visuals
    /// <summary>
    /// 오라 시각 효과 생성 (v2: 데이터 주도 색상)
    /// </summary>
    private void CreateAuraVisuals()
    {
        if (_auraObject != null) return;

        // 오라 오브젝트 생성 (플레이어 자식)
        _auraObject = new GameObject("Forcefield_Visual");

        if (_playerTransform != null)
        {
            _auraObject.transform.SetParent(_playerTransform);
            _auraObject.transform.localPosition = Vector3.zero;
        }
        else
        {
            // 플레이어가 없으면 현재 위치에 생성
            _auraObject.transform.position = transform.position;
        }

        // [v2] 데이터에서 색상 가져오기 (기본값 폴백)
        Color innerColor = _data != null ? _data.innerAuraColor : new Color(0f, 1f, 1f, 0.5f);
        Color outerColor = _data != null ? _data.outerAuraColor : new Color(0f, 0.5f, 1f, 0.5f);

        // 내부 오라
        _innerLineRenderer = CreateOctagonLineRenderer(_auraObject.transform, "InnerAura",
            innerColor, innerAuraWidth, 5);

        // 외부 오라
        _outerLineRenderer = CreateOctagonLineRenderer(_auraObject.transform, "OuterAura",
            outerColor, outerAuraWidth, 3);

        // 초기화
        _auraRadius = _data.attackRange;
        _outerAuraRadius = _data.attackRange * outerRadiusMultiplier;

        UpdateOctagonShape(_innerLineRenderer, _auraRadius);
        UpdateOctagonShape(_outerLineRenderer, _outerAuraRadius);

        // 초기에는 외부 오라 비활성화 (레벨 3 이상일 때만 활성화)
        _outerLineRenderer.enabled = false;
    }

    /// <summary>
    /// 오라 시각 업데이트 (매 프레임 호출됨)
    /// </summary>
    private void UpdateForcefieldVisuals()
    {
        if (_auraObject == null) return;

        // 크기 업데이트 (레벨업 시 등)
        float sizeMult = GetSizeMultiplier();
        _auraRadius = _data.attackRange * sizeMult;
        _outerAuraRadius = _data.attackRange * sizeMult * outerRadiusMultiplier;

        // 팔각형 모양 업데이트
        UpdateOctagonShape(_innerLineRenderer, _auraRadius);

        // 외부 오라 업데이트 (레벨 3 이상일 때만 활성화)
        if (_outerLineRenderer != null && _outerLineRenderer.enabled)
        {
            UpdateOctagonShape(_outerLineRenderer, _outerAuraRadius);
        }
    }

    /// <summary>
    /// 팔각형 LineRenderer 생성 (코드리뷰 반영: sharedMaterial 사용)
    /// </summary>
    private LineRenderer CreateOctagonLineRenderer(Transform parent, string name, Color color, float width, int sortOrder)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(parent);
        lineObj.transform.localPosition = Vector3.zero;

        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.useWorldSpace = false; // 로컬 좌표 사용 (자동으로 따라다님)
        line.startWidth = width;
        line.endWidth = width;

        // [코드리뷰 반영] shared material 사용 (GC 방지)
        if (lineRendererMaterial != null)
        {
            line.sharedMaterial = lineRendererMaterial;
        }
        else
        {
            // 폴백: 인스펙터에서 할당 안 했으면 기본 머티리얼 사용
            line.material = new Material(Shader.Find("Sprites/Default"));
        }

        line.startColor = color;
        line.endColor = color;
        line.loop = true;
        line.sortingOrder = sortOrder;
        line.positionCount = 8;

        return line;
    }

    /// <summary>
    /// 팔각형 모양 업데이트
    /// </summary>
    private void UpdateOctagonShape(LineRenderer line, float radius)
    {
        if (line == null) return;

        const int OCTAGON_SIDES = 8;
        const float ANGLE_STEP = 45f;

        for (int i = 0; i < OCTAGON_SIDES; i++)
        {
            float angleRad = i * ANGLE_STEP * Mathf.Deg2Rad;
            float x = Mathf.Cos(angleRad) * radius;
            float y = Mathf.Sin(angleRad) * radius;
            line.SetPosition(i, new Vector3(x, y, 0));
        }
    }
    #endregion

    #region Level Management
    /// <summary>
    /// 레벨 변경 시 처리
    /// </summary>
    protected override void OnLevelChanged(int newLevel)
    {
        base.OnLevelChanged(newLevel);

        // 레벨 3 이상일 때 외부 오라 활성화
        if (newLevel >= 3)
        {
            if (_outerLineRenderer != null)
            {
                _outerLineRenderer.enabled = true;
            }
        }
        else
        {
            if (_outerLineRenderer != null)
            {
                _outerLineRenderer.enabled = false;
            }
        }
    }
    #endregion

    #region Cleanup
    /// <summary>
    /// 스킬 비활성화 시 정리 (코드리뷰 반영: proper cleanup)
    /// </summary>
    protected override void OnDeactivate()
    {
        _isAuraActive = false;
        _damageTimer = 0f;

        // 시각 효과 비활성화
        if (_auraObject != null)
        {
            _auraObject.SetActive(false);
        }

        // LineRenderer 참조 유지 (오브젝트 풀링 고려)
        // 필요시 Destroy(_auraObject)로 완전 제거 가능
    }
    #endregion
}
