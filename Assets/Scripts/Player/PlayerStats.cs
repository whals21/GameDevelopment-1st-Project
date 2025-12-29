using UnityEngine;
using System.Collections;

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

    private bool isInvincible = false;

    [Header("체력 재생 시스템")]
    [SerializeField] private float healthRegenRate = 0f;  // 초당 체력 재생량
    private Coroutine healthRegenCoroutine;  // 체력 재생 코루틴

    [Header("경험치 보너스 시스템")]
    [SerializeField] private float expBonusMultiplier = 1.0f;  // 경험치 보너스 배수 (1.0 = 100%)

    public static PlayerStats Instance { get; private set; } // 12/29추가

    private void Awake()    //12/29추가
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 중복 제거
            return;
        }
        Instance = this;
    }

    void Start()
    {
        currentHp = maxHp;
        // 시작 시 UI 갱신
        if (PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.UpdateHp(currentHp, maxHp);
            PlayerHUD.Instance.UpdateLevel(level);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isInvincible) return;

        currentHp -= damage;

        if (currentHp < 0) currentHp = 0;

        if (PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.UpdateHp(currentHp, maxHp);
        }

        if (ObjectPoolManager.Instance != null)
        {
            DamageText text = ObjectPoolManager.Instance.GetDamageText();
            if (text != null)
            {
                text.Init(damage, false, transform.position, true);
            }
        }

        if (currentHp > 0) // 아직 살아있을 때만 무적 실행
        {
            StartCoroutine(InvincibleRoutine());
        }

        if (currentHp <= 0)
        {
            currentHp = 0;

            // 게임오버
            if (PlayerHUD.Instance != null)
            {
                PlayerHUD.Instance.ShowGameOverUI();
            }

            // gameObject.SetActive(false); // 플레이어가 아예 사라짐
        }
    }
    public void Heal(float amount)
    {
        currentHp += amount;

        // 최대 체력을 넘지 않도록
        if (currentHp > maxHp)
        {
            currentHp = maxHp;
        }

        // UI 갱신
        if (PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.UpdateHp(currentHp, maxHp);
        }
    }

    IEnumerator InvincibleRoutine()
    {
        isInvincible = true; // 무적 켜기

        yield return new WaitForSeconds(0.5f); // 2. 0.5초 기다리기

        isInvincible = false;
    }

    public void GainExp(int amount)
    {
        // 경험치 보너스 적용
        float finalExp = amount * expBonusMultiplier;
        currentExp += finalExp;

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

        if (PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.UpdateExp(currentExp, MaxExp);
            PlayerHUD.Instance.UpdateLevel(level);

            // PlayerHUD.Instance.StartLevelUpSequence(); 
        }

        // 레벨업 매니저 호출
        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.ShowLevelUp();
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

    // 체력 재생 관련 메서드
    public void SetHealthRegenRate(float rate)
    {
        healthRegenRate = rate;

        // 코루틴 관리
        if (healthRegenRate > 0f)
        {
            if (healthRegenCoroutine == null)
            {
                healthRegenCoroutine = StartCoroutine(HealthRegenRoutine());
            }
        }
        else
        {
            if (healthRegenCoroutine != null)
            {
                StopCoroutine(healthRegenCoroutine);
                healthRegenCoroutine = null;
            }
        }
    }

    public float GetHealthRegenRate() { return healthRegenRate; }

    public void AddHealthRegenRate(float amount)
    {
        SetHealthRegenRate(healthRegenRate + amount);
    }

    /// <summary>
    /// 체력 재생 코루틴 - 1초마다 체력 회복
    /// </summary>
    private IEnumerator HealthRegenRoutine()
    {
        while (healthRegenRate > 0f && currentHp < maxHp)
        {
            yield return new WaitForSeconds(1f);

            if (currentHp < maxHp && healthRegenRate > 0f)
            {
                Heal(healthRegenRate);
            }
        }
        healthRegenCoroutine = null;
    }

    // 경험치 보너스 관련 메서드
    public void SetExpBonusMultiplier(float multiplier) { expBonusMultiplier = multiplier; }
    public float GetExpBonusMultiplier() { return expBonusMultiplier; }
    public void AddExpBonusMultiplier(float amount) { expBonusMultiplier += amount; }

    // [외부 접근용]
    public float Speed { get { return speed; } }
    public float MaxHp { get { return maxHp; } }
    public float CurrentHp { get { return currentHp; } }
    public float MagnetRange { get { return magnetRange; } }
    public float Damage { get { return damage; } }
    public float Cooldown { get { return cooldown; } }
    public int Level { get { return level; } }
}