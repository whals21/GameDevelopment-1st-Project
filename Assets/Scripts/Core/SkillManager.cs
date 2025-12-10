using UnityEngine;
using System.Linq;

public class SkillManager : MonoBehaviour
{
    #region Constants
    private const int MAX_SKILLS = 5;
    private const float ENEMY_CACHE_INTERVAL = 0.5f;
    #endregion

    #region Singleton
    public static SkillManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [Header("스킬 설정")]
    [SerializeField] private SkillData[] testSkills;

    [Header("테스트용 스킬 레벨 설정")]
    [SerializeField] private int[] testSkillLevels = new int[5];
    #endregion

    #region Private Fields
    private SkillData[] equippedSkills;
    private int[] skillLevels;
    private float[] cooldownTimers;

    // 최적화: 적 리스트 캐싱
    private Enemy[] cachedEnemies;
    private float enemyCacheTimer;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        SetupSingleton();
        InitializeArrays();
    }

    void Start()
    {
        EquipTestSkills();
    }

    void Update()
    {
        UpdateCooldowns();
        AutoAttackSkills();
        UpdateEnemyCache();
    }
    #endregion

    #region Initialization
    private void SetupSingleton()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void InitializeArrays()
    {
        equippedSkills = new SkillData[MAX_SKILLS];
        skillLevels = new int[MAX_SKILLS];
        cooldownTimers = new float[MAX_SKILLS];
    }

    private void EquipTestSkills()
    {
        if (testSkills == null) return;

        for (int i = 0; i < Mathf.Min(testSkills.Length, MAX_SKILLS); i++)
        {
            if (testSkills[i] != null)
            {
                // 테스트 스킬 레벨 설정 (인스펙터에서 직접 조정 가능)
                int level = (i < testSkillLevels.Length) ? Mathf.Max(1, testSkillLevels[i]) : 1;
                EquipSkill(i, testSkills[i], level);
            }
        }
    }
    #endregion

    #region Public API
    public void EquipSkill(int slot, SkillData skill, int level = 1)
    {
        if (!IsValidSlot(slot) || skill == null) return;

        equippedSkills[slot] = skill;
        skillLevels[slot] = Mathf.Max(1, level);
        cooldownTimers[slot] = 0f;
    }

    public int GetSkillLevel(int slot) => IsValidSlot(slot) ? skillLevels[slot] : 0;
    public bool HasSkill(int slot) => IsValidSlot(slot) && equippedSkills[slot] != null;
    public int GetEquippedSkillCount() => equippedSkills.Count(skill => skill != null);

    // 런타임에 스킬 레벨 설정 (인스펙터에서 테스트용)
    [ContextMenu("Update Skill Levels From Inspector")]
    public void UpdateSkillLevelsFromInspector()
    {
        for (int i = 0; i < MAX_SKILLS && i < testSkillLevels.Length; i++)
        {
            if (HasSkill(i) && testSkillLevels[i] != skillLevels[i])
            {
                skillLevels[i] = Mathf.Max(1, testSkillLevels[i]);
                Debug.Log($"슬롯 {i}: 스킬 레벨을 {skillLevels[i]}로 업데이트");
            }
        }
    }

    // 런타임에 특정 스킬 레벨 설정
    public void SetSkillLevel(int slot, int level)
    {
        if (HasSkill(slot))
        {
            skillLevels[slot] = Mathf.Max(1, level);
            Debug.Log($"슬롯 {slot}: 스킬 레벨을 {level}로 설정");
        }
    }
    #endregion

    #region Combat System
    private void AutoAttackSkills()
    {
        for (int i = 0; i < MAX_SKILLS; i++)
        {
            if (HasSkill(i) && cooldownTimers[i] <= 0f)
            {
                ExecuteSkill(i);
            }
        }
    }

    private void ExecuteSkill(int slot)
    {
        var skill = equippedSkills[slot];
        var level = skillLevels[slot];
        var target = FindNearestEnemy();

        if (target == null) return;

        // 발사체 생성
        int projectileCount = GetProjectileCount(skill, level);
        for (int i = 0; i < projectileCount; i++)
        {
            SpawnProjectile(skill, level, target, i);
        }

        // 쿨다운 설정
        cooldownTimers[slot] = GetSkillCooldown(skill, level);
    }

    private Transform FindNearestEnemy()
    {
        if (cachedEnemies == null || cachedEnemies.Length == 0) return null;

        Transform nearest = null;
        float minDistance = float.MaxValue;

        foreach (var enemy in cachedEnemies)
        {
            if (enemy == null || enemy.CurrentHP <= 0) continue;

            float distance = Vector3.SqrMagnitude(transform.position - enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    private void UpdateEnemyCache()
    {
        enemyCacheTimer += Time.deltaTime;
        if (enemyCacheTimer >= ENEMY_CACHE_INTERVAL)
        {
            enemyCacheTimer = 0f;
            cachedEnemies = FindObjectsOfType<Enemy>();
        }
    }
    #endregion

    #region Projectile Spawning
    private void SpawnProjectile(SkillData skill, int level, Transform target, int index)
    {
        if (skill?.projectilePrefab == null) return;

        // 스탯 계산
        float damage = skill.damage * GetDamageMultiplier(skill, level);
        float speed = skill.projectileSpeed * GetProjectileSpeedMultiplier(skill, level);
        float sizeMultiplier = GetProjectileSizeMultiplier(skill, level);

        // 발사 방향 계산
        Vector2 direction = CalculateProjectileDirection(target, skill, level, index);

        // 발사체 생성
        if (skill.projectilePrefab.GetComponent<BoomerangProjectile>() != null)
        {
            SpawnSpecialProjectile(skill, damage, speed, direction, sizeMultiplier);
        }
        else
        {
            SpawnRegularProjectile(skill, damage, speed, direction, sizeMultiplier);
        }
    }

    private void SpawnRegularProjectile(SkillData skill, float damage, float speed, Vector2 direction, float sizeMultiplier)
    {
        var projectile = ObjectPoolManager.Instance.GetProjectile();
        if (projectile == null)
        {
            projectile = Instantiate(skill.projectilePrefab).GetComponent<Projectile>();
        }
        else
        {
            projectile.gameObject.SetActive(true);
            projectile.gameObject.transform.position = transform.position;
        }

        // 크기 조절
        projectile.gameObject.transform.localScale = Vector3.one * sizeMultiplier;

        projectile.Init(damage, speed, direction);
    }

    private void SpawnSpecialProjectile(SkillData skill, float damage, float speed, Vector2 direction, float sizeMultiplier)
    {
        // 부메랑도 오브젝트 풀에서 가져오기
        var boomerang = ObjectPoolManager.Instance.GetBoomerang();
        if (boomerang == null)
        {
            // 풀이 비어있으면 새로 생성
            boomerang = Instantiate(skill.projectilePrefab).GetComponent<BoomerangProjectile>();
        }
        else
        {
            boomerang.gameObject.SetActive(true);
            boomerang.transform.position = transform.position;
        }

        // 크기 조절
        boomerang.transform.localScale = Vector3.one * sizeMultiplier;

        boomerang.Init(damage, speed, direction);
    }

    private Vector2 CalculateProjectileDirection(Transform target, SkillData skill, int level, int index)
    {
        Vector2 baseDirection = (target.position - transform.position).normalized;
        int projectileCount = GetProjectileCount(skill, level);

        if (projectileCount <= 1) return baseDirection;

        // 확산 각도 계산
        float totalAngle = 30f;
        float angleStep = totalAngle / (projectileCount - 1);
        float startAngle = -totalAngle * 0.5f;
        float currentAngle = startAngle + (angleStep * index);

        return Quaternion.Euler(0, 0, currentAngle) * baseDirection;
    }
    #endregion

    #region Cooldown Management
    private void UpdateCooldowns()
    {
        for (int i = 0; i < MAX_SKILLS; i++)
        {
            if (cooldownTimers[i] > 0f)
            {
                cooldownTimers[i] = Mathf.Max(0f, cooldownTimers[i] - Time.deltaTime);
            }
        }
    }
    #endregion

    #region Stats Calculation
    private int GetProjectileCount(SkillData skill, int level)
    {
        if (skill == null || level <= 1) return 1;

        var levelData = GetLevelData(skill, level);
        return skill.projectileCount + (levelData?.additionalProjectiles ?? 0);
    }

    private float GetSkillCooldown(SkillData skill, int level)
    {
        if (skill == null) return 1f;
        return skill.cooldown * GetCooldownMultiplier(skill, level);
    }

    private float GetDamageMultiplier(SkillData skill, int level)
    {
        return GetLevelData(skill, level)?.damageMultiplier ?? 1f;
    }

    private float GetCooldownMultiplier(SkillData skill, int level)
    {
        return GetLevelData(skill, level)?.cooldownMultiplier ?? 1f;
    }

    private float GetProjectileSpeedMultiplier(SkillData skill, int level)
    {
        return GetLevelData(skill, level)?.projectileSpeedMultiplier ?? 1f;
    }

    private float GetProjectileSizeMultiplier(SkillData skill, int level)
    {
        var levelData = GetLevelData(skill, level);
        if (levelData == null) return 1f;

        // 기본 크기 배수에 누적 증가량 더하기
        return levelData.projectileSizeMultiplier + GetCumulativeScaleIncrease(skill, level);
    }

    private float GetCumulativeScaleIncrease(SkillData skill, int currentLevel)
    {
        if (skill?.levels == null) return 0f;

        float totalIncrease = 0f;
        // 현재 레벨까지의 모든 크기 증가량 누적
        for (int i = 1; i <= currentLevel && i <= skill.levels.Length; i++)
        {
            totalIncrease += skill.levels[i - 1].projectileScaleIncrease;
        }

        return totalIncrease;
    }

    private SkillLevel GetLevelData(SkillData skill, int level)
    {
        if (skill?.levels == null || level <= 0 || level > skill.levels.Length)
            return null;

        return skill.levels[level - 1];
    }
    #endregion

    #region Utility
    private bool IsValidSlot(int slot) => slot >= 0 && slot < MAX_SKILLS;
    #endregion
}