using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("플레이어 능력치 설정")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float currentHp;
    [SerializeField] private float magnetRange = 3f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 1f;

    [Header("성장 시스템")]
    [SerializeField] private int level = 1;
    [SerializeField] private float currentExp = 0;
    [SerializeField] private AnimationCurve expCurve;

    void Start()
    {
        currentHp = maxHp;
        // 시작 시 UI 갱신
        if (PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.UpdateHp(currentHp, maxHp);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHp -= damage;

        if (currentHp < 0) currentHp = 0;

        if (PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.UpdateHp(currentHp, maxHp);
        }

        if (currentHp <= 0)
        {
            // Die(); 
        }
    }

    public void GainExp(int amount)
    {
        currentExp += amount;

        // 경험치바 UI 갱신
        if (PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.UpdateExp(currentExp, MaxExp);
        }

        if (currentExp >= MaxExp)
        {
            LevelUp();
        }
    }
    void LevelUp()
    {
        currentExp -= MaxExp;
        level++;

        Debug.Log($"레벨 업 현재 레벨: {level}");

        // 경험치바 갱신
        if (PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.UpdateExp(currentExp, MaxExp);

            // 시퀀스 시작
            PlayerHUD.Instance.StartLevelUpSequence();
        }
    }
    public float MaxExp
    {
        get
        {
            if (expCurve == null || expCurve.length == 0) return 100 * level; // 안전장치
            return expCurve.Evaluate(level);
        }
    }

    // 스탯 증가 메서드
    public void AddSpeed(float amount) { speed += amount; }
    public void AddMaxHp(float amount) { maxHp += amount; currentHp += amount; }
    public void AddMagnetRange(float amount) { magnetRange += amount; }
    public void AddDamage(float amount) { damage += amount; }
    public void AddCooldown(float amount) { cooldown += amount; }

    // [외부 접근용]
    public float Speed { get { return speed; } }
    public float MaxHp { get { return maxHp; } }
    public float CurrentHp { get { return currentHp; } }
    public float MagnetRange { get { return magnetRange; } }
    public float Damage { get { return damage; } }
    public float Cooldown { get { return cooldown; } }
    public int Level { get { return level; } }
}