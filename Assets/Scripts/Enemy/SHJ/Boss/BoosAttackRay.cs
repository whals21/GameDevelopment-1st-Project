using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoosAttackRay : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayer;

    private BossController boss;
    private Vector2 currentDir;     // 계속 갱신되는 방향
    private Vector2 fireDir;        // 발사 시 고정 방향

    public void Initialize(BossController controller)   //일단 연결 부모하고
    {
        boss = controller;
    }

    // 예고 단계에서 매 프레임 호출
    public void UpdateDirection()       //애가 업데이트 되어야 대상을 찾겠지?
    {
        if (boss.target == null) return;

        currentDir =
            (boss.target.position - transform.position).normalized;
    }

  

    // 공격 발동 순간
    public void Fire()
    {
        fireDir = currentDir;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            fireDir,
            boss.CurrentAttackRayLength,
            targetLayer
        );

        if (hit.collider != null)//패턴 발동되겠지?
        {
            
        }
    }


}
