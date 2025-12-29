using UnityEngine;

/// <summary>
/// 모든 스킬의 기본 클래스 (Abstract)
/// 모든 스킬은 이 클래스를 상속받아야 합니다.
/// 매니저는 SkillBase만 바라보며, 구체적인 구현은 자식 클래스에 위임합니다.
///
/// 성능 최적화: 상태 캐싱 (State Caching)
/// - 스탯 계산은 레벨 변경 시 한 번만 수행되어 캐싱됩니다.
/// - GetFinalDamage() 등의 메서드는 O(1)로 캐시된 값을 반환합니다.
/// - 뱀서류 게임에서 1초에 수십 발의 총알이 발사되어도 성능 저하가 없습니다.
/// </summary>
public abstract class SkillBase : MonoBehaviour
{
    #region Protected Fields
    protected SkillData _data;
    protected int _currentLevel = 1;
    protected float _currentCooldown;
    protected bool _isActive = false;
    #endregion

    #region Cached Stats (상태 캐싱)
    /// <summary>
    /// 계산된 스탯을 캐싱해두는 필드들
    /// 레벨 변경 시 한 번만 재계산되어 성능을 최적화합니다.
    /// </summary>
    private float _cachedDamage;
    private float _cachedCooldown;
    private float _cachedSpeedMultiplier;
    private float _cachedSizeMultiplier;
    private int _cachedProjectileCount;
    #endregion

    #region Properties
    public bool IsActive => _isActive;
    public int CurrentLevel => _currentLevel;

    /// <summary>
    /// 스킬 데이터 (읽기 전용)
    /// 원칙: SkillData는 런타임에 변경되지 않습니다.
    /// </summary>
    public SkillData Data => _data;

    public float CurrentCooldown => _currentCooldown;
    #endregion

    #region Initialization
    /// <summary>
    /// 스킬 초기화
    /// </summary>
    /// <param name="data">스킬 데이터</param>
    /// <param name="level">시작 레벨 (기본 1)</param>
    public virtual void Init(SkillData data, int level = 1)
    {
        _data = data;
        _currentLevel = Mathf.Clamp(level, 1, 5);
        _currentCooldown = 0f; // 바로 사용할 수 있도록 쿨타임 0으로 시작
        _isActive = true;

        // 스탯 캐싱 (초기화 시 한 번만 계산)
        RecalculateStats();

        OnInitialize();
    }

    /// <summary>
    /// 초기화 시 추가 로직 (자식 클래스에서 오버라이드)
    /// </summary>
    protected virtual void OnInitialize()
    {
        // 자식 클래스에서 필요시 구현
    }
    #endregion

    #region Core Loop
    /// <summary>
    /// 매니저가 매 프레임 호출합니다.
    /// 쿨다운을 관리하고, 시점이 되면 Execute()를 호출합니다.
    /// </summary>
    public virtual void UpdateSkill()
    {
        if (!_isActive) return;

        // 쿨다운 감소
        if (_currentCooldown > 0f)
        {
            _currentCooldown -= Time.deltaTime;
        }

        // 쿨다운이 완료되면 스킬 발동
        if (_currentCooldown <= 0f)
        {
            if (TryExecute())
            {
                _currentCooldown = _cachedCooldown; // 캐시된 쿨타임 사용
            }
        }
    }

    /// <summary>
    /// 스킬 발동 시도
    /// </summary>
    /// <returns>발동 성공 여부</returns>
    protected virtual bool TryExecute()
    {
        // 타겟이 있는지 확인 등 실행 가능 여부 체크
        if (!CanExecute())
        {
            return false;
        }

        Execute();
        return true;
    }

    /// <summary>
    /// 스킬 실행 가능 여부 확인 (자식 클래스에서 오버라이드 가능)
    /// </summary>
    protected virtual bool CanExecute()
    {
        return _data != null && _isActive;
    }

    /// <summary>
    /// 실제 스킬 효과 발동
    /// 각 스킬마다 다른 방식으로 구현합니다.
    /// </summary>
    protected abstract void Execute();
    #endregion

    #region Level Management
    /// <summary>
    /// 스킬 레벨 설정
    /// 레벨 변경 시 스탯이 자동으로 재계산되어 캐싱됩니다.
    /// </summary>
    /// <param name="newLevel">새 레벨</param>
    public virtual void SetLevel(int newLevel)
    {
        if (newLevel == _currentLevel) return;

        _currentLevel = Mathf.Clamp(newLevel, 1, 5);

        // 스탯 재계산 및 캐싱
        RecalculateStats();

        OnLevelChanged(_currentLevel);
    }

