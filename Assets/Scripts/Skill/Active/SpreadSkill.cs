using UnityEngine;

/// <summary>
/// 분산 스킬 - 다중 방향 투사체 발사
/// 데이터 주도 설계로 다양한 분산 패턴을 지원합니다.
///
/// 지원하는 스킬 유형 (데이터로 설정):
/// - 단일 타겟팅: 가장 가까운 적에게 1발 발사
/// - 다중 발사: 360도 분산으로 여러 발 발사 (예: Tri-Laser, SpiralShot)
/// - 공전 투사체: 플레이어 주변을 공전하며 공격 (예: MagneticDart)
///
/// 성능 최적화:
/// - PlayerController.Instance로 빠른 플레이어 참조
/// - Physics2D.OverlapCircleNonAlloc로 반경 내 적만 탐색
///
/// 설계 원칙:
/// - 문자열 분기 제거 (하드코딩 없음)
/// - 데이터 주도 설계 (모든 특성은 SkillData로 설정)
/// - 코드 중복 제거 (통합된 발사 메서드)
/// </summary>
[SkillType(SkillType.Spread)]
public class SpreadSkill : SkillBase
{
    #region Private Fields
    private Transform _playerTransform;
    private int _enemyLayerMask;
    private Collider2D[] _hitBuffer = new Collider2D[128];
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
    /// <summary>
    /// 분산 스킬 실행
    /// 데이터에 정의된 발사 패턴에 따라 투사체를 발사합니다.
    /// </summary>
    protected override void Execute()
    {
        if (_data == null) return;

        // 데이터에서 발사 개수 가져오기 (specialProjectileCount 사용)
        int projectileCount = Mathf.Max(1, _data.specialProjectileCount);
        Vector3 origin = _playerTransform != null ? _playerTransform.position : transform.position;

        // 단일 발사 vs 다중 발사 분기
        if (projectileCount == 1)
        {
            // 단일 발사: 가장 가까운 적 타겟팅
            Vector3 direction = FindNearestEnemyDirection(origin);
            SpawnProjectile(direction, -1); // orbitIndex -1 = 공전 안 함
        }
        else
        {
            // 다중 발사: 360도 자동 분산 (데이터 기반)
            float angleStep = 360f / projectileCount;
            float startAngle = _data.initialAngleOffset;

            for (int i = 0; i < projectileCount; i++)
            {
                float angle = startAngle + (angleStep * i);
                Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;

                // 공전 모드면 i를 orbitIndex로 전달, 아니면 -1
                int orbitIndex = _data.isOrbiting ? i : -1;
                SpawnProjectile(direction, orbitIndex);
            }
        }
    }

    /// <summary>
    /// 가장 가까운 적의 방향을 찾습니다.
    /// 타겟이 없으면 기본 방향(우측)을 반환합니다.
    /// </summary>
    private Vector3 FindNearestEnemyDirection(Vector3 origin)
    {
        float searchRadius = _data.enemySearchRadius > 0 ? _data.enemySearchRadius : 20f;

        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, searchRadius, _hitBuffer, _enemyLayerMask);
        if (hitCount == 0) return Vector3.right;

        // 가장 가까운 적 찾기
        Enemy nearest = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            if (_hitBuffer[i] != null && _hitBuffer[i].TryGetComponent<Enemy>(out var enemy))
            {
                if (enemy == null || enemy.CurrentHP <= 0) continue;

                float distance = Vector3.SqrMagnitude(origin - enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = enemy;
                }
            }
        }

        if (nearest != null)
        {
            return (nearest.transform.position - origin).normalized;
        }

        return Vector3.right;
    }

    /// <summary>
    /// 투사체를 생성합니다. (통합 메서드 - 코드 중복 제거)
    /// </summary>
    /// <param name="direction">발사 방향</param>
    /// <param name="orbitIndex">공전 인덱스 (-1이면 공전 안 함, 0+이면 공전 모드)</param>
    private void SpawnProjectile(Vector3 direction, int orbitIndex)
    {
        if (_data.prefab == null) return;

        float damage = GetFinalDamage();
        float speed = _data.projectileSpeed * GetSpeedMultiplier();
        float size = GetSizeMultiplier();

        Vector3 spawnPos = _playerTransform != null ? _playerTransform.position : transform.position;

        // 오브젝트 풀링 사용
        Projectile projectile = null;

        if (ObjectPoolManager.Instance != null)
        {
            projectile = ObjectPoolManager.Instance.GetProjectile();
        }

        if (projectile == null)
        {
            GameObject projObj = Instantiate(_data.prefab, spawnPos, Quaternion.identity);
            projectile = projObj.GetComponent<Projectile>();

            if (projectile == null)
            {
                projectile = projObj.AddComponent<Projectile>();
            }
        }
        else
        {
            projectile.gameObject.SetActive(true);
            projectile.gameObject.transform.position = spawnPos;
        }

        // 크기 조절
        projectile.transform.localScale = Vector3.one * size;

        // 데이터에서 이동 방식 가져와서 투사체 초기화
        projectile.Setup(direction, damage, speed, _data.movementType);

        // [v2] 관통 효과 전달
        if (_data.penetrationCount > 0)
        {
            projectile.SetPenetration(_data.penetrationCount);
        }

        // [v2] 생존 시간 오버라이드 전달
        if (_data.projectileLifetime > 0)
        {
            projectile.SetLifetime(_data.projectileLifetime);
        }

        // 시각 효과 설정 (v2) - SkillData에서 스프라이트, 색상, 크기 적용
        projectile.SetSprite(_data.projectileSprite, _data.projectileColor, _data.projectileScale * size);

        // 공전 인덱스 설정 (공전 모드일 때만)
        if (orbitIndex >= 0)
        {
            projectile.SetOrbitIndex(orbitIndex);
        }
    }
    #endregion
}
