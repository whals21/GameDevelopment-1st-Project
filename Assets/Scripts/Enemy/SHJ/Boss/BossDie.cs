using System.Collections;
using UnityEngine;

// ==========================
// BossDie
// - 보스 체력 관리
// - 플레이어 공격 연동
// - 사망 시 처리 + 아이템 드랍 자리
// ==========================
public class BossDie : MonoBehaviour
{
    private BossController bossController;  // 보스 컨트롤러
    private float currentHp;

    private void Awake()
    {
        bossController = GetComponent<BossController>();
        if (bossController == null)
        {
            Debug.LogError("BossDie: BossController가 없어요!");
        }
    }

    private void Start()
    {
        if (bossController != null && bossController.Data != null)
        {
            // 초기 체력 세팅
            currentHp = bossController.Data.hp;
        }
    }

    /// <summary>
    /// 플레이어가 보스를 공격할 때 호출
    /// </summary>
    /// <param name="damage">플레이어 공격력</param>
    public void TakeDamage(float damage)
    {
        if (currentHp <= 0) return; // 이미 죽은 상태

        currentHp -= damage;

        // 체력 0 이하 시 사망 처리
        if (currentHp <= 0)
        {
            currentHp = 0;
            Die();
        }

        // 디버그 로그 또는 체력 UI 업데이트 가능
        Debug.Log($"Boss {bossController.name} HP: {currentHp}/{bossController.Data.hp}");
    }

    /// <summary>
    /// 보스 사망 처리
    /// </summary>
    private void Die()
    {
        Debug.Log($"Boss {bossController.name} 사망!");

        //보스 상태 종료
        bossController.SetState(null);

        //발판 정리
        bossController.ReturnAllWarningPads();

        //보스 비활성화
        gameObject.SetActive(false);

        //아이템 드랍
        DropItem();

        // 5. 플레이어 경험치 보상 등 처리 가능 (나중에)
        // TODO: 경험치 지급
    }

    
    /// 보스 사망 시 아이템 드랍 처리
    private void DropItem()
    {
       
        Debug.Log("아이템 드랍 처리 자리");
    }
}
