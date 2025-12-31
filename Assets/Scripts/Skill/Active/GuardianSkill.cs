using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어 주변을 회전하며 적을 공격하는 가디언을 소환하는 스킬
/// 레벨에 따라 소환되는 가디언의 수가 증가
/// </summary>
public class GuardianSkill : SkillBase
{
    #region Private Fields
    // 다중 가디언 관리 (레벨에 따라 2~3마리 소환 가능)
    private List<GameObject> _guardianObjects = new List<GameObject>();
    private Transform _playerTransform;
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
    }
    #endregion

    #region Core Loop
    protected override void Execute()
    {
        // 스킬 발동 시마다 가디언 수 체크 및 갱신
        UpdateGuardians();
    }

    /// <summary>
    /// 가디언 소환수를 관리하고 스탯을 업데이트
    /// </summary>
    private void UpdateGuardians()
    {
        // [v2] 파괴되거나 비활성화된 가디언 cleanup
        for (int i = _guardianObjects.Count - 1; i >= 0; i--)
        {
            GameObject guardian = _guardianObjects[i];
            // null이거나 비활성화된 가디언 제거
            if (guardian == null || !guardian.activeInHierarchy)
            {
                Debug.Log($"[GuardianSkill] Found invalid guardian at index {i}, removing from list");
                _guardianObjects.RemoveAt(i);
            }
        }

        int targetCount = GetTargetGuardianCount();
        int currentCount = _guardianObjects.Count;

        Debug.Log($"[GuardianSkill] UpdateGuardians - Current: {currentCount}, Target: {targetCount}");

        // 마리수 부족 시 생성
        while (_guardianObjects.Count < targetCount)
        {
            CreateSingleGuardian();
        }

        // 마리수 초과 시 제거 (레벨 다운 등의 예외 상황)
        while (_guardianObjects.Count > targetCount)
        {
            RemoveGuardian();
        }

        // 모든 가디언 스탯 업데이트
        UpdateAllGuardianStats();
    }

    /// <summary>
    /// 레벨에 따른 목표 가디언 수를 반환
    /// 레벨에 정비례하게 가디언 수 증가 (최대 5레벨)
    /// </summary>
    private int GetTargetGuardianCount()
    {
        // 레벨 1~5에 따라 1~5마리 소환
        return Mathf.Clamp(_currentLevel, 1, 5);
    }

    /// <summary>
    /// 단일 가디언을 생성
    /// </summary>
    private void CreateSingleGuardian()
    {
        if (_data?.prefab == null) return;

        GameObject guardianObj = Instantiate(_data.prefab, _playerTransform);
        guardianObj.name = $"Guardian_{_data.skillName}_{_guardianObjects.Count}";

        // GuardianTop 컴포넌트를 가져와서 초기화
        var guardianTop = guardianObj.GetComponent<GuardianTop>();
        if (guardianTop != null)
        {
            float damage = GetFinalDamage();
            float range = _data.attackRange * GetSizeMultiplier();
            float knockback = 5f; // 넉백력
            float speed = _data.hoverSpeed; // 회전 속도 (도/초)
            int enemyLayerMask = LayerMask.GetMask("Enemy", "Boss");

            guardianTop.Initialize(damage, knockback, range, speed, _playerTransform, enemyLayerMask);
        }

        _guardianObjects.Add(guardianObj);
    }

    /// <summary>
    /// 가장 최근에 생성된 가디언을 제거
    /// </summary>
    private void RemoveGuardian()
    {
        if (_guardianObjects.Count == 0) return;

        int lastIndex = _guardianObjects.Count - 1;
        GameObject guardianToRemove = _guardianObjects[lastIndex];

        Debug.Log($"[GuardianSkill] Removing guardian: {guardianToRemove?.name}");

        if (guardianToRemove != null)
        {
            Destroy(guardianToRemove);
        }

        _guardianObjects.RemoveAt(lastIndex);
    }

    /// <summary>
    /// 모든 가디언의 스탯을 업데이트
    /// GuardianTop 컴포넌트를 사용하여 스탯 업데이트
    /// </summary>
    private void UpdateAllGuardianStats()
    {
        float finalDamage = GetFinalDamage();
        float finalRange = _data.attackRange * GetSizeMultiplier();
        float knockback = 5f;
        float speed = _data.hoverSpeed;
        int enemyLayerMask = LayerMask.GetMask("Enemy", "Boss");

        // 여러 마리일 때 서로 겹치지 않게 각도 분배
        float angleStep = 360f / _guardianObjects.Count;

        for (int i = 0; i < _guardianObjects.Count; i++)
        {
            if (_guardianObjects[i] != null &&
                _guardianObjects[i].TryGetComponent<GuardianTop>(out var guardianTop))
            {
                // GuardianTop 스탯 업데이트
                guardianTop.UpdateStats(finalDamage, knockback, speed);

                // 초기 각도 설정 (360도를 균등하게 분배)
                float initialAngle = i * angleStep;
                guardianTop.SetInitialAngle(initialAngle);
            }
        }
    }
    #endregion

    #region Level Management
    protected override void OnLevelChanged(int newLevel)
    {
        base.OnLevelChanged(newLevel);
        // 레벨 변경 시 가디언 수 재조정
        UpdateGuardians();
    }
    #endregion

    #region Cleanup
    public override void Deactivate()
    {
        base.Deactivate();

        Debug.Log($"[GuardianSkill] Deactivate - Removing {_guardianObjects.Count} guardians");

        // 모든 가디언 제거
        foreach (var guardian in _guardianObjects)
        {
            if (guardian != null)
            {
                Destroy(guardian);
            }
        }
        _guardianObjects.Clear();
    }
    #endregion
}

