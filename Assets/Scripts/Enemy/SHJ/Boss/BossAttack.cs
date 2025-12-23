using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BossAttackType
{
    Pattern001,
    Pattern002,
    Pattern003
}

public class BossAttack : MonoBehaviour
{
    private BossController boss;
    private IBossAttack currentAttack;

    // 현재 공격 중인가?
    public bool IsAttacking => currentAttack != null;

    public void Initialize(BossController controller)
    {
        boss = controller;
    }

    // 공격 시도
    public void TryStartAttack(BossAttackType type)
    {
        if (currentAttack != null)
            return;

        IBossAttack attack = BossAttackPatternFactory.Create(type);
        if (attack == null)
            return;

        attack.Initialize(boss);

        if (!attack.CanExecute())
            return;

        currentAttack = attack;

        // 공격 중에는 이동 상태 제거
        boss.SetState(null);
    }

    // 공격 Tick
    public void Tick()
    {
        if (currentAttack == null)
            return;

        currentAttack.Tick();

        if (currentAttack.IsFinished)
        {
            currentAttack.Exit();
            currentAttack = null;
        }
    }
}
public static class BossAttackPatternFactory    // 내가 바로 공격패턴들을 등록하는 팩터리야
{
    private static Dictionary<BossAttackType, Func<IBossAttack>> table
        = new Dictionary<BossAttackType, Func<IBossAttack>>()
    {
        { BossAttackType.Pattern001, () => new BossAttackPattern001() },
        { BossAttackType.Pattern002, () => new BossAttackPattern002() },
        { BossAttackType.Pattern003, () => new BossAttackPattern003() },
    };

    public static IBossAttack Create(BossAttackType type)
    {
        if (!table.TryGetValue(type, out var creator))
        {
            UnityEngine.Debug.LogError($"패턴 등록 안 됨: {type}");
            return null;
        }

        return creator();
    }
}

public abstract class BossAttackBase : IBossAttack  //내 역할은 상태머신
{

    protected BossController boss;

    protected float warningTimer;     // 경고 타이머
    protected bool executed;           // Execute 되었는가

    protected GameObject warningPad;  // 사용 중인 발판
    protected bool padSpawned;

    protected const float WARNING_TIME = 3f;

    public virtual void Initialize(BossController boss)
    {
        this.boss = boss;
        warningTimer = 0f;
        executed = false;
        padSpawned = false;
        warningPad = null;
    }

    // 사거리 체크
    protected bool CheckRange()
    {
        if (boss == null || boss.Col == null || boss.target == null)
            return false;

        if (boss.CurrentAttackRange <= 0)
            return false;

        Collider2D hit = Physics2D.OverlapCircle(
            boss.transform.position,
            boss.CurrentAttackRange,
            LayerMask.GetMask("Player")
        );

        return hit != null;
    }

    // 레이 체크
    protected bool CheckRay()
    {
        if (boss.CurrentAttackRayLength <= 0)
            return false;

        RaycastHit2D hit = Physics2D.Raycast(
            boss.transform.position,
            boss.transform.right,
            boss.CurrentAttackRayLength,
            LayerMask.GetMask("Player")
        );

        return hit.collider != null;
    }

    public abstract bool CanExecute();

    // 상태머신 Tick
    public void Tick()
    {
        if (!executed)
        {
            warningTimer += Time.deltaTime;
            UpdateWarning();

            if (warningTimer >= WARNING_TIME)
            {
                Execute();
                executed = true;
            }
        }
        else
        {
            OnAttackTick();
        }
    }

    protected abstract void UpdateWarning();
    public abstract void Execute();
    protected virtual void OnAttackTick() { }
    public abstract void Exit();
    public abstract bool IsFinished { get; }
}       
class BossAttackPattern001 : BossAttackBase
{
    private bool finished;
    private bool dashStarted;
    private Vector2 lockedTargetPos;
    private Vector2 dashDir;

    private const float WARNING_DURATION = 3f;
    private float warningElapsed;

    public override bool IsFinished => finished;

    public override void Initialize(BossController boss)
    {
        base.Initialize(boss);
        finished = false;
        dashStarted = false;
        warningElapsed = 0f;
    }

    public override bool CanExecute()
    {
        if (!CheckRange() && !CheckRay())
            return false;

        if (boss.target == null)
            return false;

        // 마지막 감지 위치 LOCK
        lockedTargetPos = boss.target.position;
        dashDir = (lockedTargetPos - (Vector2)boss.transform.position).normalized;

        return true;
    }

    protected override void UpdateWarning()
    {
        if (padSpawned) return;

        // 컨트롤러 발판 풀 사용
        warningPad = boss.GetWarningPad(0);
        if (warningPad == null) return;

        warningPad.transform.position = new Vector3(lockedTargetPos.x, lockedTargetPos.y, -1f);
        warningPad.transform.localScale = Vector3.one * 2f;

        var sr = warningPad.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 100;
            sr.color = new Color(1, 0, 0, 0.5f); // 붉은색 표시
        }

        padSpawned = true;
    }

    public override void Execute()
    {
        dashStarted = true;
    }

    protected override void OnAttackTick()
    {
        if (!dashStarted)
        {
            // 경고 타이머
            warningTimer += Time.deltaTime;

            if (!padSpawned)
            {
                warningPad = boss.GetWarningPad(0);
                if (warningPad != null)
                {
                    warningPad.transform.position = lockedTargetPos;
                    warningPad.transform.localScale = Vector3.one * 2f;
                    var sr = warningPad.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.color = new Color(1, 0, 0, 0.5f);
                    padSpawned = true;
                }
            }

            if (warningTimer >= WARNING_TIME)
            {
                dashStarted = true;
                if (warningPad != null)
                    boss.ReturnWarningPad(warningPad);
            }

            return;
        }

        // 돌격 이동
        float speed = boss.CurrentDamageMove;
        Vector2 nextPos = boss.rb.position + dashDir * speed * Time.deltaTime;
        boss.rb.MovePosition(nextPos);

        if (Vector2.Distance(boss.rb.position, lockedTargetPos) < 0.1f)
            finished = true;
    }

    public override void Exit()
    {
        if (warningPad != null)
        {
            boss.ReturnWarningPad(warningPad);
            warningPad = null;
        }

        boss.SetState(new BossMove(boss));
    }
}

class BossAttackPattern002 : IBossAttack
{
    private BossController boss;
    private bool fired;

    public bool IsFinished => fired;

    public void Initialize(BossController boss)
    {
        this.boss = boss;
        fired = false;
    }

    public bool CanExecute()
    {
        // 예: 사거리 조건
        return boss.CurrentAttackRange > 0;
    }

    public void Tick()
    {
        Execute();
    }

    public void Execute()
    {
        Debug.Log("Pattern001 Fire");
        fired = true;
    }

    public void Exit() { }
}
class BossAttackPattern003 : IBossAttack
{
    private BossController boss;
    private bool fired;

    public bool IsFinished => fired;

    public void Initialize(BossController boss)
    {
        this.boss = boss;
        fired = false;
    }

    public bool CanExecute()
    {
        // 예: 사거리 조건
        return boss.CurrentAttackRange > 0;
    }

    public void Tick()
    {
        Execute();
    }

    public void Execute()
    {
        Debug.Log("Pattern001 Fire");
        fired = true;
    }

    public void Exit() { }
}
