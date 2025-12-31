using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 드론 스킬 구현
/// 자율적으로 적을 찾아 공격하는 드론을 소환합니다.
/// 레벨에 따라 소환되는 드론의 수가 증가합니다.
///
/// 성능 최적화:
/// - PlayerController.Instance로 빠른 플레이어 참조
/// - 로컬 좌표계 사용으로 불필요한 연산 제거
/// - Physics2D.OverlapCircleNonAlloc로 반경 내 적만 탐색
///
/// 확장성:
/// - 레벨에 따른 다중 소환 지원
/// - 데이터 위임 패턴 (계산은 부모, 행동은 자식)
/// - 데이터 주도 설계 (호버링 파라미터 ScriptableObject화)
/// </summary>
public class DroneSkill : SkillBase
{
    #region Private Fields
    // 다중 드론 관리 (레벨에 따라 2~3마리 소환 가능)
    private List<GameObject> _droneObjects = new List<GameObject>();
    private Transform _playerTransform;
    #endregion

    #region Initialization
    protected override void OnInitialize()
    {
        base.OnInitialize();

        // 싱글톤으로 빠른 플레이어 참조
        if (PlayerController.Instance != null)
        {
            _playerTransform = PlayerController.Instance?.transform;//12/31
        }
    }
    #endregion

    #region Core Loop
    protected override void Execute()
    {
        // 스킬 발동 시마다 드론 수 체크 및 갱신
        UpdateDrones();
    }

    /// <summary>
    /// 드론 소환수를 관리하고 스탯을 업데이트합니다.
    /// </summary>
    private void UpdateDrones()
    {
        int targetCount = GetTargetDroneCount();

        // 마리수 부족 시 생성
        while (_droneObjects.Count < targetCount)
        {
            CreateSingleDrone();
        }

        // 마리수 초과 시 제거 (레벨 다운 등의 예외 상황)
        while (_droneObjects.Count > targetCount)
        {
            RemoveDrone();
        }

        // 모든 드론 스탯 업데이트
        UpdateAllDroneStats();
    }

    /// <summary>
    /// 레벨에 따른 목표 드론 수를 반환합니다.
    /// 뱀서류 게임 패턴: 레벨업 시 소환수 증가
    /// </summary>
    private int GetTargetDroneCount()
    {
        if (_currentLevel >= 7) return 3;
        if (_currentLevel >= 4) return 2;
        return 1;
    }

    /// <summary>
    /// 단일 드론을 생성합니다.
    /// </summary>
    private void CreateSingleDrone()
    {
        if (_data?.prefab == null) return;

        GameObject droneObj = Instantiate(_data.prefab, _playerTransform);
        droneObj.name = $"Drone_{_data.skillName}_{_droneObjects.Count}";

        // Drone 컴포넌트를 가져와서 초기화
        var drone = droneObj.GetComponent<Drone>();
        if (drone != null)
        {
            float damage = GetFinalDamage();
            float cooldown = GetFinalCooldown();
            float radius = _data.hoverRadius;
            float speed = _data.hoverSpeed;
            float projectileSpeed = _data.projectileSpeed;
            int enemyLayerMask = LayerMask.GetMask("Enemy");

            drone.Initialize(_playerTransform, damage, cooldown, radius, speed, projectileSpeed, enemyLayerMask, _data);
        }

        _droneObjects.Add(droneObj);
    }

    /// <summary>
    /// 가장 최근에 생성된 드론을 제거합니다.
    /// </summary>
    private void RemoveDrone()
    {
        if (_droneObjects.Count == 0) return;

        int lastIndex = _droneObjects.Count - 1;
        GameObject droneToRemove = _droneObjects[lastIndex];

        if (droneToRemove != null)
        {
            Destroy(droneToRemove);
        }

        _droneObjects.RemoveAt(lastIndex);
    }

    /// <summary>
    /// 모든 드론의 스탯을 업데이트합니다.
    /// Drone 컴포넌트를 사용하여 스탯 업데이트
    /// </summary>
    private void UpdateAllDroneStats()
    {
        float finalDamage = GetFinalDamage();
        float finalCooldown = GetFinalCooldown();

        for (int i = 0; i < _droneObjects.Count; i++)
        {
            if (_droneObjects[i] != null &&
                _droneObjects[i].TryGetComponent<Drone>(out var drone))
            {
                // Drone 스탯 업데이트
                drone.UpdateStats(finalDamage, finalCooldown, _data.hoverSpeed);
            }
        }
    }
    #endregion

    #region Level Management
    protected override void OnLevelChanged(int newLevel)
    {
        base.OnLevelChanged(newLevel);
        // 레벨 변경 시 드론 수 재조정
        UpdateDrones();
    }
    #endregion

    #region Cleanup
    public override void Deactivate()
    {
        base.Deactivate();

        // 모든 드론 제거
        foreach (var drone in _droneObjects)
        {
            if (drone != null)
            {
                Destroy(drone);
            }
        }
        _droneObjects.Clear();
    }
    #endregion



}