    /// <summary>
    /// 레벨 변경 시 추가 로직 (자식 클래스에서 오버라이드)
    /// </summary>
    /// <param name="newLevel">새 레벨</param>
    protected virtual void OnLevelChanged(int newLevel)
    {
        // 자식 클래스에서 필요시 구현
    }
    #endregion

    #region Stats Calculation (State Caching)

    /// <summary>
    /// 모든 스탯을 재계산하여 캐싱합니다.
    /// 레벨 변경이나 초기화 시 한 번만 호출됩니다.
    /// 복잡한 계산 로직이 모두 여기에 집중됩니다.
    /// </summary>
    private void RecalculateStats()
    {
        if (_data == null)
        {
            _cachedDamage = 0f;
            _cachedCooldown = 1f;
            _cachedSpeedMultiplier = 1f;
            _cachedSizeMultiplier = 1f;
            _cachedProjectileCount = 1;
            return;
        }

        SkillLevel levelData = GetLevelData(_currentLevel);

        // 데미지 계산
        float damageMult = levelData?.damageMultiplier ?? 1f;
        _cachedDamage = _data.damage * damageMult;

        // 쿨타임 계산
        float cooldownMult = levelData?.cooldownMultiplier ?? 1f;
        _cachedCooldown = Mathf.Max(0.1f, _data.cooldown * cooldownMult);

        // 속도 배수 계산
        _cachedSpeedMultiplier = levelData?.projectileSpeedMultiplier ?? 1f;

        // 크기 배수 계산 (루프 포함)
        float sizeMult = levelData?.projectileSizeMultiplier ?? 1f;
        float totalIncrease = 0f;
        for (int i = 1; i <= _currentLevel && i <= _data.levels.Length; i++)
        {
            totalIncrease += _data.levels[i - 1].projectileScaleIncrease;
        }
        _cachedSizeMultiplier = sizeMult + totalIncrease;

        // 투사체 개수 계산
        int additionalProjectiles = levelData?.additionalProjectiles ?? 0;
        _cachedProjectileCount = _data.projectileCount + additionalProjectiles;
    }

    /// <summary>
    /// 현재 레벨의 최종 데미지 (캐시된 값 반환, O(1))
    /// </summary>
    protected virtual float GetFinalDamage()
    {
        return _cachedDamage;
    }

    /// <summary>
    /// 현재 레벨의 최종 쿨타임 (캐시된 값 반환, O(1))
    /// </summary>
    protected virtual float GetFinalCooldown()
    {
        return _cachedCooldown;
    }

    /// <summary>
    /// 현재 레벨의 투사체 속도 배수 (캐시된 값 반환, O(1))
    /// </summary>
    protected virtual float GetSpeedMultiplier()
    {
        return _cachedSpeedMultiplier;
    }

    /// <summary>
    /// 현재 레벨의 투사체 크기 배수 (캐시된 값 반환, O(1))
    /// </summary>
    protected virtual float GetSizeMultiplier()
    {
        return _cachedSizeMultiplier;
    }

    /// <summary>
    /// 현재 레벨의 투사체 개수 (캐시된 값 반환, O(1))
    /// </summary>
    protected virtual int GetProjectileCount()
    {
        return _cachedProjectileCount;
    }

    /// <summary>
    /// 레벨 데이터 가져오기
    /// </summary>
    /// <param name="level">레벨</param>
    /// <returns>레벨 데이터 (없으면 null)</returns>
    protected SkillLevel GetLevelData(int level)
    {
        if (_data?.levels == null || level <= 0 || level > _data.levels.Length)
            return null;

        return _data.levels[level - 1];
    }
    #endregion

    #region Cleanup
    /// <summary>
    /// 스킬 비활성화
    /// </summary>
    public virtual void Deactivate()
    {
        _isActive = false;
        OnDeactivate();
    }

    /// <summary>
    /// 비활성화 시 추가 로직 (자식 클래스에서 오버라이드)
    /// </summary>
    protected virtual void OnDeactivate()
    {
        // 자식 클래스에서 필요시 구현
    }
    #endregion
}
