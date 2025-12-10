using UnityEngine;

[CreateAssetMenu(menuName = "Skill/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    public string skillName;
    public string description;
    public Sprite icon;
    public GameObject projectilePrefab;  // 발사체 프리팹

    [Header("스탯")]
    public float damage = 10f;
    public float cooldown = 1f;
    public float projectileSpeed = 15f;
    public int projectileCount = 1;
    public float attackRange = 10f;

    [Header("레벨별 스탯")]
    public SkillLevel[] levels;  // 5개 레벨 데이터

    [Header("EVO 설정")]
    public SkillData evoSkill;  // 진화 시 획득할 스킬
    public int[] evoRequirements;  // 진화에 필요한 다른 스킬 ID
}

[System.Serializable]
public class SkillLevel
{
    public int level;
    public float damageMultiplier = 1f;
    public float cooldownMultiplier = 1f;
    public int additionalProjectiles = 0;
    public float projectileSpeedMultiplier = 1f;
    public string upgradeDescription;
}