#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 패시브 스킬 데이터 자동 생성 에디터 스크립트
/// Unity 메뉴에서 기본 패시브 스킬 데이터들을 생성
/// </summary>
public class PassiveSkillDataCreator
{
    private const string FOLDER_PATH = "Assets/Scripts/Skill/Passive";

    [MenuItem("Tools/Create Passive Skill Data")]
    public static void CreateAllPassiveSkillData()
    {
        // 폴더 생성
        if (!Directory.Exists(FOLDER_PATH))
        {
            Directory.CreateDirectory(FOLDER_PATH);
        }

        // 모든 패시브 스킬 타입에 대한 데이터 생성
        CreateExperienceBonusSkill();
        CreateMagnetRangeSkill();
        CreateDamageBoostSkill();
        CreateMaxHealthSkill();
        CreateHealthRegenSkill();
        CreateMovementSpeedSkill();
        CreateCooldownReductionSkill();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PassiveSkillDataCreator] 모든 패시브 스킬 데이터 생성 완료!");
    }

    private static void CreateExperienceBonusSkill()
    {
        var skillData = ScriptableObject.CreateInstance<PassiveSkillData>();
        skillData.passiveType = PassiveSkillType.ExperienceBonus;
        skillData.skillName = "경험치 증가";
        skillData.description = "획득하는 경험치가 증가합니다.";
        skillData.effectLevels = new float[] { 0.1f, 0.15f, 0.20f, 0.25f, 0.30f }; // 10%, 15%, 20%, 25%, 30%
        skillData.priority = 5;

        SaveAsset(skillData, "ExperienceBonusSkill");
    }

    private static void CreateMagnetRangeSkill()
    {
        var skillData = ScriptableObject.CreateInstance<PassiveSkillData>();
        skillData.passiveType = PassiveSkillType.MagnetRange;
        skillData.skillName = "자석 범위 증가";
        skillData.description = "경험치 볼을 끌어당기는 범위가 증가합니다.";
        skillData.effectLevels = new float[] { 1.0f, 1.5f, 2.0f, 2.5f, 3.0f }; // 1.0m, 1.5m, 2.0m, 2.5m, 3.0m
        skillData.priority = 4;

        SaveAsset(skillData, "MagnetRangeSkill");
    }

    private static void CreateDamageBoostSkill()
    {
        var skillData = ScriptableObject.CreateInstance<PassiveSkillData>();
        skillData.passiveType = PassiveSkillType.DamageBoost;
        skillData.skillName = "공격력 증가";
        skillData.description = "모든 스킬의 데미지가 증가합니다.";
        skillData.effectLevels = new float[] { 0.10f, 0.15f, 0.20f, 0.25f, 0.30f }; // 10%, 15%, 20%, 25%, 30%
        skillData.priority = 8;

        SaveAsset(skillData, "DamageBoostSkill");
    }

    private static void CreateMaxHealthSkill()
    {
        var skillData = ScriptableObject.CreateInstance<PassiveSkillData>();
        skillData.passiveType = PassiveSkillType.MaxHealth;
        skillData.skillName = "최대 체력 증가";
        skillData.description = "플레이어의 최대 체력이 증가합니다.";
        skillData.effectLevels = new float[] { 20f, 35f, 50f, 70f, 100f }; // 20, 35, 50, 70, 100 HP
        skillData.priority = 7;

        SaveAsset(skillData, "MaxHealthSkill");
    }

    private static void CreateHealthRegenSkill()
    {
        var skillData = ScriptableObject.CreateInstance<PassiveSkillData>();
        skillData.passiveType = PassiveSkillType.HealthRegen;
        skillData.skillName = "체력 재생";
        skillData.description = "시간이 지남에 따라 체력이 자동으로 회복됩니다.";
        skillData.effectLevels = new float[] { 1.0f, 1.5f, 2.0f, 2.5f, 3.0f }; // 1.0, 1.5, 2.0, 2.5, 3.0 HP/초
        skillData.priority = 3;

        SaveAsset(skillData, "HealthRegenSkill");
    }

    private static void CreateMovementSpeedSkill()
    {
        var skillData = ScriptableObject.CreateInstance<PassiveSkillData>();
        skillData.passiveType = PassiveSkillType.MovementSpeed;
        skillData.skillName = "이동속도 증가";
        skillData.description = "플레이어의 이동속도가 증가합니다.";
        skillData.effectLevels = new float[] { 0.15f, 0.20f, 0.25f, 0.30f, 0.35f }; // 15%, 20%, 25%, 30%, 35%
        skillData.priority = 6;

        SaveAsset(skillData, "MovementSpeedSkill");
    }

    private static void CreateCooldownReductionSkill()
    {
        var skillData = ScriptableObject.CreateInstance<PassiveSkillData>();
        skillData.passiveType = PassiveSkillType.CooldownReduction;
        skillData.skillName = "쿨다운 감소";
        skillData.description = "모든 스킬의 쿨다운 시간이 감소합니다.";
        skillData.effectLevels = new float[] { 0.08f, 0.12f, 0.16f, 0.20f, 0.25f }; // 8%, 12%, 16%, 20%, 25%
        skillData.priority = 9;

        SaveAsset(skillData, "CooldownReductionSkill");
    }

    private static void SaveAsset(PassiveSkillData skillData, string fileName)
    {
        string assetPath = Path.Combine(FOLDER_PATH, $"{fileName}.asset");
        AssetDatabase.CreateAsset(skillData, assetPath);
        Debug.Log($"[PassiveSkillDataCreator] 생성됨: {assetPath}");
    }
}
#endif