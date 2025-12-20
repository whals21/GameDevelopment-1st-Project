using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossState
{
    protected BossController boss;

    public BossState(BossController boss)
    {
        this.boss = boss;
    }

    // 상태 진입 시 1회 실행
    public virtual void Enter() { }

    // 매 프레임 로직 (판단, 상태 전환)
    public virtual void UpdateState() { }

    // 물리 처리 (이동, Rigidbody)
    public virtual void FixedUpdateState() { }

    // 상태 종료 시 1회 실행
    public virtual void Exit() { }
}
