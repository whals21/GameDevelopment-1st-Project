using UnityEngine;
using System.Collections;

/// <summary>
/// RPG 스킬 구현
/// 가장 가까운 적에게 유도 로켓을 발사합니다.
/// 레벨에 따라 한 번에 발사하는 로켓 수가 증가합니다.
///
/// 성능 최적화:
/// - TargetingHelper로 타겟팅 로직 재사용 (코드 중복 제거)
/// - PlayerController.Instance로 빠른 플레이어 참조
/// - LayerMask 캐싱으로 불필요한 콜라이더 필터링
/// - 적 캐싱으로 불필요한 탐색 최소화
///
/// 확장성:
/// - 레벨에 따른 로켓 수 증가
/// - 데이터 주도 설계 (rapidFireInterval, defaultSpreadAngle 등)
/// - 수학적 단순화 (균등 분할 공식)
/// </summary>
public class RPGSkill : SkillBase
{
    #region Private Fields
    private Transform _playerTransform;
    private int _enemyLayerMask;

    // 적 타겟팅 (캐싱)
    private Enemy _currentTarget;
    private float _targetUpdateTimer;
    private const float TARGET_UPDATE_INTERVAL = 0.5f;
    #endregion

    #region Initialization
    protected override void OnInitialize()
    {
        base.OnInitialize();

        // 싱글톤으로 빠른 플레이어 참조
        if (PlayerController.Instance != null)
        {
            _playerTransform = PlayerController.Instance.transform;
        }

        // LayerMask 캐싱
        _enemyLayerMask = LayerMask.GetMask("Enemy");
    }
    #endregion

    #region Core Loop
    public override void UpdateSkill()
    {
        base.UpdateSkill();
        UpdateTarget();
    }

    /// <summary>
    /// RPG 스킬 발동
    /// 현재 타겟에게 로켓을 발사합니다.
    /// </summary>
    protected override void Execute()
    {
        // 타겟 유효성 검사
        if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy || _currentTarget.CurrentHP <= 0)
        {
            return;
        }

        // 다중 로켓 발사
        int rocketCount = GetRocketCount();
        StartCoroutine(FireRocketsSequence(rocketCount));
    }

    /// <summary>
    /// 다중 로켓 발사 시퀀스
    /// </summary>
    private IEnumerator FireRocketsSequence(int rocketCount)
    {
        float delay = _data.rapidFireInterval;

        for (int i = 0; i < rocketCount; i++)
        {
            // 스킬 비활성화 시 중단
            if (!_isActive) yield break;

            FireSingleRocket(i, rocketCount);

            // 발사 간 딜레이 (마지막은 제외)
            if (i < rocketCount - 1 && delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
        }
    }

    /// <summary>
    /// 단일 로켓 발사
    /// </summary>
    private void FireSingleRocket(int index, int totalCount)
    {
        if (_currentTarget == null || ObjectPoolManager.Instance == null) return;

        // v2: 통합 투사체 시스템 사용 (RPGProjectile → Projectile + Homing)
        Projectile rocket = ObjectPoolManager.Instance.GetProjectile();
        if (rocket == null)
        {
            Debug.LogWarning("[RPGSkill] Projectile를 풀에서 가져올 수 없습니다.");
            return;
        }

        // 기본 방향 계산
        Vector3 targetPos = _currentTarget.transform.position;
        Vector3 direction = (targetPos - _playerTransform.position).normalized;

        // 통합된 각도 계산 공식 (수학적 단순화)
        if (totalCount > 1)
        {
            float totalSpread = _data.defaultSpreadAngle;
            float offset = CalculateAngleOffset(index, totalCount, totalSpread);
            direction = Quaternion.Euler(0, 0, offset) * direction;
        }

        // 로켓 발사 (v2: 위치 설정 후 Setup 호출)
        float damage = GetFinalDamage();
        float speed = GetProjectileSpeed();
        float size = GetSizeMultiplier();
        rocket.transform.position = _playerTransform.position;
        rocket.gameObject.SetActive(true);
        rocket.Setup(direction, damage, speed, ProjectileMovementType.Homing);

        // 시각 효과 설정 (v2) - SkillData에서 스프라이트, 색상, 크기 적용
        Debug.Log($"[RPGSkill] {_data.skillName} - sprite: {_data.projectileSprite?.name ?? "null"}, color: {_data.projectileColor}, scale: {_data.projectileScale * size}");
        rocket.SetSprite(_data.projectileSprite, _data.projectileColor, _data.projectileScale * size);
    }
    #endregion

    #region Targeting
    /// <summary>
    /// 타겟 업데이트 (TargetingHelper 사용)
    /// 일정 주기로 가장 가까운 적을 찾습니다.
    /// </summary>
    private void UpdateTarget()
    {
        _targetUpdateTimer += Time.deltaTime;
        if (_targetUpdateTimer >= TARGET_UPDATE_INTERVAL)
        {
            _targetUpdateTimer = 0f;

            if (_playerTransform == null || _data == null) return;

            // TargetingHelper 사용 (코드 중복 제거)
            float searchRadius = _data.attackRange > 0 ? _data.attackRange : 15f;
            _currentTarget = TargetingHelper.FindNearestEnemy(
                _playerTransform.position,
                searchRadius,
                _enemyLayerMask
            );
        }
    }
    #endregion

    #region Stats Calculation
    /// <summary>
    /// 다중 발사체 각도 오프셋을 계산합니다.
    /// 수학적 단순화: 모든 개수를 하나의 공식으로 처리
    ///
    /// 공식: (인덱스 / (전체-1) - 0.5) * 전체각도
    /// 예: 2개 -> (-0.5 * 60), (0.5 * 60) -> -30도, +30도
    /// 예: 3개 -> (-0.5 * 60), (0 * 60), (0.5 * 60) -> -30도, 0도, +30도
    /// </summary>
    private float CalculateAngleOffset(int index, int totalCount, float spreadAngle)
    {
        if (totalCount <= 1) return 0f;
        return (index / (totalCount - 1f) - 0.5f) * spreadAngle;
    }

    /// <summary>
    /// 레벨에 따른 로켓 수를 반환합니다.
    /// </summary>
    private int GetRocketCount()
    {
        int baseCount = _data.projectileCount > 0 ? _data.projectileCount : 1;
        int additionalCount = GetAdditionalProjectiles();
        return Mathf.Max(baseCount + additionalCount, 1);
    }

    /// <summary>
    /// 투사체 속도를 반환합니다.
    /// </summary>
    private float GetProjectileSpeed()
    {
        SkillLevel levelData = GetLevelData(_currentLevel);
        float speedMultiplier = levelData?.projectileSpeedMultiplier ?? 1f;
        float baseSpeed = _data.projectileSpeed > 0 ? _data.projectileSpeed : 8f;
        return baseSpeed * speedMultiplier;
    }

    /// <summary>
    /// 레벨별 추가 투사체 수를 가져옵니다.
    /// </summary>
    private int GetAdditionalProjectiles()
    {
        SkillLevel levelData = GetLevelData(_currentLevel);
        return levelData?.additionalProjectiles ?? 0;
    }
    #endregion

    #region Cleanup
    protected override void OnDeactivate()
    {
        _currentTarget = null;
        _targetUpdateTimer = 0f;
    }
    #endregion
}
