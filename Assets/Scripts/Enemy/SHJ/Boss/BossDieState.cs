using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDieState : StateMachineBehaviour
{
    private BossDie bossDie;

    // Die 상태 진입 시
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        bossDie = animator.GetComponentInParent<BossDie>();
        if (bossDie != null && bossDie.bossController != null)
        {
            // 모든 기능 멈추기
            var bc = bossDie.bossController;
            bc.SetState(null);                // 상태 머신 멈춤
            bc.ReturnAllWarningPads();        // 발판 리셋
            if (bc.attackComp != null)
                bc.attackComp.enabled = false; // 공격 컴포넌트 비활성화
        }
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (bossDie != null)
        {
            // 죽은 보스 숨기기
            bossDie.DropItem();
            bossDie.gameObject.SetActive(false);

            // 절대 Initialize() 호출하지 않음
            // HP, 상태 등 초기화는 Spawn/리셋 시점에만
        }
    }
    //// Die 상태 종료 시 (핵심)
    //public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    if (bossDie != null)
    //    {
    //        bossDie.OnDeathAnimationFinished();
    //    }
    //}
}
