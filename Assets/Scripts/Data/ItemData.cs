using UnityEngine;

// 아이템이 무기인지 패시브인지 구분
public enum ItemType
{
    Active,
    Passive
}

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Object/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("아이템 종류")]
    public ItemType itemType;

    [Header("공통 정보")]
    public string itemName;
    public Sprite itemIcon;
    [TextArea] public string itemDesc;

    [Header("액티브 스킬 설정 (Active일 때만)")]
    public SkillData skillData;

    [Header("패시브 스킬 설정 (Passive일 때만)")]
    public PassiveSkillType passiveType;
    public float[] passiveAmounts;

    // 수치 가져오는 함수
    public float GetPassiveValue(int level)
    {
        if (passiveAmounts == null || passiveAmounts.Length == 0) return 0f;
        int index = Mathf.Clamp(level - 1, 0, passiveAmounts.Length - 1);
        return passiveAmounts[index];
    }
}