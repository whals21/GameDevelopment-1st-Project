using UnityEngine;
using System.Linq;
using System.Collections.Specialized;
using System.Security.Cryptography;

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
    private int forcefieldSlot = -1; // Forcefield가 장착된 슬롯
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
        UpdateForcefieldSkills(); // Forcefield 스킬들 확인 및 활성화
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

        // 기존 Forcefield 스킬 해제
        if (forcefieldSlot >= 0 && forcefieldSlot < MAX_SKILLS && equippedSkills[forcefieldSlot] != null)
        {
            if (equippedSkills[forcefieldSlot].name.Contains("Forcefield"))
            {
                ForcefieldManager.Instance.UnequipForcefield();
                forcefieldSlot = -1;
            }
        }

        equippedSkills[slot] = skill;
        skillLevels[slot] = Mathf.Max(1, level);
        cooldownTimers[slot] = 0f;

        // Forcefield 스킬 확인 및 장착
        if (skill.name.Contains("Forcefield") || skill.skillName.Contains("Forcefield"))
        {
            forcefieldSlot = slot;
            ForcefieldManager.Instance.EquipForcefield(skill, level);
        }

        Debug.Log($"슬롯 {slot}: {skill.name} (레벨 {level}) 장착 완료");
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

            // Forcefield 스킬인 경우 ForcefieldManager도 업데이트
            if (slot == forcefieldSlot && ForcefieldManager.Instance != null)
            {
                ForcefieldManager.Instance.UpdateForcefieldLevel(level);
            }

            Debug.Log($"슬롯 {slot}: 스킬 레벨을 {level}로 설정");
        }
    }
    #endregion

    #region Forcefield Management
    private void UpdateForcefieldSkills()
    {
        // 모든 스킬을 확인하여 Forcefield가 있는지 체크
        for (int i = 0; i < MAX_SKILLS; i++)
        {
            if (HasSkill(i) && equippedSkills[i] != null)
            {
                if (equippedSkills[i].name.Contains("Forcefield") || equippedSkills[i].skillName.Contains("Forcefield"))
                {
                    forcefieldSlot = i;
                    ForcefieldManager.Instance.EquipForcefield(equippedSkills[i], skillLevels[i]);
                    break;
                }
            }
        }
    }
    #endregion

    #region Combat System
    private void AutoAttackSkills()
    {
        for (int i = 0; i < MAX_SKILLS; i++)
        {
            if (!HasSkill(i)) continue;

            // Forcefield는 쿨타임이 없으므로 건너뛰기
            if (i == forcefieldSlot) continue;

            // Guardian과 Forcefield는 지속적인 스킬이므로 쿨다운 체크 조정
            var skill = equippedSkills[i];
            if (skill.skillType == SkillType.Guardian || skill.skillType == SkillType.Forcefield)
            {
                // 지속 스킬은 첫 실행 이후에는 주기적으로 체크하지 않음
                if (cooldownTimers[i] > 0f) continue;
            }

            if (cooldownTimers[i] <= 0f)
            {
                ExecuteSkill(i);
            }
        }
    }

    private void ExecuteSkill(int slot)
    {
        var skill = equippedSkills[slot];
        var level = skillLevels[slot];

        // 스킬 타입에 따른 분기 처리
        switch (skill.skillType)
        {
            case SkillType.Projectile:
                ExecuteProjectileSkill(skill, level, slot);
                break;

            case SkillType.Guardian:
                ExecuteGuardianSkill(skill, level, slot);
                break;

            case SkillType.Forcefield:
                ExecuteForcefieldSkill(skill, level, slot);
                break;

            case SkillType.Special:
                ExecuteSpecialSkill(skill, level, slot);
                break;

            default:
                Debug.LogWarning($"알 수 없는 스킬 타입: {skill.skillType}");
                break;
        }
    }

    private void ExecuteProjectileSkill(SkillData skill, int level, int slot)
    {
        var target = FindNearestEnemy();
        if (target == null) return;

        // 발사체 생성
        int projectileCount = GetProjectileCount(skill, level);
        Debug.Log($"ExecuteProjectileSkill: 슬롯 {slot}, 스킬 {skill.name}, 레벨 {level}, 발사체 수 {projectileCount}");

        for (int i = 0; i < projectileCount; i++)
        {
            SpawnProjectile(skill, level, target, i);
        }

        // 쿨다운 설정
        float cooldown = GetSkillCooldown(skill, level);
        cooldownTimers[slot] = cooldown;
        Debug.Log($"쿨다운 설정: {cooldown}초");
    }

    private void ExecuteGuardianSkill(SkillData skill, int level, int slot)
    {
        Debug.Log($"ExecuteGuardianSkill: 슬롯 {slot}, 스킬 {skill.name}, 레벨 {level}");

        // 가디언 스킬 컴포넌트 찾기 또는 생성
        GameObject guardianObj = GameObject.Find(skill.skillName);
        GuardianSkill guardianSkill = null;

        if (guardianObj != null)
        {
            guardianSkill = guardianObj.GetComponent<GuardianSkill>();
        }
        else
        {
            // 새로운 가디언 오브젝트 생성
            if (skill.skillObjectPrefab != null)
            {
                guardianObj = Instantiate(skill.skillObjectPrefab, transform);
                guardianObj.name = skill.skillName;
                guardianSkill = guardianObj.GetComponent<GuardianSkill>();
            }
        }

        if (guardianSkill != null)
        {
            // 레벨 설정
            guardianSkill.SetLevel(level);

            // 활성화 (이미 활성화되어 있으면 레벨업만)
            if (!guardianSkill.IsGuardianActive())
            {
                guardianSkill.ActivateGuardian();
            }
            else
            {
                // 이미 활성화된 상태면 레벨업 처리
                guardianSkill.UpgradeGuardian();
            }
        }
        else
        {
            Debug.LogError($"가디언 스킬을 찾을 수 없습니다: {skill.skillName}");
        }

        // Guardian은 지속시간이 있으므로 쿨다운이 다름
        // 일단 기본 쿨다운 적용 (필요시 조정)
        float cooldown = GetSkillCooldown(skill, level);
        cooldownTimers[slot] = cooldown;
    }

    private void ExecuteForcefieldSkill(SkillData skill, int level, int slot)
    {
        // 기존 ForcefieldManager와 연동 (이미 구현되어 있을 것)
        Debug.Log($"ExecuteForcefieldSkill: 슬롯 {slot}, 스킬 {skill.name}, 레벨 {level}");

        // ForcefieldManager로 위임
        if (ForcefieldManager.Instance != null)
        {
            ForcefieldManager.Instance.EquipForcefield(skill, level);
        }
    }

    private void ExecuteSpecialSkill(SkillData skill, int level, int slot)
    {
        // 특수 스킬 처리 (확장용)
        Debug.Log($"ExecuteSpecialSkill: 슬롯 {slot}, 스킬 {skill.name}, 레벨 {level}");

        // 특수 스킬에 따른 개별 처리
        if (skill.skillObjectPrefab != null)
        {
            GameObject specialObj = Instantiate(skill.skillObjectPrefab);
            // 스킬별 특수 초기화 로직
        }
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
        else if (skill.projectilePrefab.GetComponent<BrickProjectile>() != null)
        {
            SpawnBrickProjectile(skill, damage, speed, index);
        }
        else if (skill.projectilePrefab.GetComponent<SoccerBallProjectile>() != null)
        {
            SpawnSoccerBallProjectile(skill, damage, speed, index);
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
        Transform target = FindNearestEnemy();
        Vector2 direction = CalculateProjectileDirection(target, skill, GetCurrentSkillLevel(skill), 0);

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

    private void SpawnBrickProjectile(SkillData skill, float damage, float speed, int index)
    {
        // Brick 오브젝트 풀에서 가져오기
        var brick = ObjectPoolManager.Instance.GetBrick();
        if (brick == null)
        {
            // 풀이 비어있으면 새로 생성
            brick = Instantiate(skill.projectilePrefab).GetComponent<BrickProjectile>();
        }
        else
        {
            brick.gameObject.SetActive(true);
            brick.transform.position = transform.position;
        }

        // Brick 발사 방향 계산 (플레이어 위쪽으로 원형 분산)
        int projectileCount = GetProjectileCount(skill, GetCurrentSkillLevel(skill));
        Vector2 direction;

        // 위쪽 중심으로 원형 분산 발사
        float angleStep = 360f / projectileCount; // 360도 등분
        float angle = angleStep * index;

        // 약간의 무작위성 추가로 자연스러움
        angle += Random.Range(-10f, 10f);

        // 위쪽(90도)을 기준으로 분산
        direction = Quaternion.Euler(0, 0, 90f + angle) * Vector2.right;

        // Brick 초기화
        brick.InitBrick(damage, speed, direction);
    }

    private int GetCurrentSkillLevel(SkillData skill)
    {
        for (int i = 0; i < equippedSkills.Length; i++)
        {
            if (equippedSkills[i] == skill)
            {
                return skillLevels[i];
            }
        }
        return 1; // 기본값
    }

    private Vector2 CalculateBrickDirection(int index, int totalCount)
    {
        // Brick을 원형으로 균등 분배
        float angleStep = 360f / totalCount;
        float angle = angleStep * index;

        // 무작위 방향으로 약간 변화 추가 (자연스러움)
        angle += Random.Range(-15f, 15f);

        return Quaternion.Euler(0, 0, angle) * Vector2.right;
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

    private void SpawnSoccerBallProjectile(SkillData skill, float damage, float speed, int index)
    {
        // SoccerBall 오브젝트 풀에서 가져오기
        var soccerBall = ObjectPoolManager.Instance.GetSoccerBall();

        if (soccerBall == null)
        {
            // 풀이 비어있으면 새로 생성
            soccerBall = Instantiate(skill.projectilePrefab).GetComponent<SoccerBallProjectile>();
        }
        else
        {
            soccerBall.gameObject.SetActive(true);
            soccerBall.transform.position = transform.position;
        }

        // 가장 가까운 적 찾기
        Transform nearestEnemy = FindNearestEnemy();
        Vector2 direction;

        if (nearestEnemy != null)
        {
            direction = CalculateProjectileDirection(nearestEnemy, skill, GetCurrentSkillLevel(skill), 0);
        }
        else
        {
            // 적이 없으면 랜덤 방향
            float randomAngle = Random.Range(0f, 360f);
            direction = Quaternion.Euler(0, 0, randomAngle) * Vector2.right;
        }

        soccerBall.InitSoccerBall(damage, speed, direction);
    }
    #endregion

    #region Cooldown Management
    private void UpdateCooldowns()
    {
        for (int i = 0; i < MAX_SKILLS; i++)
        {
            // Forcefield는 쿨타임이 없으므로 건너뛰기
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
        if (skill == null) return 3f; // 기본 3초

        // 임시방편: Molotov는 항상 3초 쿨다운
        if (skill.projectilePrefab != null && skill.projectilePrefab.GetComponent<MolotovProjectile>() != null)
        {
            Debug.Log($"Molotov 쿨다운: 3초");
            return 3f;
        }

        float cooldown = skill.cooldown * GetCooldownMultiplier(skill, level);
        Debug.Log($"일반 스킬 쿨다운: {cooldown}초 (기본: {skill.cooldown}, 배수: {GetCooldownMultiplier(skill, level)})");

        // 최소 1초 보장
        return Mathf.Max(1f, cooldown);
    }

    private float GetDamageMultiplier(SkillData skill, int level)
    {
        return GetLevelData(skill, level)?.damageMultiplier ?? 1f;
    }

    private float GetCooldownMultiplier(SkillData skill, int level)
    {
        var levelData = GetLevelData(skill, level);
        float multiplier = levelData?.cooldownMultiplier ?? 1f;

        // 0이면 1로 대체 (쿨다운이 0초가 되는 것 방지)
        if (multiplier <= 0) multiplier = 1f;

        return multiplier;
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