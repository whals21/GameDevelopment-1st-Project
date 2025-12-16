using UnityEngine;

// 스킬 타입 정의
public enum SkillType
{
    Projectile,  // 일반 투사체 스킬 (축구공, 부메랑 등)
    Guardian,    // 가디언 스킬
    Forcefield,  // 포스필드 스킬
    Special      // 기타 특수 스킬
}

[CreateAssetMenu(menuName = "Skill/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    public string skillName;
    public string description;
    public Sprite icon;

    [Header("스킬 타입")]
    public SkillType skillType = SkillType.Projectile;

    [Header("프리팹 설정")]
    public GameObject projectilePrefab;  // 발사체 프리팹 (Projectile 타입)
    public GameObject skillObjectPrefab; // 스킬 오브젝트 프리팹 (Guardian/Forcefield/Special 타입)

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
    public float projectileSizeMultiplier = 1f;  // 투사체 크기 배수
    public float projectileScaleIncrease = 0f;    // 레벨업 시 크기 증가량 (부메랑 4,5레벨용)
    public string upgradeDescription;
}