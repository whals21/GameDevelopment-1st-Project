using UnityEngine;
using System.Collections;

public class BossDie : MonoBehaviour
{
    public BossController bossController;
    private Animator animator;

    private float currentHp;
    private bool isDead = false;

    [SerializeField] private float dieAnimationSpeed = 1f;
    [SerializeField] private float fadeOutDelay = 0.3f;

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
        if (animator != null) animator.speed = 1f;
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

        // 모든 기능 멈춤
        bossController?.SetState(null);
        bossController?.ReturnAllWarningPads();
        if (bossController?.attackComp != null)
            bossController.attackComp.enabled = false;

        // Die 애니메이션 재생
        if (animator != null)
        {
            animator.speed = dieAnimationSpeed;
            animator.SetTrigger("isDie");
        }
    }

    // Animation Event에서 호출
    public void OnDeathAnimationFinished()
    {
        DropItem();

        // 약간 지연 후 보스 비활성화
        gameObject.SetActive(false);
    }



    public void DropItem()
    {
        Debug.Log("보스 아이템 드랍");
    }
}
