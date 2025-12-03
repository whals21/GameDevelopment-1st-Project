using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("플레이어 능력치 설정")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float currentHp;
    [SerializeField] private float magnetRange = 3f;
    

    void Start()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(float damage)
    {
        currentHp -= damage;
    }

    // [외부 접근용]
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
}