/// <summary>
/// 가디언 동작 컴포넌트
/// 가디언 오브젝트에 붙어서 회전 및 공격을 담당
///
/// 설계 원칙:
/// - 플레이어의 자식으로 생성되므로 PlayerController 참조 불필요
/// - 부모 기준 로컬 좌표로 위치 계산
/// - 스탯 계산은 GuardianSkill에서 위임받음
/// </summary>
public class GuardianBehavior : MonoBehaviour
{
    // 위임받은 스탯 (직접 계산하지 않음)
    private float _damage;
    private float _range;
    private float _attackCooldown;

    // 회전 관련
    private float _angleOffset = 0f;  // 초기 각도 (여러 마리일 때 분배용)
    private float _currentAngle = 0f;
    private float _attackTimer = 0f;

    // 회전 설정 (데이터에서 가져올 수도 있음)
    private const float ORBIT_RADIUS = 3f;
    private const float ROTATION_SPEED = 180f;

    // LayerMask 캐싱
    private int _enemyLayerMask;

    /// <summary>
    /// 스탯을 설정합니다.
    /// GuardianSkill에서 계산된 값을 받아 사용한다
    /// </summary>
    public void SetStats(float damage, float range, float cooldown)
    {
        _damage = damage;
        _range = range;
        _attackCooldown = Mathf.Max(0.3f, cooldown);
    }

    /// <summary>
    /// 초기 각도 오프셋을 설정
    /// 여러 마리가 서로 겹치지 않게 배치
    /// </summary>
    public void SetOffsetAngle(float offset)
    {
        _angleOffset = offset;
        _currentAngle = offset;
    }

    public void Activate()
    {
        gameObject.SetActive(true);

        // LayerMask 캐싱 (Enemy + Boss)
        _enemyLayerMask = LayerMask.GetMask("Enemy", "Boss");
    }

    private void Update()
    {
        // 부모(Player) 기준 로컬 회전 (월드 좌표 계산 불필요)
        _currentAngle += ROTATION_SPEED * Time.deltaTime;

        float rad = _currentAngle * Mathf.Deg2Rad;
        transform.localPosition = new Vector3(
            Mathf.Cos(rad) * ORBIT_RADIUS,
            Mathf.Sin(rad) * ORBIT_RADIUS,
            0
        );

        // 주기적 공격
        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackCooldown)
        {
            _attackTimer = 0f;
            AttackNearestEnemy();
        }
    }

    /// <summary>
    /// 가장 가까운 적 공격 (TargetingHelper 사용)
    /// </summary>
    private void AttackNearestEnemy()
    {
        if (_range <= 0) return;

        // TargetingHelper로 중복 제거
        Enemy nearest = TargetingHelper.FindNearestEnemy(
            transform.position,
            _range,
            _enemyLayerMask
        );

        if (nearest != null)
        {
            // 위임받은 데미지로 공격
            nearest.TakeDamage(_damage, null);  // GuardianBehavior는 SkillBase가 아니므로 null 전달
        }
    }
}
