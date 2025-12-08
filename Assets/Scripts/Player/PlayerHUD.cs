using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance;

    [Header("UI ������Ʈ ����")]
    public Slider hpSlider;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateHp(float currentHp, float maxHp)
    {
        hpSlider.value = currentHp / maxHp;
    }
}