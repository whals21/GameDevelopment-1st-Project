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
    }

    // Die 상태 종료 시 (핵심)
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (bossDie != null)
        {
            bossDie.OnDeathAnimationFinished();
        }
    }
}
