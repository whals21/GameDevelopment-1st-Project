using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDieState : StateMachineBehaviour
{
    private BossDie bossDie;
    private Enemy enemy;
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
        enemy = animator.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            // Enemy 이동, 공격 등 필요 시 여기서 멈출 수 있음
            // 하지만 풀 반환은 애니 종료 후 처리
            enemy.rb.velocity = Vector2.zero; // 이동 정지
            // 필요하다면 충돌 처리/공격도 여기서 멈출 수 있음
        }
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var bossDie = animator.GetComponentInParent<BossDie>();
        if (bossDie != null)
        {
            bossDie.OnDeathAnimationFinished();

        }
        if (enemy != null)
        {
            // Enemy 애니 종료 후 풀 반환
            ObjectPoolManager.Instance.ReturnEnemy(enemy);
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
