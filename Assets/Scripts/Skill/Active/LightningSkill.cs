using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LightningSkill : MonoBehaviour
{
    #region Serialized Fields
    [Header("스킬 설정")]
    [SerializeField] private SkillData skillData;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private bool isActive = true;

    [Header("밸런스 수치")]
    [SerializeField] private float baseCooldown = 3f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private int baseLightningCount = 1;
    [SerializeField] private float cooldownReductionPerLevel = 0.2f;
    [SerializeField] private float damageIncreasePerLevel = 2.5f;

    [Header("타겟팅")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float strikeDelay = 0.15f;
    [SerializeField] private float attackRange = 15f; 
    #endregion

    #region Private Fields
    private float currentCooldown;
    private int lightningCount;
    private float damage;
    private float cooldownTime;
    private Enemy[] cachedEnemies;
    private float enemyCacheTimer;
    private const float ENEMY_CACHE_INTERVAL = 0.5f;
    #endregion

    #region Properties
    public int CurrentLevel => currentLevel;
    public bool IsActive => isActive;
    public SkillData SkillData => skillData;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 기본값 설정
        if (skillData == null)
        {
            Debug.LogWarning("LightningSkill: SkillData가 설정되지 않았습니다!");
            isActive = false;
            return;
        }
    }

    private void Start()
    {
        // 스킬 데이터에서 값 가져오기
        LoadSkillData();

        // 적 리스트 캐싱 시작
        enemyCacheTimer = 0f;
    }

    private void Update()
    {
        if (!isActive) return;

        // 쿨타임 업데이트
        if (currentCooldown > 0f)
        {
            currentCooldown -= Time.deltaTime;
        }

        // 자동 발동 체크
        if (currentCooldown <= 0f)
        {
            TryActivateLightning();
        }

        // 적 캐시 업데이트
        UpdateEnemyCache();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 스킬 데이터 설정
    /// </summary>
    public void SetSkillData(SkillData data)
    {
        skillData = data;
        if (data != null)
        {
            LoadSkillData();
            isActive = true;
        }
        else
        {
            isActive = false;
        }
    }

    /// <summary>
    /// 스킬 레벨 설정
    /// </summary>
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        LoadSkillData();
    }

    /// <summary>
    /// 스킬 활성화/비활성화
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            currentCooldown = 0f; // 비활성화 시 쿨타임 초기화
        }
    }

    /// <summary>
    /// 수동 발동
    /// </summary>
    public void ManualActivate()
    {
        if (isActive && currentCooldown <= 0f)
        {
            ActivateLightning();
        }
    }

    /// <summary>
    /// 쿨타임 초기화
    /// </summary>
    public void ResetCooldown()
    {
        currentCooldown = 0f;
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 스킬 데이터에서 값 로드
    /// </summary>
    private void LoadSkillData()
    {
        if (skillData == null)
        {
            Debug.LogWarning("LightningSkill: SkillData가 null입니다!");
            return;
        }

        // 레벨별 데이터 가져오기
        var levelData = GetLevelData(currentLevel);
        if (levelData != null)
        {
            damage = baseDamage * levelData.damageMultiplier;
            cooldownTime = baseCooldown * levelData.cooldownMultiplier;
            lightningCount = baseLightningCount + levelData.additionalProjectiles;
        }
        else
        {
            // 기본값으로 설정
            damage = baseDamage + (damageIncreasePerLevel * (currentLevel - 1));
            cooldownTime = baseCooldown - (cooldownReductionPerLevel * (currentLevel - 1));
            lightningCount = baseLightningCount + (currentLevel - 1);
        }

        // 최소/최대값 제한
        lightningCount = Mathf.Clamp(lightningCount, 1, 10);
        cooldownTime = Mathf.Max(cooldownTime, 0.5f);
        damage = Mathf.Max(damage, 1f);
    }

    /// <summary>
    /// 레벨 데이터 가져오기
    /// </summary>
    private SkillLevel GetLevelData(int level)
    {
        if (skillData == null || skillData.levels == null || level <= 0 || level > skillData.levels.Length)
        {
            return null;
        }
        return skillData.levels[level - 1];
    }

    /// <summary>
    /// 번개 발동 시도
    /// </summary>
    private void TryActivateLightning()
    {
        Enemy[] availableEnemies = GetAvailableEnemies();
        if (availableEnemies.Length > 0)
        {
            ActivateLightning();
        }
    }

    /// <summary>
    /// 번개 발동
    /// </summary>
    private void ActivateLightning()
    {
        Enemy[] availableEnemies = GetAvailableEnemies();
        if (availableEnemies.Length == 0) return;

        // 목표 적 선택
        Enemy[] targets = SelectTargets(availableEnemies);
        if (targets.Length == 0) return;

        // 번개 생성
        StartCoroutine(SpawnLightningSequence(targets));

        // 쿨타임 설정
        currentCooldown = cooldownTime;
    }

    /// <summary>
    /// 번개 생성 시퀀스
    /// </summary>
    private IEnumerator SpawnLightningSequence(Enemy[] targets)
    {
        int strikesToSpawn = Mathf.Min(lightningCount, targets.Length);

        for (int i = 0; i < strikesToSpawn; i++)
        {
            if (targets[i] == null || !targets[i].gameObject.activeInHierarchy)
            {
                // 비활성화된 적이면 다른 적 찾기
                targets[i] = GetRandomEnemy();
                if (targets[i] == null) continue;
            }

            // 번개 생성
            SpawnLightningStrike(targets[i]);

            // 다음 번개까지의 지연
            if (i < strikesToSpawn - 1)
            {
                yield return new WaitForSeconds(strikeDelay);
            }
        }
    }

    /// <summary>
    /// 개별 번개 생성
    /// </summary>
    private void SpawnLightningStrike(Enemy target)
    {
        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogError("LightningSkill: ObjectPoolManager를 찾을 수 없습니다!");
            return;
        }

        LightningStrike lightning = ObjectPoolManager.Instance.GetLightning();
        if (lightning != null)
        {
            // 타겟 설정 (OnEnable()에서 모든 초기화 및 애니메이션 시작)
            lightning.SetTarget(target, damage);
        }
        else
        {
            Debug.LogError("LightningSkill: LightningStrike를 풀에서 가져올 수 없습니다!");
        }
    }

    /// <summary>
    /// 목표 적 선택
    /// </summary>
    private Enemy[] SelectTargets(Enemy[] availableEnemies)
    {
        List<Enemy> targets = new List<Enemy>();

        // 모든 적 중에서 무작위로 선택
        System.Random random = new System.Random();
        Enemy[] shuffledEnemies = availableEnemies.OrderBy(x => random.Next()).ToArray();

        int actualStrikes = Mathf.Min(lightningCount, shuffledEnemies.Length);
        for (int i = 0; i < actualStrikes; i++)
        {
            targets.Add(shuffledEnemies[i]);
        }

        return targets.ToArray();
    }

    /// <summary>
    /// 사용 가능한 적 목록 가져오기
    /// </summary>
    private Enemy[] GetAvailableEnemies()
    {
        if (cachedEnemies == null)
        {
            UpdateEnemyCache();
        }
        return cachedEnemies ?? new Enemy[0];
    }

    /// <summary>
    /// 무작위 적 가져오기
    /// </summary>
    private Enemy GetRandomEnemy()
    {
        Enemy[] enemies = GetAvailableEnemies();
        if (enemies.Length == 0) return null;

        int randomIndex = Random.Range(0, enemies.Length);
        return enemies[randomIndex];
    }

    /// <summary>
    /// 적 캐시 업데이트 (사정거리 제한 적용)
    /// </summary>
    private void UpdateEnemyCache()
    {
        enemyCacheTimer += Time.deltaTime;
        if (enemyCacheTimer >= ENEMY_CACHE_INTERVAL)
        {
            enemyCacheTimer = 0f;

            // 플레이어 위치 기준으로 사정거리 내의 적만 탐색
            Vector3 playerPosition = transform.position; // LightningSkill이 플레이어를 따라다닌다고 가정
            cachedEnemies = FindObjectsOfType<Enemy>()
                .Where(e => e.gameObject.activeInHierarchy &&
                       Vector3.Distance(playerPosition, e.transform.position) <= attackRange)
                .ToArray();
        }
    }
    #endregion

    #region Debug
    // private void OnGUI()
    // {
    //     if (!isActive) return;

    //     // 디버그 정보 표시
    //     GUILayout.BeginArea(new Rect(10, 200, 300, 180));
    //     GUILayout.Label($"Lightning Skill Lv.{currentLevel}");
    //     GUILayout.Label($"Damage: {damage:F1}");
    //     GUILayout.Label($"Lightning Count: {lightningCount}");
    //     GUILayout.Label($"Attack Range: {attackRange}m");
    //     GUILayout.Label($"Cooldown: {cooldownTime:F1}s");
    //     GUILayout.Label($"Next Strike: {currentCooldown:F1}s");
    //     GUILayout.Label($"Enemies in Range: {(cachedEnemies?.Length ?? 0)}");
    //     GUILayout.EndArea();
    // }
    #endregion
}