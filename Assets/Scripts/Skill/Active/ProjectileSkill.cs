using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적을 타겟팅하여 투사체를 발사하는 스킬
/// 레벨에 따라 투사체 수와 크기가 증가
/// </summary>
public class ProjectileSkill : SkillBase
{
    #region Private Fields
    private Transform _playerTransform;
    private List<Enemy> _cachedEnemies;
    private float _enemyCacheTimer;
    private const float ENEMY_CACHE_INTERVAL = 0.3f;

    // LayerMask 캐싱 (검색 속도 최적화)
    private int _enemyLayerMask;
    #endregion

    #region Initialization
    protected override void OnInitialize()
    {
        base.OnInitialize();

        // LayerMask 캐싱 (Enemy 레이어만 감지)
        _enemyLayerMask = LayerMask.GetMask("Enemy");

        // 싱글톤 활용 - 빠르고 확실한 참조
        if (PlayerController.Instance != null)
        {
            _playerTransform = PlayerController.Instance.transform;
        }

        // 초기 적 캐싱
        _cachedEnemies = new List<Enemy>();
        UpdateEnemyCache();
    }
    #endregion

    #region Core Loop
    public override void UpdateSkill()
    {
        base.UpdateSkill();
        UpdateEnemyCache();
    }

    protected override void Execute()
    {
        if (_data == null || _data.prefab == null) return;

        // 가장 가까운 적 찾기
        Transform target = FindNearestEnemy();

        // 기본 방향 결정: 타겟이 있으면 타겟 방향, 없으면 플레이어 좌우 랜덤
        Vector3 baseDirection;
        if (target != null)
        {
            // 타겟 방향
            baseDirection = (target.position - transform.position).normalized;
        }
        else
        {
            // 플레이어 좌우 랜덤 각도 (-45도 ~ +45도)
            float randomAngle = Random.Range(-45f, 45f);
            baseDirection = Quaternion.Euler(0, 0, randomAngle) * Vector3.right;
        }

        // 투사체 발사
        int projectileCount = GetProjectileCount();

        for (int i = 0; i < projectileCount; i++)
        {
            // 기본 방향을 중심으로 확산각 적용
            Vector3 fireDirection = CalculateSpreadDirection(baseDirection, i, projectileCount, _data.defaultSpreadAngle);
            SpawnProjectile(fireDirection);
        }
    }

    /// <summary>
    /// 투사체 생성
    /// </summary>
    private void SpawnProjectile(Vector3 direction)
    {
        float damage = GetFinalDamage();
        float speed = _data.projectileSpeed * GetSpeedMultiplier();
        float size = GetSizeMultiplier();

        // 오브젝트 풀링 사용 (ObjectPoolManager가 있다면)
        Projectile projectile = null;

        if (ObjectPoolManager.Instance != null)
        {
            projectile = ObjectPoolManager.Instance.GetProjectile();
        }

        // 풀이 비어있거나 없으면 직접 생성
        if (projectile == null)
        {
            GameObject projObj = Instantiate(_data.prefab, transform.position, Quaternion.identity);
            projectile = projObj.GetComponent<Projectile>();

            if (projectile == null)
            {
                // 프리팹에 Projectile 컴포넌트가 없으면 추가
                projectile = projObj.AddComponent<Projectile>();
            }
        }
        else
        {
            projectile.gameObject.SetActive(true);
            projectile.gameObject.transform.position = transform.position;
        }

        // Molotov는 투사체 크기 고정, 화염지대 크기만 증가
        float visualScale = (_data.movementType == ProjectileMovementType.Molotov) ? 1f : size;

        // 크기 조절 (Molotov 제외하고 레벨에 따라 증가)
        projectile.transform.localScale = Vector3.one * visualScale;

        // Molotov만 Layer 14: Projectile2로 설정
        if (_data.movementType == ProjectileMovementType.Molotov)
        {
            projectile.gameObject.layer = LayerMask.NameToLayer("Projectile2");
        }

        // 데이터에서 이동 방식 가져와서 투사체 초기화
        projectile.Setup(direction, damage, speed, _data.movementType, this);  // 통계 기록용 source 전달

        // 디버그: 스킬 이름 확인 (진화 스킬 추적용)
        Debug.Log($"[ProjectileSkill] 투사체 생성 - Data.skillName: {_data.skillName}, source.Data.skillName: {this?.Data?.skillName ?? "null"}");

        // 시각 효과 설정 (v2) - SkillData에서 스프라이트, 색상, 크기 적용
        Debug.Log($"[ProjectileSkill] {_data.skillName} - sprite: {_data.projectileSprite?.name ?? "null"}, color: {_data.projectileColor}, scale: {_data.projectileScale * visualScale}");
        projectile.SetSprite(_data.projectileSprite, _data.projectileColor, _data.projectileScale * visualScale);

        // 이펙트 크기 배율 설정 (Molotov의 FireGround 크기 등에 적용 - 항상 레벨 기반 크기 사용)
        projectile.SetEffectSizeMultiplier(size);
    }
    #endregion

