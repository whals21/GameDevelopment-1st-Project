using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Object/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("스킬 데이터 연결")]
    public SkillData skillData;

    [Header("UI 표시 정보")]
    public string itemName;
    [TextArea]
    public string itemDesc;
    public Sprite itemIcon;
}