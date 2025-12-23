using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMove : BossState
{
    private Transform target;

    public BossMove(BossController boss) : base(boss)
    {
        // 플레이어 타겟 설정
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    public override void FixedUpdateState()
    {
        if (target == null) return;

        Vector2 dir = (target.position - boss.transform.position).normalized;

        // 이동 속도는 Boss 데이터에서 가져옴
        float speed = boss.Data.move;

        boss.rb.velocity = dir * speed;
    }

    public override void Exit()
    {
        // 상태 종료 시 이동 정지
        boss.rb.velocity = Vector2.zero;
    }
}
