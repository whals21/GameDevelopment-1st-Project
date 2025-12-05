using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    // 어디서든 PlayerHUD.Instance로 부를 수 있게 만드는 '싱글톤'
    public static PlayerHUD Instance;

    [Header("UI 컴포넌트 연결")]
    public Slider hpSlider;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateHp(float currentHp, float maxHp)
    {
        // 슬라이더는 0 ~ 1 사이의 값(비율)으로 움직임
        hpSlider.value = currentHp / maxHp;
    }
}