/// <summary>
/// 드론 동작 컴포넌트
/// 드론이 플레이어 주변을 호버링하며 적을 공격합니다.
///
/// 설계 원칙:
/// - 플레이어의 자식으로 생성되므로 PlayerController 참조 불필요
/// - 부모 기준 로컬 좌표로 위치 계산
/// - 스탯 계산은 DroneSkill에서 위임받음
/// - 호버링 파라미터는 SkillData에서 가져옴 (데이터 주도 설계)
/// </summary>
public class DroneBehavior : MonoBehaviour
{
    // 위임받은 스탯 (직접 계산하지 않음)
    private float _damage;
    private float _cooldown;
    private SkillData _data;

    // 회전 관련
    private float _angleOffset = 0f;  // 초기 각도 (여러 마리일 때 분배용)
    private float _hoverAngle = 0f;
    private float _attackTimer = 0f;

    // LayerMask 캐싱
    private int _enemyLayerMask;



    /// <summary>
    /// 스탯을 설정합니다.
    /// DroneSkill에서 계산된 값을 받습니다.
    /// </summary>
    public void SetStats(float damage, float cooldown, SkillData data)
    {
        _damage = damage;
        _cooldown = cooldown;
        _data = data;
    }

    /// <summary>
    /// 초기 각도 오프셋을 설정합니다.
    /// 여러 마리가 서로 겹치지 않게 배치하기 위함입니다.
    /// </summary>
    public void SetOffsetAngle(float offset)
    {
        _angleOffset = offset;
        _hoverAngle = offset;
    }

    public void Activate()
    {
        gameObject.SetActive(true);

        // LayerMask 캐싱
        _enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    private void Update()
    {
        if (_data == null) return;

        // 부모(Player) 기준 로컬 회전 (월드 좌표 계산 불필요)
        _hoverAngle += _data.hoverSpeed * Time.deltaTime;

        float rad = _hoverAngle * Mathf.Deg2Rad;
        transform.localPosition = new Vector3(
            Mathf.Cos(rad) * _data.hoverRadius,
            Mathf.Sin(rad) * _data.hoverRadius * _data.hoverEllipseRatio,  // 타원형 비율 적용
            0
        );

        // 공격 타이머 (데이터에서 설정한 최소값으로 클램프)
        _attackTimer += Time.deltaTime;
        float attackInterval = Mathf.Max(_data.attackIntervalMin, _cooldown);
        if (_attackTimer >= attackInterval)
        {
            _attackTimer = 0f;
            //FireAtEnemy();//12/31
            ApplyDamageToNearestEnemy();//12/31
        }
    }

    /// <summary>
    /// 가장 가까운 적에게 투사체 발사 (TargetingHelper 사용)
    /// </summary>
    private void FireAtEnemy()
    {
        if (_data == null) return;

        float searchRadius = _data.enemySearchRadius > 0 ? _data.enemySearchRadius : 20f;
        Vector3 worldPos = transform.position;

        // TargetingHelper로 중복 제거
        Enemy nearest = TargetingHelper.FindNearestEnemy(
            worldPos,
            searchRadius,
            _enemyLayerMask
        );

        if (nearest != null && _data.prefab != null)
        {
            // 투사체 발사
            Vector3 direction = (nearest.transform.position - worldPos).normalized;

            // 오브젝트 풀링 사용
            Projectile projectile = null;
            if (ObjectPoolManager.Instance != null)
            {
                projectile = ObjectPoolManager.Instance.GetProjectile();
            }

            if (projectile == null)
            {
                GameObject projectileObj = Instantiate(_data.prefab, worldPos, Quaternion.identity);
                projectile = projectileObj.GetComponent<Projectile>();
                if (projectile == null)
                {
                    projectile = projectileObj.AddComponent<Projectile>();
                }
            }
            else
            {
                projectile.gameObject.SetActive(true);
                projectile.gameObject.transform.position = worldPos;
            }

            // 위임받은 데미지 사용
            projectile.Setup(direction, _damage, _data.projectileSpeed, _data.movementType);

            // 시각 효과 설정 (v2) - SkillData에서 스프라이트, 색상, 크기 적용
            projectile.SetSprite(_data.projectileSprite, _data.projectileColor, _data.projectileScale);
        }
    }

    #region 12/31
    private void ApplyDamageToNearestEnemy()
    {
        Vector3 worldPos = transform.position;
        var targets = new List<IDamageable>();
        Collider2D[] hits = new Collider2D[32];
        int count = Physics2D.OverlapCircleNonAlloc(worldPos, _data.enemySearchRadius, hits, _enemyLayerMask);

        for (int i = 0; i < count; i++)
        {
            if (hits[i].TryGetComponent<IDamageable>(out var dmg) && dmg.CurrentHP > 0)
                targets.Add(dmg);
        }

        if (targets.Count == 0) return;

        // 가장 가까운 적 찾기
        IDamageable nearest = null;
        float minDist = float.MaxValue;
        foreach (var t in targets)
        {
            float dist = Vector3.Distance(worldPos, ((MonoBehaviour)t).transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = t;
            }
        }

        nearest?.TakeDamage(_damage);
    }
    #endregion
}