    #region Targeting

    /// <summary>
    /// OverlapSphere를 활용한 근처 적 캐싱 
    /// 플레이어 주변 반경 내의 적만 검색하여 성능을 최적화
    /// </summary>
    private void UpdateEnemyCache()
    {
        _enemyCacheTimer += Time.deltaTime;
        if (_enemyCacheTimer >= ENEMY_CACHE_INTERVAL)
        {
            _enemyCacheTimer = 0f;

            if (_playerTransform == null) return;

            // 반경 내의 적 콜라이더만 탐색 (전체 씬 검색 대비 성능 향상)
            float searchRadius = _data.enemySearchRadius > 0 ? _data.enemySearchRadius : 20f;
            Collider[] hitColliders = Physics.OverlapSphere(_playerTransform.position, searchRadius, _enemyLayerMask);

            // 리스트로 변환 (Null 체크 유리)
            _cachedEnemies.Clear();
            foreach (var col in hitColliders)
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null && enemy.CurrentHP > 0)
                {
                    _cachedEnemies.Add(enemy);
                }
            }
        }
    }

    private Transform FindNearestEnemy()
    {
        if (_cachedEnemies == null || _cachedEnemies.Count == 0)
            return null;

        Transform nearest = null;
        float minDistance = float.MaxValue;
        Vector3 origin = _playerTransform != null ? _playerTransform.position : transform.position;

        foreach (var enemy in _cachedEnemies)
        {
            if (enemy == null || enemy.CurrentHP <= 0) continue;

            float distance = Vector3.SqrMagnitude(origin - enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }
    #endregion

    #region Utility
    /// <summary>
    /// 투사체 확산 방향 계산
    /// 매개변수로 확산각을 받아 기획자가 데이터로 조정 가능하게 한다.
    /// </summary>
    private Vector3 CalculateSpreadDirection(Vector3 baseDirection, int index, int totalCount, float spreadAngle)
    {
        if (totalCount <= 1) return baseDirection;

        float angleStep = spreadAngle / (totalCount - 1);
        float startAngle = -spreadAngle * 0.5f;
        float currentAngle = startAngle + (angleStep * index);

        return Quaternion.Euler(0, 0, currentAngle) * baseDirection;
    }

    /// <summary>
    /// 랜덤한 수평 방향 반환
    /// 매개변수로 받은 각도 범위 내에서 랜덤한 방향을 반환한다.
    /// </summary>
    /// <param name="angleRange">각도 범위 (예: 60f이면 -30도 ~ +30도)</param>
    private Vector3 GetRandomHorizontalDirection(float angleRange = 60f)
    {
        float halfRange = angleRange * 0.5f;
        float randomAngle = Random.Range(-halfRange, halfRange);
        return Quaternion.Euler(0, 0, randomAngle) * Vector3.right;
    }
    #endregion
}
