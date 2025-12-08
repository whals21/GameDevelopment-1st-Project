using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("�÷��̾� �ɷ�ġ ����")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float currentHp;
    [SerializeField] private float magnetRange = 3f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float cooldown = 1f;

    [Header("���� �ý���")]
    [SerializeField] private int level = 1;
    [SerializeField] private float currentExp = 0;
    [SerializeField] private AnimationCurve expCurve;


    void Start()
    {
        currentHp = maxHp;
        //���۽� UI ����
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

    public void AddExp(float amount)
    {
        currentExp += amount;

        // ����ġ�� UI ���� ������ �ִٸ� ���⼭ ȣ��
        if (currentExp >= MaxExp)
        {
            LevelUp();
        }
    }

    void LevelUp()
    {
        currentExp -= MaxExp; // ���� ����ġ �̿�
        level++;
        // ��ų ����â ��￹��
    }

    public float MaxExp
    {
        get
        {
            if (expCurve == null || expCurve.length == 0) return 100 * level; // ������ġ
            return expCurve.Evaluate(level);
        }
    }

    public void AddSpeed(float amount) { speed += amount; }
    public void AddMaxHp(float amount) { maxHp += amount; currentHp += amount; }
    public void AddMagnetRange(float amount) { magnetRange += amount; }
    public void AddDamage(float amount) { damage += amount; }
    public void AddCooldown(float amount) { cooldown += amount; }

    // [�ܺ� ���ٿ�]
    public float Speed
    {
        get { return speed; }
    }

    public float MaxHp
    {
        get { return maxHp; }
    }

    public float CurrentHp
    {
        get { return currentHp; }
    }

    public float MagnetRange
    {
        get { return magnetRange; }
    }

    public float Damage
    {
        get { return damage; }
    }

    public float Cooldown
    {
        get { return cooldown; }
    }

    public int Level
    {
        get { return level; }
    }
}