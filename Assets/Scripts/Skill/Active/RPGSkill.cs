using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RPGSkill : MonoBehaviour
{
    #region Serialized Fields
    [Header("스킬 설정")]
    [SerializeField] private SkillData skillData;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private bool isActive = true;

    [Header("밸런스 수치")]
    [SerializeField] private float baseCooldown = 4f;
    [SerializeField] private float baseDamage = 20f;
    [SerializeField] private int baseProjectileCount = 1;
    [SerializeField] private float cooldownReductionPerLevel = 0.2f;

    [Header("발사 설정")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float spreadAngle = 15f; // 다중 발사체 분산 각도
    [SerializeField] private float attackRange = 15f; // 사정거리
    [SerializeField] private float targetUpdateInterval = 0.5f;
    #endregion

    #region Private Fields
    private float currentCooldown;
    private int projectileCount;
    private float damage;
    private float cooldownTime;
    private Enemy currentTarget;
    private float targetUpdateTimer;
    private Transform playerTransform;
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
            Debug.LogWarning("RPGSkill: SkillData가 설정되지 않았습니다!");
            isActive = false;
            return;
        }

        // 플레이어 Transform 찾기
        playerTransform = transform;
    }

    private void Start()
    {
        // 스킬 데이터에서 값 가져오기
        LoadSkillData();
    }

    private void Update()
    {
        if (!isActive) return;

        // 쿨타임 업데이트
        if (currentCooldown > 0f)
        {
            currentCooldown -= Time.deltaTime;
        }

        // 타겟 업데이트
        UpdateTarget();

        // 자동 발동 체크
        if (currentCooldown <= 0f && currentTarget != null)
        {
            FireRPG();
        }
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
            currentTarget = null;
        }
    }

    /// <summary>
    /// 수동 발동
    /// </summary>
    public void ManualFire()
    {
        if (isActive && currentCooldown <= 0f && currentTarget != null)
        {
            FireRPG();
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
            Debug.LogWarning("RPGSkill: SkillData가 null입니다!");
            return;
        }

        // 레벨별 데이터 가져오기
        var levelData = GetLevelData(currentLevel);
        if (levelData != null)
        {
            damage = baseDamage * levelData.damageMultiplier;
            cooldownTime = baseCooldown * levelData.cooldownMultiplier;
            projectileCount = baseProjectileCount + levelData.additionalProjectiles;
        }
        else
        {
            // 기본값으로 설정 (Survivor.io 스펙 기반)
            damage = baseDamage * GetLevelDamageMultiplier(currentLevel);
            cooldownTime = baseCooldown - (cooldownReductionPerLevel * (currentLevel - 1));
            projectileCount = GetLevelProjectileCount(currentLevel);
        }

        // 최소/최대값 제한
        projectileCount = Mathf.Clamp(projectileCount, 1, 5);
        cooldownTime = Mathf.Max(cooldownTime, 1f);
        damage = Mathf.Max(damage, 1f);
    }

    /// <summary>
    /// 레벨별 데미지 배수 (Survivor.io 스펙 기반)
    /// </summary>
    private float GetLevelDamageMultiplier(int level)
    {
        switch (level)
        {
            case 1: return 2f;   // ★
            case 2: return 4f;   // ★★
            case 3: return 4f;   // ★★★
            case 4: return 6f;   // ★★★★
            case 5: return 6f;   // ★★★★★
            default: return 6f + (level - 5) * 2f;
        }
    }

    /// <summary>
    /// 레벨별 발사체 수 (Survivor.io 스펙 기반)
    /// </summary>
    private int GetLevelProjectileCount(int level)
    {
        switch (level)
        {
            case 1: return 1;  // ★
            case 2: return 1;  // ★★
            case 3: return 2;  // ★★★ (+1)
            case 4: return 2;  // ★★★★
            case 5: return 3;  // ★★★★★ (+1)
            default: return 3;
        }
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
    /// 타겟 업데이트
    /// </summary>
    private void UpdateTarget()
    {
        targetUpdateTimer += Time.deltaTime;
        if (targetUpdateTimer >= targetUpdateInterval)
        {
            targetUpdateTimer = 0f;

            // 가장 가까운 적 찾기
            currentTarget = FindClosestEnemy();
        }
    }

    /// <summary>
    /// 가장 가까운 적 찾기
    /// </summary>
    private Enemy FindClosestEnemy()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        Enemy closestEnemy = null;
        float closestDistance = attackRange;

        foreach (Enemy enemy in enemies)
        {
            if (!enemy.gameObject.activeInHierarchy) continue;

            float distance = Vector3.Distance(playerTransform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    /// <summary>
    /// RPG 발사
    /// </summary>
    private void FireRPG()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy) return;

        // 발사체 발사
        StartCoroutine(FireProjectilesSequence());

        // 쿨타임 설정
        currentCooldown = cooldownTime;
    }

    /// <summary>
    /// 다중 발사체 시퀀스
    /// </summary>
    private IEnumerator FireProjectilesSequence()
    {
        int strikesToSpawn = Mathf.Min(projectileCount, 1);

        for (int i = 0; i < strikesToSpawn; i++)
        {
            FireSingleProjectile(i);

            // 발사 간 딜레이
            if (i < strikesToSpawn - 1)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    /// <summary>
    /// 단일 발사체 발사
    /// </summary>
    private void FireSingleProjectile(int index)
    {
        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogError("RPGSkill: ObjectPoolManager를 찾을 수 없습니다!");
            return;
        }

        RPGProjectile projectile = ObjectPoolManager.Instance.GetRPG();
        if (projectile != null)
        {
            Vector3 targetPosition = currentTarget.transform.position;

            // 다중 발사체 각도 계산
            Vector3 direction = (targetPosition - transform.position).normalized;

            if (projectileCount > 1)
            {
                // 중간 발사체는 정면, 양옆은 분산
                float angleOffset = 0f;
                if (projectileCount == 2)
                {
                    angleOffset = index == 0 ? -spreadAngle : spreadAngle;
                }
                else if (projectileCount == 3)
                {
                    angleOffset = index == 0 ? -spreadAngle : (index == 1 ? 0 : spreadAngle);
                }
                else if (projectileCount >= 4)
                {
                    angleOffset = (index - (projectileCount - 1) * 0.5f) * (spreadAngle * 2f / (projectileCount - 1));
                }

                direction = Quaternion.Euler(0, 0, angleOffset) * direction;
            }

            // 발사체 초기화
            projectile.SetParameters(transform.position, direction, damage, projectileSpeed);
        }
        else
        {
            Debug.LogError("RPGSkill: RPGProjectile를 풀에서 가져올 수 없습니다!");
        }
    }
    #endregion

    #region Debug
    // private void OnGUI()
    // {
    //     if (!isActive) return;

    //     // 디버그 정보 표시
    //     GUILayout.BeginArea(new Rect(10, 250, 300, 200));
    //     GUILayout.Label($"RPG Skill Lv.{currentLevel}");
    //     GUILayout.Label($"Damage: {damage:F1}");
    //     GUILayout.Label($"Projectile Count: {projectileCount}");
    //     GUILayout.Label($"Projectile Speed: {projectileSpeed}m/s");
    //     GUILayout.Label($"Attack Range: {attackRange}m");
    //     GUILayout.Label($"Cooldown: {cooldownTime:F1}s");
    //     GUILayout.Label($"Next Fire: {currentCooldown:F1}s");
    //     GUILayout.Label($"Target: {(currentTarget != null ? currentTarget.name : "None")}");
    //     GUILayout.EndArea();
    // }

    private void OnDrawGizmosSelected()
    {
        // 사정거리 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 타겟까지 선 표시
        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
    }
    #endregion
}