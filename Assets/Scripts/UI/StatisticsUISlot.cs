using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatisticsUISlot : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Image iconImage; // 스킬 아이콘
    [SerializeField] private TextMeshProUGUI nameText; // 스킬 이름
    [SerializeField] private TextMeshProUGUI damageText; // 데미지 숫자
    [SerializeField] private TextMeshProUGUI dpsText; // DPS 숫자
    [SerializeField] private Slider damageGauge; // 딜량 그래프 (Slider)

    // 데이터 세팅 함수
    public void Init(SkillSessionData data, float maxDamageInSession, Sprite icon, string nameOverride)
    {
        if (string.IsNullOrEmpty(nameOverride) == false)
        {
            nameText.text = nameOverride; // 한글이름
        }
        else
        {
            nameText.text = data.skillName; // 영어이름
        }

        damageText.text = $"{data.totalDamage:N0}"; // 데미지
        dpsText.text = $"{data.dps:F1}"; // DPS

        // 그래프 게이지
        if (maxDamageInSession > 0)
            damageGauge.value = data.totalDamage / maxDamageInSession;
        else
            damageGauge.value = 0;

        if (icon != null)
        {
            iconImage.sprite = icon;
            iconImage.color = Color.white;
        }
    }
}