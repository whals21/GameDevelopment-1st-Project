using UnityEngine;

public class BossDie : MonoBehaviour, IDamageable
{
    public Transform Transform => transform;
    private BossController bossController;
    private Animator animator;

    private float currentHp;
    public float CurrentHp => currentHp;
    public float MaxHp => bossController.Data.hp;

    private bool isDead = false;

    private void Awake()
    {
        bossController = GetComponent<BossController>();
        animator = GetComponentInChildren<Animator>();

        if (bossController == null)
            Debug.LogError("BossDie: BossController 없음");
        if (animator == null)
            Debug.LogError("BossDie: Animator 없음");
    }

    /// <summary>
    /// BossController에서 Data 세팅 후 반드시 호출
    /// </summary>
    public void Initialize()
    {
        currentHp = MaxHp;
        isDead = false;
    }

    // IDamageable 구현
    public void TakeDamage(float damage, bool isCritical = false)
    {
        if (isDead) return;

        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, MaxHp);

        // 데미지 텍스트 (Enemy와 동일)
        if (ObjectPoolManager.Instance != null)
        {
            DamageText text = ObjectPoolManager.Instance.GetDamageText();
            if (text != null)
                text.Init(damage, isCritical, transform.position);
        }

        if (currentHp <= 0)
            Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        bossController.SetState(null);
        bossController.ReturnAllWarningPads();

        animator.SetTrigger("isDie");
    }

    // Animation Event
    public void OnDeathAnimationFinished()
    {
        DropItem();
        gameObject.SetActive(false);
    }

    private void DropItem()
    {
        Debug.Log("보스 아이템 드랍");
    }
}
