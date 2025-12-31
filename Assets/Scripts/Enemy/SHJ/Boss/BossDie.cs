using UnityEngine;

// ==========================
// BossDie
// - 보스 체력 관리
// - 데미지 처리
// - 사망 애니메이션 트리거
// - Animation Event로 사망 후 처리
// ==========================
public class BossDie : MonoBehaviour
{
    private BossController bossController;
    private Animator animator;

    private float currentHp;

    public float CurrentHp => currentHp;
    public float MaxHp => bossController.Data.hp;

    private bool isDead = false;

    private void Awake()
    {
        bossController = GetComponent<BossController>();
        animator = GetComponent<Animator>();

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
        currentHp = bossController.Data.hp;
        isDead = false;
    }

    /// <summary>
    /// 보스가 데미지를 받을 때 호출
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, MaxHp);

        Debug.Log($"Boss HP: {currentHp} / {MaxHp}");

        if (currentHp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 사망 처리 (애니메이션 시작까지만 담당)
    /// </summary>
    private void Die()
    {
        isDead = true;

        // 보스 행동 정지
        bossController.SetState(null);

        // 경고 발판 회수
        bossController.ReturnAllWarningPads();

        // 사망 애니메이션 트리거
        animator.SetTrigger("isDie");
    }

    /// <summary>
    /// Animation Event
    /// Die 애니메이션 마지막 프레임에서 호출됨
    /// </summary>
    public void OnDeathAnimationEnd()
    {
        // 아이템 드랍
        DropItem();

        // 보스 비활성화
        gameObject.SetActive(false);
    }

    private void DropItem()
    {
        Debug.Log("아이템 드랍 처리");
    }
}
