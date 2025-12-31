using UnityEngine;

public class BossDie : MonoBehaviour
{
    private BossController bossController;
    private Animator animator;

    private float currentHp;
    private bool isDead = false;

    private void Awake()
    {
        bossController = GetComponent<BossController>();
        animator = GetComponentInChildren<Animator>();
    }

    public void Initialize()
    {
        currentHp = MaxHp;
        isDead = false;
        gameObject.SetActive(true);
    }

    public float MaxHp => bossController != null ? bossController.Data.hp : 100;

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, MaxHp);

        if (currentHp <= 0f)
            Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        bossController?.SetState(null);
        bossController?.ReturnAllWarningPads();

        animator?.SetTrigger("isDie");
    }

    // Animation Event에서 호출
    public void OnDeathAnimationFinished()
    {
        DropItem();
        bossController?.SetState(null);
        bossController?.ReturnAllWarningPads();

        // **여기서 보스 완전히 비활성화**
        gameObject.SetActive(false);
    }

    private void DropItem()
    {
        Debug.Log("보스 아이템 드랍");
    }
}
