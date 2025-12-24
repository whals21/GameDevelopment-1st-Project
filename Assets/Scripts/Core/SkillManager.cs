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

    [Header("패시브 스킬 테스트")]
    [SerializeField] public PassiveSkillData[] testPassiveSkills;

    [Header("테스트용 패시브 스킬 레벨 설정")]
    [SerializeField] public int[] testPassiveSkillLevels = new int[5];
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

    // Drone 관련
    private DroneSkill droneSkillInstance; // 드론 스킬 인스턴스

    // 패시브 스킬 관련 (외부에서 설정)
    private float globalDamageMultiplier = 1f;     // 전체 데미지 배수
    private float globalCooldownMultiplier = 1f;   // 전체 쿨다운 배수
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

        // 기존 Forcefield 스킬 해제 (단, 같은 슬롯에 같은 스킬이면 건너뜀)
        if (skill.skillType == SkillType.Forcefield)
        {
            if (forcefieldSlot >= 0 && forcefieldSlot < MAX_SKILLS && forcefieldSlot != slot && equippedSkills[forcefieldSlot] != null)
            {
                if (equippedSkills[forcefieldSlot].skillType == SkillType.Forcefield)
                {
                    ForcefieldManager.Instance.UnequipForcefield();
                    forcefieldSlot = -1;
                }
            }
        }

        equippedSkills[slot] = skill;
        skillLevels[slot] = Mathf.Max(1, level);
        cooldownTimers[slot] = 0f;

        // Forcefield 스킬 확인 및 장착 (skillType으로 체크)
        if (skill.skillType == SkillType.Forcefield)
        {
            forcefieldSlot = slot;
            ForcefieldManager.Instance.EquipForcefield(skill, level);
        }
    }

    public int GetSkillLevel(int slot) => IsValidSlot(slot) ? skillLevels[slot] : 0;
    public bool HasSkill(int slot) => IsValidSlot(slot) && equippedSkills[slot] != null;
    public int GetEquippedSkillCount() => equippedSkills.Count(skill => skill != null);

    /// <summary>
    /// 스킬 교체 (진화 시 사용)
    /// 기존 스킬을 제거하고 새 스킬을 같은 슬롯에 장착
    /// </summary>
    /// <param name="oldSkill">제거할 기존 스킬</param>
    /// <param name="newSkill">장착할 새 스킬</param>
    /// <param name="level">새 스킬의 레벨</param>
    public void ReplaceSkill(SkillData oldSkill, SkillData newSkill, int level)
    {
        if (oldSkill == null || newSkill == null) return;

        // 기존 스킬이 장착된 슬롯 찾기
        int slot = -1;
        for (int i = 0; i < MAX_SKILLS; i++)
        {
            if (equippedSkills[i] == oldSkill)
            {
                slot = i;
                break;
            }
        }

        if (slot < 0)
        {
            Debug.LogWarning($"[SkillManager] ReplaceSkill: {oldSkill.skillName}을(를) 찾을 수 없습니다.");
            return;
        }

        // Forcefield 스킬 교체 처리
        if (oldSkill.skillType == SkillType.Forcefield)
        {
            ForcefieldManager.Instance?.UnequipForcefield();
            forcefieldSlot = -1;
        }

        // Guardian/Drone 스킬 오브젝트 정리
        if (oldSkill.skillType == SkillType.Guardian)
        {
            GameObject guardianObj = GameObject.Find(oldSkill.skillName);
            if (guardianObj != null) Destroy(guardianObj);
        }
        else if (oldSkill.skillType == SkillType.Drone && droneSkillInstance != null)
        {
            Destroy(droneSkillInstance.gameObject);
            droneSkillInstance = null;
        }

        // MagneticDart 스킬 오브젝트 정리
        if (oldSkill.skillType == SkillType.Special)
        {
            var magneticDartSkill = FindObjectOfType<MagneticDartSkill>();
            if (magneticDartSkill != null) Destroy(magneticDartSkill.gameObject);
        }

        // 새 스킬 장착
        EquipSkill(slot, newSkill, level);

        Debug.Log($"[SkillManager] {oldSkill.skillName} → {newSkill.skillName} 스킬 교체 완료 (슬롯 {slot})");
    }

    // 런타임에 스킬 레벨 설정 (인스펙터에서 테스트용)
    [ContextMenu("Update Skill Levels From Inspector")]
    public void UpdateSkillLevelsFromInspector()
    {
        for (int i = 0; i < MAX_SKILLS && i < testSkillLevels.Length; i++)
        {
            if (HasSkill(i) && testSkillLevels[i] != skillLevels[i])
            {
                skillLevels[i] = Mathf.Max(1, testSkillLevels[i]);
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
        }
    }
    #endregion

    #region Forcefield Management
    private void UpdateForcefieldSkills()
    {
        // 모든 스킬을 확인하여 Forcefield가 있는지 체크 (skillType으로 확인)
        for (int i = 0; i < MAX_SKILLS; i++)
        {
            if (HasSkill(i) && equippedSkills[i] != null)
            {
                if (equippedSkills[i].skillType == SkillType.Forcefield)
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

            // Guardian, Drone, Special은 지속적인 스킬이므로 쿨다운 체크 조정
            var skill = equippedSkills[i];
            if (skill.skillType == SkillType.Guardian || skill.skillType == SkillType.Drone || skill.skillType == SkillType.Special)
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
        // 슬롯 유효성 검사
        if (!ValidateSkillSlot(slot, out SkillData skill, out int level))
        {
            LogError($"ExecuteSkill: 슬롯 {slot} 유효성 검증 실패");
            return;
        }

        // 스킬 실행 시작 로그
        LogSkill($"ExecuteSkill 시작", skill, level, slot);

        // 스킬 타입에 따른 분기 처리
        bool success = false;
        try
        {
            switch (skill.skillType)
            {
                case SkillType.Projectile:
                    success = ExecuteProjectileSkill(skill, level, slot);
                    break;

                case SkillType.Guardian:
                    success = ExecuteGuardianSkill(skill, level, slot);
                    break;

                // Forcefield는 지속형 스킬로서 ExecuteSkill로 실행되지 않음
                // EquipSkill()에서 ForcefieldManager.EquipForcefield()를 통해 활성화됨
                case SkillType.Forcefield:
                    LogWarning($"ExecuteSkill: Forcefield는 ExecuteSkill로 실행되지 않아야 합니다. [슬롯{slot}]");
                    success = false;
                    break;

                case SkillType.Drone:
                    success = ExecuteDroneSkill(skill, level, slot);
                    break;

                case SkillType.Lightning:
                    success = ExecuteLightningSkill(skill, level, slot);
                    break;

                case SkillType.RPG:
                    success = ExecuteRPGSkill(skill, level, slot);
                    break;

                case SkillType.Special:
                    success = ExecuteSpecialSkill(skill, level, slot);
                    break;

                default:
                    LogError($"ExecuteSkill: 알 수 없는 스킬 타입 {skill.skillType}");
                    break;
            }
        }
        catch (System.Exception ex)
        {
            LogError($"ExecuteSkill: 스킬 실행 중 예외 발생 - {ex.Message}");
            success = false;
        }

        // 실행 결과 로깅
        if (success)
        {
            LogSkill($"ExecuteSkill 성공", skill, level, slot);
        }
        else
        {
            LogError($"ExecuteSkill: 스킬 실행 실패 - 스킬: {skill?.skillName ?? "Unknown"} (타입: {skill?.skillType}) [슬롯{slot}]");
        }
    }

    private bool ExecuteProjectileSkill(SkillData skill, int level, int slot)
    {
        try
        {
            // 목표 탐지
            var target = FindNearestEnemy();
            if (target == null)
            {
                // 적이 없는 것은 실패가 아닌 정상적인 상황, 쿨다운만 설정하고 성공으로 처리
                LogWarning($"ExecuteProjectileSkill: 적이 없어 발사 생략");
                float noTargetCooldown = GetSkillCooldown(skill, level);
                cooldownTimers[slot] = noTargetCooldown;
                return true; // 적이 없는 것은 실패가 아님
            }

            // 발사체 생성
            int projectileCount = GetProjectileCount(skill, level);
            LogSkill($"발사체 {projectileCount}개 발사 예정", skill, level, slot);

            int successCount = 0;
            for (int i = 0; i < projectileCount; i++)
            {
                if (SpawnProjectile(skill, level, target, i))
                {
                    successCount++;
                }
            }

            // 발사 결과 검증
            if (successCount == 0)
            {
                LogError($"ExecuteProjectileSkill: 모든 발사체 생성 실패");
                return false;
            }

            if (successCount < projectileCount)
            {
                LogWarning($"ExecuteProjectileSkill: 일부 발사체만 생성됨 ({successCount}/{projectileCount})");
            }

            // 쿨다운 설정
            float cooldown = GetSkillCooldown(skill, level);
            cooldownTimers[slot] = cooldown;
            LogSkill($"발사체 {successCount}개 발사 완료, 쿨다운 {cooldown:F1}초 설정", skill, level, slot);

            return true;
        }
        catch (System.Exception ex)
        {
            LogError($"ExecuteProjectileSkill: 예외 발생 - {ex.Message}");
            return false;
        }
    }

    private bool ExecuteGuardianSkill(SkillData skill, int level, int slot)
    {
        try
        {
            LogSkill("가디언 스킬 실행 시작", skill, level, slot);

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
                else
                {
                    LogError("ExecuteGuardianSkill: 스킬 오브젝트 프리팹이 null");
                    return false;
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
                    LogSkill("가디언 스킬 새로 활성화", skill, level, slot);
                }
                else
                {
                    // 이미 활성화된 상태면 레벨업 처리
                    guardianSkill.UpgradeGuardian();
                    LogSkill("가디언 스킬 레벨업", skill, level, slot);
                }

                // Guardian은 지속시간이 있으므로 쿨다운 설정
                float cooldown = GetSkillCooldown(skill, level);
                cooldownTimers[slot] = cooldown;
                return true;
            }
            else
            {
                LogError($"ExecuteGuardianSkill: 가디언 스킬 컴포넌트를 찾을 수 없음: {skill.skillName}");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            LogError($"ExecuteGuardianSkill: 예외 발생 - {ex.Message}");
            return false;
        }
    }

    private bool ExecuteDroneSkill(SkillData skill, int level, int slot)
    {
        try
        {
            LogSkill("드론 스킬 실행 시작", skill, level, slot);

            // 드론 스킬 프리팹에서 DroneSkill 컴포넌트 찾기
            DroneSkill droneSkill = null;

            if (droneSkillInstance == null)
            {
                if (skill.skillObjectPrefab != null)
                {
                    GameObject droneSkillObj = Instantiate(skill.skillObjectPrefab, transform);
                    droneSkill = droneSkillObj.GetComponent<DroneSkill>();

                    if (droneSkill != null)
                    {
                        // PlayerController에서 플레이어 Transform 찾기
                        var playerController = FindObjectOfType<PlayerController>();
                        Transform player = playerController != null ? playerController.transform : null;

                        if (player == null)
                        {
                            LogError("ExecuteDroneSkill: 플레이어 Transform을 찾을 수 없음");
                        }

                        droneSkill.SetPlayerTransform(player);
                        droneSkillInstance = droneSkill;
                        LogSkill("드론 스킬 인스턴스 새로 생성", skill, level, slot);
                    }
                    else
                    {
                        LogError("ExecuteDroneSkill: DroneSkill 컴포넌트를 프리팹에서 찾을 수 없음");
                        return false;
                    }
                }
                else
                {
                    LogError("ExecuteDroneSkill: 스킬 오브젝트 프리팹이 null");
                    return false;
                }
            }
            else
            {
                droneSkill = droneSkillInstance;
            }

            if (droneSkill != null)
            {
                // 먼저 SkillData 설정
                droneSkill.SetSkillData(skill);
                droneSkill.InitializeDroneSkill(level);
                LogSkill("드론 스킬 초기화 완료", skill, level, slot);

                // 쿨다운 설정 (드론 스킬은 자체 쿨다운을 관리하지만, 스킬 시스템에서도 관리)
                float cooldown = GetSkillCooldown(skill, level);
                cooldownTimers[slot] = cooldown;
                return true;
            }
            else
            {
                LogError("ExecuteDroneSkill: DroneSkill 인스턴스를 생성할 수 없음");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            LogError($"ExecuteDroneSkill: 예외 발생 - {ex.Message}");
            return false;
        }
    }

    private bool ExecuteLightningSkill(SkillData skill, int level, int slot)
    {
        try
        {
            LogSkill("번개 스킬 실행 시작", skill, level, slot);

            // 번개 스킬 오브젝트 생성
            GameObject lightningSkillObj;

            // 프리팹이 있으면 사용, 없으면 새로 생성
            if (skill.skillObjectPrefab != null)
            {
                lightningSkillObj = Instantiate(skill.skillObjectPrefab);
            }
            else
            {
                // 기본 번개 스킬 오브젝트 생성
                lightningSkillObj = new GameObject("LightningSkill_" + slot);
                lightningSkillObj.AddComponent<LightningSkill>();
            }

            // 부모 설정
            lightningSkillObj.transform.SetParent(transform);

            // LightningSkill 컴포넌트 설정
            LightningSkill lightningSkill = lightningSkillObj.GetComponent<LightningSkill>();
            if (lightningSkill != null)
            {
                // 먼저 SkillData 설정
                lightningSkill.SetSkillData(skill);
                lightningSkill.SetLevel(level);
                LogSkill("번개 스킬 오브젝트 생성 및 설정 완료", skill, level, slot);

                // 쿨다운 설정 (번개 스킬은 자체 쿨다운을 관리하지만, 스킬 시스템에서도 관리)
                float cooldown = GetSkillCooldown(skill, level);
                cooldownTimers[slot] = cooldown;
                return true;
            }
            else
            {
                LogError("ExecuteLightningSkill: LightningSkill 컴포넌트를 찾을 수 없음");
                Destroy(lightningSkillObj); // 실패한 오브젝트 정리
                return false;
            }
        }
        catch (System.Exception ex)
        {
            LogError($"ExecuteLightningSkill: 예외 발생 - {ex.Message}");
            return false;
        }
    }

    private bool ExecuteRPGSkill(SkillData skill, int level, int slot)
    {
        try
        {
            LogSkill("RPG 스킬 실행 시작", skill, level, slot);

            // RPG 스킬 프리팹에서 RPGSkill 컴포넌트 찾기
            RPGSkill rpgSkill = null;
            GameObject rpgSkillObj = GameObject.Find(skill.skillName);

            if (rpgSkillObj != null)
            {
                rpgSkill = rpgSkillObj.GetComponent<RPGSkill>();
            }
            else
            {
                // 새로운 RPG 스킬 오브젝트 생성
                if (skill.skillObjectPrefab != null)
                {
                    rpgSkillObj = Instantiate(skill.skillObjectPrefab, transform);
                    rpgSkillObj.name = skill.skillName;
                    rpgSkill = rpgSkillObj.GetComponent<RPGSkill>();
                }
                else
                {
                    LogError("ExecuteRPGSkill: 스킬 오브젝트 프리팹이 null");
                    return false;
                }
            }

            if (rpgSkill != null)
            {
                // 레벨 설정
                rpgSkill.SetLevel(level);

                // 스킬 데이터 설정
                rpgSkill.SetSkillData(skill);

                // 활성화
                if (!rpgSkill.IsActive)
                {
                    rpgSkill.SetActive(true);
                    LogSkill("RPG 스킬 활성화됨", skill, level, slot);
                }
                else
                {
                    LogSkill("RPG 스킬 이미 활성화됨", skill, level, slot);
                }

                // 쿨다운 설정
                float cooldown = GetSkillCooldown(skill, level);
                cooldownTimers[slot] = cooldown;
                return true;
            }
            else
            {
                LogError($"ExecuteRPGSkill: RPG 스킬 컴포넌트를 찾을 수 없음: {skill.skillName}");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            LogError($"ExecuteRPGSkill: 예외 발생 - {ex.Message}");
            return false;
        }
    }

    private bool ExecuteSpecialSkill(SkillData skill, int level, int slot)
    {
        try
        {
            LogSkill("특수 스킬 실행 시작", skill, level, slot);

            // 특수 스킬에 따른 개별 처리
            if (skill.skillObjectPrefab != null)
            {
                // MagneticDartSkill은 이미 존재하는 인스턴스가 있는지 확인
                var existingMagneticDart = FindObjectOfType<MagneticDartSkill>();
                if (existingMagneticDart != null)
                {
                    // 이미 활성화되어 있으면 레벨만 업데이트
                    existingMagneticDart.SetLevel(level);
                    LogSkill("MagneticDart 스킬 이미 활성화됨, 레벨 업데이트", skill, level, slot);
                }
                else
                {
                    // 새로 생성
                    GameObject specialObj = Instantiate(skill.skillObjectPrefab);
                    if (specialObj != null)
                    {
                        // 부모 설정
                        specialObj.transform.SetParent(transform);

                        // MagneticDartSkill 확인 및 활성화
                        var magneticDartSkill = specialObj.GetComponent<MagneticDartSkill>();
                        if (magneticDartSkill != null)
                        {
                            magneticDartSkill.Activate(level);
                            LogSkill("MagneticDart 스킬 활성화 완료", skill, level, slot);
                        }

                        LogSkill("특수 스킬 오브젝트 생성 완료", skill, level, slot);
                    }
                    else
                    {
                        LogError("ExecuteSpecialSkill: 특수 스킬 오브젝트 생성 실패");
                        return false;
                    }
                }

                // 쿨다운 설정 (지속형 스킬이므로 한 번만 실행)
                float cooldown = GetSkillCooldown(skill, level);
                cooldownTimers[slot] = cooldown;
                return true;
            }
            else
            {
                LogError("ExecuteSpecialSkill: 스킬 오브젝트 프리팹이 null");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            LogError($"ExecuteSpecialSkill: 예외 발생 - {ex.Message}");
            return false;
        }
    }

    private Transform FindNearestEnemy()
    {
        if (cachedEnemies == null || cachedEnemies.Length == 0)
        {
            // 디버그용: 적이 없는 상황 로깅 (너무 많이 출력되지 않도록 주기적으로만)
            if (Time.frameCount % 60 == 0) // 60프레임마다 한 번만 로그
            {
                LogWarning("FindNearestEnemy: 적 캐시가 비어있음");
            }
            return null;
        }

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
    private bool SpawnProjectile(SkillData skill, int level, Transform target, int index)
    {
        try
        {
            if (skill?.projectilePrefab == null)
            {
                LogError("SpawnProjectile: 스킬 또는 프리팹이 null");
                return false;
            }

            // 스탯 계산 (패시브 스킬 효과 포함)
            float damage = skill.damage * GetFinalDamageMultiplier(skill, level);
            float speed = skill.projectileSpeed * GetProjectileSpeedMultiplier(skill, level);
            float sizeMultiplier = GetProjectileSizeMultiplier(skill, level);

            // 발사체 생성
            if (skill.projectilePrefab.GetComponent<BoomerangProjectile>() != null)
            {
                return SpawnBoomerangProjectile(skill, damage, speed, sizeMultiplier);
            }
            else if (skill.projectilePrefab.GetComponent<MolotovProjectile>() != null)
            {
                return SpawnMolotovProjectile(skill, damage, level, index);
            }
            else if (skill.projectilePrefab.GetComponent<BrickProjectile>() != null)
            {
                return SpawnBrickProjectile(skill, damage, speed, index);
            }
            else if (skill.projectilePrefab.GetComponent<SoccerBallProjectile>() != null)
            {
                return SpawnSoccerBallProjectile(skill, damage, speed, index);
            }
            else
            {
                // 발사 방향 계산
                Vector2 direction = CalculateProjectileDirection(target, skill, level, index);
                return SpawnRegularProjectile(skill, damage, speed, direction, sizeMultiplier);
            }
        }
        catch (System.Exception ex)
        {
            LogError($"SpawnProjectile: 예외 발생 - {ex.Message}");
            return false;
        }
    }

    private bool SpawnRegularProjectile(SkillData skill, float damage, float speed, Vector2 direction, float sizeMultiplier)
    {
        try
        {
            var projectile = ObjectPoolManager.Instance.GetProjectile();
            if (projectile == null)
            {
                projectile = Instantiate(skill.projectilePrefab).GetComponent<Projectile>();
                if (projectile == null)
                {
                    LogError("SpawnRegularProjectile: Projectile 컴포넌트 생성 실패");
                    return false;
                }
            }
            else
            {
                projectile.gameObject.SetActive(true);
                projectile.gameObject.transform.position = transform.position;
            }

            // 크기 조절
            projectile.gameObject.transform.localScale = Vector3.one * sizeMultiplier;

            projectile.Init(damage, speed, direction);
            return true;
        }
        catch (System.Exception ex)
        {
            LogError($"SpawnRegularProjectile: 예외 발생 - {ex.Message}");
            return false;
        }
    }

    private bool SpawnBoomerangProjectile(SkillData skill, float damage, float speed, float sizeMultiplier)
    {
        try
        {
            // 부메랑도 오브젝트 풀에서 가져오기
            var boomerang = ObjectPoolManager.Instance.GetBoomerang();
            if (boomerang == null)
            {
                // 풀이 비어있으면 새로 생성
                boomerang = Instantiate(skill.projectilePrefab).GetComponent<BoomerangProjectile>();
                if (boomerang == null)
                {
                    LogError("SpawnBoomerangProjectile: BoomerangProjectile 컴포넌트 생성 실패");
                    return false;
                }
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
            return true;
        }
        catch (System.Exception ex)
        {
            LogError($"SpawnBoomerangProjectile: 예외 발생 - {ex.Message}");
            return false;
        }
    }

    private bool SpawnMolotovProjectile(SkillData skill, float damage, int level, int index)
    {
        try
        {
            // Molotov 오브젝트 풀에서 가져오기
            var molotov = ObjectPoolManager.Instance.GetMolotov();
            if (molotov == null)
            {
                // 풀이 비어있으면 새로 생성
                molotov = Instantiate(skill.projectilePrefab).GetComponent<MolotovProjectile>();
                if (molotov == null)
                {
                    LogError("SpawnMolotovProjectile: MolotovProjectile 컴포넌트 생성 실패");
                    return false;
                }
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
            return true;
        }
        catch (System.Exception ex)
        {
            LogError($"SpawnMolotovProjectile: 예외 발생 - {ex.Message}");
            return false;
        }
    }

    private bool SpawnBrickProjectile(SkillData skill, float damage, float speed, int index)
    {
        try
        {
            // Brick 오브젝트 풀에서 가져오기
            var brick = ObjectPoolManager.Instance.GetBrick();
            if (brick == null)
            {
                // 풀이 비어있으면 새로 생성
                brick = Instantiate(skill.projectilePrefab).GetComponent<BrickProjectile>();
                if (brick == null)
                {
                    LogError("SpawnBrickProjectile: BrickProjectile 컴포넌트 생성 실패");
                    return false;
                }
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
            return true;
        }
        catch (System.Exception ex)
        {
            LogError($"SpawnBrickProjectile: 예외 발생 - {ex.Message}");
            return false;
        }
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

    private bool SpawnSoccerBallProjectile(SkillData skill, float damage, float speed, int index)
    {
        try
        {
            // SoccerBall 오브젝트 풀에서 가져오기
            var soccerBall = ObjectPoolManager.Instance.GetSoccerBall();

            if (soccerBall == null)
            {
                // 풀이 비어있으면 새로 생성
                soccerBall = Instantiate(skill.projectilePrefab).GetComponent<SoccerBallProjectile>();
                if (soccerBall == null)
                {
                    LogError("SpawnSoccerBallProjectile: SoccerBallProjectile 컴포넌트 생성 실패");
                    return false;
                }
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
            return true;
        }
        catch (System.Exception ex)
        {
            LogError($"SpawnSoccerBallProjectile: 예외 발생 - {ex.Message}");
            return false;
        }
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
            return 3f;
        }

        float cooldown = skill.cooldown * GetFinalCooldownMultiplier(skill, level);

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

    #region Validation & Logging
    /// <summary>
    /// 스킬 슬롯 유효성 검사
    /// </summary>
    /// <param name="slot">검사할 슬롯</param>
    /// <param name="skill">출력할 스킬 데이터</param>
    /// <param name="level">출력할 스킬 레벨</param>
    /// <returns>유효한 슬롯이면 true</returns>
    private bool ValidateSkillSlot(int slot, out SkillData skill, out int level)
    {
        skill = null;
        level = 1;

        if (!IsValidSlot(slot))
        {
            LogError($"ValidateSkillSlot: 잘못된 슬롯 {slot}");
            return false;
        }

        skill = equippedSkills[slot];
        if (skill == null)
        {
            LogError($"ValidateSkillSlot: 슬롯 {slot}에 스킬이 없음");
            return false;
        }

        level = skillLevels[slot];
        if (level <= 0)
        {
            LogError($"ValidateSkillSlot: 슬롯 {slot}의 스킬 레벨이 0 이하 ({level})");
            level = 1;
        }

        return true;
    }

    /// <summary>
    /// 스킬 관련 로그 출력
    /// </summary>
    /// <param name="message">로그 메시지</param>
    /// <param name="skill">스킬 데이터</param>
    /// <param name="level">스킬 레벨</param>
    /// <param name="slot">슬롯 번호</param>
    private void LogSkill(string message, SkillData skill, int level, int slot = -1)
    {
        // 빈 메서드로 유지 (필요시 로그 활성화)
    }

    /// <summary>
    /// 에러 로그 출력
    /// </summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        Debug.LogError($"[SkillManager] {message}");
    }

    /// <summary>
    /// 경고 로그 출력
    /// </summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[SkillManager] {message}");
    }
    #endregion

    #region Utility
    private bool IsValidSlot(int slot) => slot >= 0 && slot < MAX_SKILLS;
    #endregion
    public int GetSkillLevel(SkillData data)
    {
        if (data == null) return 0;

        // 보유중인 스킬 검사
        for (int i = 0; i < equippedSkills.Length; i++)
        {
            // 데이터가 똑같은 게 있으면
            if (equippedSkills[i] == data)
            {
                return skillLevels[i]; // 그 스킬의 레벨을 반환
            }
        }
        return 0; // 없으면 0레벨
    }

    public void UnlockOrUpgradeSkill(SkillData skillToUp)
    {
        if (skillToUp == null) return;

        // 보유중인 스킬인지 확인 (있으면 레벨업)
        for (int i = 0; i < MAX_SKILLS; i++)
        {
            if (equippedSkills[i] == skillToUp)
            {
                int currentLevel = skillLevels[i];
                SetSkillLevel(i, currentLevel + 1); // 레벨업 후 종료
                return;
            }
        }

        // 없는 스킬이라면? 빈 슬롯 찾아서 새로 배우기
        for (int i = 0; i < MAX_SKILLS; i++)
        {
            if (equippedSkills[i] == null) // 빈 슬롯 찾기
            {
                EquipSkill(i, skillToUp, 1); // 1레벨로 장착후 종료
                return;
            }
        }
    }

    #region Passive Skill Integration
    /// <summary>
    /// 전체 데미지 배수 설정 (패시브 스킬용)
    /// </summary>
    /// <param name="multiplier">데미지 배수 (1.0 = 100%)</param>
    public void SetDamageMultiplier(float multiplier)
    {
        globalDamageMultiplier = Mathf.Max(0.1f, multiplier);
    }

    /// <summary>
    /// 전체 쿨다운 배수 설정 (패시브 스킬용)
    /// </summary>
    /// <param name="multiplier">쿨다운 배수 (1.0 = 100%)</param>
    public void SetCooldownMultiplier(float multiplier)
    {
        globalCooldownMultiplier = Mathf.Max(0.1f, multiplier);
    }

    /// <summary>
    /// 패시브 스킬이 적용된 실제 데미지 배수 반환
    /// </summary>
    /// <param name="skill">스킬 데이터</param>
    /// <param name="level">스킬 레벨</param>
    /// <returns>최종 데미지 배수</returns>
    public float GetFinalDamageMultiplier(SkillData skill, int level)
    {
        float baseMultiplier = GetDamageMultiplier(skill, level);
        return baseMultiplier * globalDamageMultiplier;
    }

    /// <summary>
    /// 패시브 스킬이 적용된 실제 쿨다운 배수 반환
    /// </summary>
    /// <param name="skill">스킬 데이터</param>
    /// <param name="level">스킬 레벨</param>
    /// <returns>최종 쿨다운 배수</returns>
    public float GetFinalCooldownMultiplier(SkillData skill, int level)
    {
        float baseMultiplier = GetCooldownMultiplier(skill, level);
        return baseMultiplier * globalCooldownMultiplier;
    }

    /// <summary>
    /// 현재 전체 데미지 배수 반환
    /// </summary>
    /// <returns>전체 데미지 배수</returns>
    public float GetGlobalDamageMultiplier()
    {
        return globalDamageMultiplier;
    }

    /// <summary>
    /// 현재 전체 쿨다운 배수 반환
    /// </summary>
    /// <returns>전체 쿨다운 배수</returns>
    public float GetGlobalCooldownMultiplier()
    {
        return globalCooldownMultiplier;
    }
    #endregion

    #region 패시브 스킬 테스트 기능
    /// <summary>
    /// 테스트용 패시브 스킬들을 자동으로 등록합니다
    /// </summary>
    [ContextMenu("테스트 패시브 스킬 등록")]
    public void RegisterTestPassiveSkills()
    {
        if (testPassiveSkills == null || testPassiveSkills.Length == 0)
        {
            return;
        }

        if (PassiveSkillManager.Instance == null)
        {
            Debug.LogError("[SkillManager] PassiveSkillManager.Instance를 찾을 수 없습니다.");
            return;
        }

        for (int i = 0; i < testPassiveSkills.Length; i++)
        {
            var passiveSkill = testPassiveSkills[i];
            if (passiveSkill != null)
            {
                // Inspector에서 설정한 레벨 사용 (없으면 최대 레벨로 설정)
                int targetLevel = GetTestPassiveSkillLevel(i);
                if (targetLevel <= 0) targetLevel = passiveSkill.maxLevel;
                if (targetLevel > passiveSkill.maxLevel) targetLevel = passiveSkill.maxLevel;

                // 목표 레벨까지 스킬 획득 반복
                for (int level = 1; level <= targetLevel; level++)
                {
                    PassiveSkillManager.Instance.TryAcquireSkill(passiveSkill.passiveType);
                }
            }
        }
    }

    /// <summary>
    /// 특정 테스트 패시브 스킬을 등록합니다
    /// </summary>
    /// <param name="index">testPassiveSkills 배열의 인덱스</param>
    [ContextMenu("테스트 패시브 스킬 첫 번째 항목 등록")]
    public void RegisterFirstTestPassiveSkill()
    {
        RegisterTestPassiveSkillByIndex(0);
    }

    /// <summary>
    /// 인덱스를 사용하여 특정 테스트 패시브 스킬을 등록합니다
    /// </summary>
    /// <param name="index">testPassiveSkills 배열의 인덱스</param>
    public void RegisterTestPassiveSkillByIndex(int index)
    {
        if (testPassiveSkills == null || index < 0 || index >= testPassiveSkills.Length)
        {
            Debug.LogError($"[SkillManager] 유효하지 않은 테스트 패시브 스킬 인덱스: {index}");
            return;
        }

        var passiveSkill = testPassiveSkills[index];
        if (passiveSkill == null)
        {
            Debug.LogError($"[SkillManager] 인덱스 {index}의 테스트 패시브 스킬이 null입니다.");
            return;
        }

        if (PassiveSkillManager.Instance == null)
        {
            Debug.LogError("[SkillManager] PassiveSkillManager.Instance를 찾을 수 없습니다.");
            return;
        }

        // Inspector에서 설정한 레벨까지 등록
        int targetLevel = GetTestPassiveSkillLevel(index);
        if (targetLevel <= 0) targetLevel = 1; // 최소 1레벨
        if (targetLevel > passiveSkill.maxLevel) targetLevel = passiveSkill.maxLevel;

        int currentLevel = PassiveSkillManager.Instance.GetSkillLevel(passiveSkill.passiveType);

        // 현재 레벨에서 목표 레벨까지 등록 반복
        for (int level = currentLevel + 1; level <= targetLevel; level++)
        {
            PassiveSkillManager.Instance.TryAcquireSkill(passiveSkill.passiveType);
        }
    }

    /// <summary>
    /// 테스트용 패시브 스킬 레벨을 가져옵니다
    /// </summary>
    /// <param name="index">testPassiveSkills 배열의 인덱스</param>
    /// <returns>설정된 레벨 (0이면 기본값 사용)</returns>
    private int GetTestPassiveSkillLevel(int index)
    {
        if (testPassiveSkillLevels == null || index < 0 || index >= testPassiveSkillLevels.Length)
            return 0;

        return testPassiveSkillLevels[index];
    }

    /// <summary>
    /// 모든 테스트 패시브 스킬을 제거합니다 (PassiveSkillManager 초기화)
    /// </summary>
    [ContextMenu("모든 테스트 패시브 스킬 제거")]
    public void ClearAllTestPassiveSkills()
    {
        if (PassiveSkillManager.Instance == null)
        {
            Debug.LogError("[SkillManager] PassiveSkillManager.Instance를 찾을 수 없습니다.");
            return;
        }

        PassiveSkillManager.Instance.ResetAllPassiveSkills();
    }

    /// <summary>
    /// 패시브 스킬 효과를 즉시 적용합니다
    /// </summary>
    [ContextMenu("패시브 스킬 효과 즉시 적용")]
    public void ApplyPassiveSkillEffects()
    {
        if (PassiveSkillManager.Instance == null)
        {
            Debug.LogError("[SkillManager] PassiveSkillManager.Instance를 찾을 수 없습니다.");
            return;
        }

        // PassiveSkillManager의 ApplyAllPassiveEffects 메서드를 호출하여 효과 적용
        var method = typeof(PassiveSkillManager).GetMethod("ApplyAllPassiveEffects",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method != null)
        {
            method.Invoke(PassiveSkillManager.Instance, null);
        }
    }
    #endregion
}