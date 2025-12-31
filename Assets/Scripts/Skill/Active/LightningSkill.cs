using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 번개 스킬 구현
/// 사정거리 내의 무작위 적에게 번개를 떨어뜨립니다.
/// 레벨에 따라 한 번에 떨어뜨리는 번개의 수가 증가합니다.
///
/// 성능 최적화:
/// - PlayerController.Instance로 빠른 플레이어 참조
/// - Physics2D.OverlapCircleNonAlloc로 반경 내 적만 탐색
/// - LayerMask로 불필요한 콜라이더 필터링
/// - 적 캐싱으로 불필요한 탐색 최소화
///
/// 확장성:
/// - 레벨에 따른 번개 수 증가
/// - 데이터 주도 설계 (strikeDelay, lightningCount 등)
/// </summary>
public class LightningSkill : SkillBase
{
    #region Private Fields
    private Transform _playerTransform;
    private int _enemyLayerMask;

    // 적 캐싱 (불필요한 탐색 최소화)
    private List<Enemy> _cachedEnemies = new List<Enemy>();
    private float _enemyCacheTimer;
    private const float ENEMY_CACHE_INTERVAL = 0.5f;
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

        // 초기 적 캐싱
        UpdateEnemyCache();
    }
    #endregion

    #region Core Loop
    public override void UpdateSkill()
    {
        base.UpdateSkill();
        UpdateEnemyCache();
    }

    /// <summary>
    /// 번개 스킬 발동
    /// 레벨에 따라 여러 적에게 번개를 떨어뜨립니다.
    /// </summary>
    protected override void Execute()
    {
        if (_data == null) return;

        // 캐싱된 적 중 무작위 선택
        if (_cachedEnemies == null || _cachedEnemies.Count == 0) return;

        // 번개 수 결정 (레벨에 따라 증가)
        int lightningCount = GetLightningCount();
        int targetsToStrike = Mathf.Min(lightningCount, _cachedEnemies.Count);

        if (targetsToStrike == 0) return;

        // 무작위로 타겟 섞기
        List<Enemy> targets = GetRandomTargets(_cachedEnemies, targetsToStrike);

        // 번개 시퀀스 시작
        StartCoroutine(SpawnLightningSequence(targets));
    }

    /// <summary>
    /// 번개 생성 시퀀스
    /// 여러 번개를 순차적으로 발사합니다.
    /// </summary>
    private IEnumerator SpawnLightningSequence(List<Enemy> targets)
    {
        if (_data == null) yield break;

        float strikeDelay = _data.strikeDelay;

        for (int i = 0; i < targets.Count; i++)
        {
            // 스킬 비활성화 시 중단
            if (!_isActive) yield break;

            if (targets[i] == null || !targets[i].gameObject.activeInHierarchy || targets[i].CurrentHP <= 0)
            {
                continue;
            }

            // 번개 생성
            SpawnLightningStrike(targets[i]);

            // 다음 번개까지의 지연 (마지막은 제외)
            if (i < targets.Count - 1 && strikeDelay > 0f)
            {
                yield return new WaitForSeconds(strikeDelay);
            }
        }
    }

    /// <summary>
    /// 개별 번개 생성
    /// </summary>
    //private void SpawnLightningStrike(Enemy target)
    //{
    //    if (target == null) return;

    //    // 오브젝트 풀링 사용
    //    LightningStrike lightning = null;
    //    if (ObjectPoolManager.Instance != null)
    //    {
    //        lightning = ObjectPoolManager.Instance.GetLightning();
    //    }

    //    if (lightning != null)
    //    {
    //        // v2: 논리와 시각 분리 - 위치만 설정, 데미지는 별도 처리
    //        float damage = GetFinalDamage();
    //        lightning.SetPosition(target.transform.position);
    //        target.TakeDamage(damage);
    //    }
    //    else
    //    {
    //        // 풀이 없으면 직접 데미지 처리 (폴백)
    //        target.TakeDamage(GetFinalDamage());
    //    }
    //}//12/31

    private void SpawnLightningStrike(IDamageable target)
    {
        if (target == null) return;

        // 오브젝트 풀링 처리 (시각 효과)
        LightningStrike lightning = ObjectPoolManager.Instance?.GetLightning();
        lightning?.SetPosition((target as MonoBehaviour)?.transform.position ?? Vector3.zero);

        // 데미지 적용
        target.TakeDamage(GetFinalDamage());
    }
    #endregion

    #region Enemy Targeting
    /// <summary>
    /// 적 캐시 업데이트 (TargetingHelper 사용)
    /// 일정 주기로 플레이어 주변 적만 탐색합니다.
    /// </summary>
    //private void UpdateEnemyCache()12//31
    //{
    //    _enemyCacheTimer += Time.deltaTime;
    //    if (_enemyCacheTimer >= ENEMY_CACHE_INTERVAL)
    //    {
    //        _enemyCacheTimer = 0f;

    //        if (_playerTransform == null) return;

    //        // 사정거리 내의 적만 탐색
    //        float searchRadius = _data.attackRange > 0 ? _data.attackRange : 15f;
    //        Vector3 origin = _playerTransform.position;

    //        // TargetingHelper로 중복 제거 (결과를 바로 _cachedEnemies에 담음)
    //        TargetingHelper.FindAllEnemies(origin, searchRadius, _enemyLayerMask, _cachedEnemies);
    //    }
    //}
    private List<IDamageable> _cachedTargets = new List<IDamageable>();//12/31
    private void UpdateEnemyCache()
    {
        _enemyCacheTimer += Time.deltaTime;
        if (_enemyCacheTimer >= ENEMY_CACHE_INTERVAL)
        {
            _enemyCacheTimer = 0f;

            if (_playerTransform == null) return;

            float searchRadius = _data.attackRange > 0 ? _data.attackRange : 15f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(_playerTransform.position, searchRadius, _enemyLayerMask);

            _cachedTargets.Clear();
            foreach (var col in hits)
            {
                IDamageable damageable = col.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    _cachedTargets.Add(damageable);
                }
            }
        }
    }//12/31
    /// <summary>
    /// 적 리스트에서 무작위로 N명 선택합니다.
    /// In-place sampling으로 GC 할당을 최소화합니다.
    /// </summary>
    private List<Enemy> GetRandomTargets(List<Enemy> enemies, int count)
    {
        List<Enemy> result = new List<Enemy>();
        int poolSize = enemies.Count;
        int targetCount = Mathf.Min(count, poolSize);

        for (int i = 0; i < targetCount; i++)
        {
            // 무작위 인덱스 선택 (poolSize 범위 내)
            int randomIndex = Random.Range(0, poolSize);
            result.Add(enemies[randomIndex]);

            // 선택한 요소를 마지막으로 스왑하고 poolSize 감소
            // 이렇게 하면 이미 선택된 적은 다시 선택되지 않음
            int lastIndex = poolSize - 1;
            Enemy temp = enemies[randomIndex];
            enemies[randomIndex] = enemies[lastIndex];
            enemies[lastIndex] = temp;

            poolSize--;
        }

        return result;
    }
    #endregion

    #region Stats Calculation
    /// <summary>
    /// 레벨에 따른 번개 수를 반환합니다.
    /// 기본값 + 레벨별 추가 개수
    /// </summary>
    private int GetLightningCount()
    {
        int baseCount = _data.projectileCount > 0 ? _data.projectileCount : 1;
        int additionalCount = GetAdditionalProjectiles();
        return Mathf.Max(baseCount + additionalCount, 1);
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
        // 별도 처리 필요 없음
    }
    #endregion
}
