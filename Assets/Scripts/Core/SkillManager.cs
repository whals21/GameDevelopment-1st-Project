using System.Linq;
using System.Security.Principal;
using UnityEngine;

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

    // Forcefield 관련
    private int forcefieldSlot = -1;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        SetupSingleton();
        InitializeArrays();
    }

    void Start()
    {
        // ForcefieldManager 초기화
        if (ForcefieldManager.Instance == null)
        {
            GameObject forcefieldManagerObj = new GameObject("ForcefieldManager");
            forcefieldManagerObj.AddComponent<ForcefieldManager>();
        }

        EquipTestSkills();
        UpdateForcefieldSkills();
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

        // 기존 Forcefield 스킬 제거
        if (forcefieldSlot >= 0 && forcefieldSlot < MAX_SKILLS && equippedSkills[forcefieldSlot] != null)
        {
            if (equippedSkills[forcefieldSlot].name.Contains("Forcefield"))
            {
                //ForcefieldManager.Instance.UnEquipForcefieldSkill();
                forcefieldSlot = -1;
            }
        }

        equippedSkills[slot] = skill;
        skillLevels[slot] = Mathf.Max(1, level);
        cooldownTimers[slot] = 0f;

        // Forcefield 스킬 장착 
        if (skill.name.Contains("Forcefield"))
        {
            forcefieldSlot = slot;
            ForcefieldManager.Instance.EquipForcefieldSkill(skill, level);
        }
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

            // Forcefield 스킬인 경우 ForcefieldManager에 업데이트
            if (slot == forcefieldSlot && ForcefieldManager.Instance != null)
            {
                ForcefieldManager.Instance.EquipForcefieldSkill(equippedSkills[slot], skillLevels[slot]);
            }
            Debug.Log($"슬롯 {slot}: 스킬 레벨을 {level}로 설정");
        }
    }
    #endregion

    #region Forcefield Management
    private void UpdateForcefieldSkills()
    {
        // 스킬에 Forcefield가 있는지 확인
        for (int i = 0; i < MAX_SKILLS; i++)
        {
            if (HasSkill(i) && equippedSkills[i].name.Contains("Forcefield") ||
                equippedSkills[i].skillName.Contains("Forcefield"))
            {
                forcefieldSlot = i;
                ForcefieldManager.Instance.EquipForcefieldSkill(equippedSkills[i], skillLevels[i]);
                break;
            }
        }
    }
    #endregion

    #region Combat System
    private void AutoAttackSkills()
    {
        for (int i = 0; i < MAX_SKILLS; i++)
        {
            // Forcefield 스킬은 자동 공격에서 제외
            if (i == forcefieldSlot) continue;

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
        Debug.Log($"ExecuteSkill: 슬롯 {slot}, 스킬 {skill.name}, 레벨 {level}, 발사체 수 {projectileCount}");

        for (int i = 0; i < projectileCount; i++)
        {
            SpawnProjectile(skill, level, target, i);
        }

        // 쿨다운 설정
        float cooldown = GetSkillCooldown(skill, level);
        cooldownTimers[slot] = cooldown;
        Debug.Log($"쿨다운 설정: {cooldown}초");
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

        // 발사체 생성
        if (skill.projectilePrefab.GetComponent<BoomerangProjectile>() != null)
        {
            SpawnBoomerangProjectile(skill, damage, speed, sizeMultiplier);
        }
        else if (skill.projectilePrefab.GetComponent<MolotovProjectile>() != null)
        {
            SpawnMolotovProjectile(skill, damage, level, index);
        }
        else
        {
            // 발사 방향 계산
            Vector2 direction = CalculateProjectileDirection(target, skill, level, index);
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

      private void SpawnBoomerangProjectile(SkillData skill, float damage, float speed, float sizeMultiplier)
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

        // 방향은 계산해서 전달
        Vector2 direction = Vector2.right; // 기본 오른쪽
        boomerang.Init(damage, speed, direction);
    }

    private void SpawnMolotovProjectile(SkillData skill, float damage, int level, int index)
    {
        // Molotov 오브젝트 풀에서 가져오기
        var molotov = ObjectPoolManager.Instance.GetMolotov();
        if (molotov == null)
        {
            // 풀이 비어있으면 새로 생성
            molotov = Instantiate(skill.projectilePrefab).GetComponent<MolotovProjectile>();
        }
        else
        {
            molotov.gameObject.SetActive(true);
            molotov.transform.position = transform.position;
        }

        // 목표 위치 계산 (플레이어 주변 랜덤 또는 균등 분배)
        Vector3 targetPos = CalculateMolotovTargetPosition(level, index);

        // Molotov 초기화
        molotov.InitMolotov(damage, targetPos);
    }

    private Vector3 CalculateMolotovTargetPosition(int level, int index)
    {
        // 레벨에 따른 발사 수
        int molotovCount = 2 + (level - 1); // 레벨 1: 2개, 레벨 2: 3개, ...

        if (molotovCount == 1)
        {
            // 가장 가까운 적 위치
            var nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                return nearestEnemy.position;
            }
            return transform.position + Vector3.forward * 5f;
        }

        // 원형으로 분배
        float angleStep = 360f / molotovCount;
        float angle = angleStep * index;
        float radius = 5f; // 플레이어로부터의 거리

        Vector3 offset = Quaternion.Euler(0, 0, angle) * Vector3.right * radius;
        return transform.position + offset;
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
            // Forcefield 스킬은 쿨다운 적용 안함
            if (i == forcefieldSlot) continue;

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
        if (skill == null) return 1;

        // Molotov 특별 처리: 레벨에 따라 2, 3, 4, 5, 6개
        if (skill.projectilePrefab != null && skill.projectilePrefab.GetComponent<MolotovProjectile>() != null)
        {
            return Mathf.Max(2, 1 + level); // 레벨 1: 2개, 레벨 2: 3개, ...
        }

        // 일반 스킬 처리
        if (level <= 1) return skill.projectileCount;

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