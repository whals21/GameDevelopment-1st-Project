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

    // public void TakeDamage(float damage)
    // {
    //     if (isDead) return;

    //     currentHp -= damage;
    //     currentHp = Mathf.Clamp(currentHp, 0, MaxHp);

    //     if (currentHp <= 0f)
    //         Die();
    // }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // ��� ��� ����
        bossController?.SetState(null);
        bossController?.ReturnAllWarningPads();
        if (bossController?.attackComp != null)
            bossController.attackComp.enabled = false;

        // Die �ִϸ��̼� ���
        if (animator != null)
        {
            animator.speed = dieAnimationSpeed;
            animator.SetTrigger("isDie");
        }
    }

    // Animation Event���� ȣ��
    public void OnDeathAnimationFinished()
    {
        DropItem();

        // �ణ ���� �� ���� ��Ȱ��ȭ
        gameObject.SetActive(false);
    }

   

    public void DropItem()
    {
        Debug.Log("���� ������ ���");
    }
}
