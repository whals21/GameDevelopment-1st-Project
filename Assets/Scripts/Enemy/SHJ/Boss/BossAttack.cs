using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ----------------------
// 보스 공격 패턴 종류 열거형
// ----------------------
public enum BossAttackType
{
    Pattern001,
    Pattern002,
    Pattern003
}

// ----------------------
// BossAttack
// 역할: 보스의 공격 흐름 관리 중앙 관리자
//  - 현재 공격 패턴 실행 여부 추적
//  - 공격 시작 조건 판단
//  - Tick 갱신 (패턴 진행)
//  - 공격 종료 처리
// ----------------------
public class BossAttack : MonoBehaviour
{
    private BossController boss;       // 보스 참조
    private IBossAttack currentAttack; // 현재 실행 중인 공격 패턴

    // 현재 공격 중인지 확인
    public bool IsAttacking => currentAttack != null;

    // 보스 참조 초기화
    public void Initialize(BossController controller)
    {
        boss = controller;
    }

    // 공격 시도
    public void TryStartAttack(BossAttackType type)
    {
        if (currentAttack != null) // 이미 공격 중이면 무시
            return;

        IBossAttack attack = BossAttackPatternFactory.Create(type); // 패턴 생성
        if (attack == null)
            return;

        attack.Initialize(boss); // 패턴 초기화

        if (!attack.CanExecute()) // 실행 조건 체크
            return;

        currentAttack = attack;

        // 공격 중에는 이동 상태 제거
        boss.SetState(null);
    }

    // 매 프레임 호출: 공격 패턴 Tick 진행
    public void Tick()
    {
        if (currentAttack == null) return;

        currentAttack.Tick(); // 패턴 Tick 호출

        // 패턴 종료 시 정리
        if (currentAttack.IsFinished)
        {
            currentAttack.Exit();
            currentAttack = null;
        }
    }
}

// ----------------------
// 공격 패턴 팩토리
// 역할: BossAttackType에 맞는 공격 패턴 객체 생성
// ----------------------
public static class BossAttackPatternFactory
{
    private static Dictionary<BossAttackType, Func<IBossAttack>> table
        = new Dictionary<BossAttackType, Func<IBossAttack>>()
    {
        { BossAttackType.Pattern001, () => new BossAttackPattern001() } // 예시 1개 등록
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

// ----------------------
// BossAttackBase
// 역할: 공통 공격 패턴 상태머신
//  - 경고(Warning) 단계
//  - 공격 실행(Execute) 단계
//  - 공격 진행(Tick)
//  - 공격 종료(Exit)
// ----------------------
public abstract class BossAttackBase : IBossAttack
{
    protected BossController boss;     // 패턴이 참조하는 보스
    protected BoosAttackRay attackRay;
    protected BossRange attackRange;

    protected float warningTimer;      // 경고 단계 시간 누적
    protected bool executed;           // 공격 실행 여부 (Execute 호출 여부)

    protected GameObject warningPad;   // 경고 단계에서 표시할 발판
    protected bool padSpawned;         // 발판 생성 여부 확인

    protected const float WARNING_TIME = 3f; // 경고 단계 지속 시간

    // ----------------------
    // 초기화
    // 패턴 시작 시 호출, 보스 참조 전달
    // ----------------------
    public virtual void Initialize(BossController boss)
    {
        this.boss = boss;
        this.attackRay = boss.attackRay;
        this.attackRange = boss.attackRange;

        warningTimer = 0f;
        executed = false;
        padSpawned = false;
        warningPad = null;
    }


    // ----------------------
    // 패턴 실행 조건 체크
    // 실제 공격 시작 가능 여부 판단
    // (범위/레이 조건 등)
    // ----------------------
    public abstract bool CanExecute();

    // ----------------------
    // Tick: 매 프레임 호출
    // 역할:
    // 1. 경고 단계 진행
    // 2. 경고 완료 시 Execute() 호출
    // 3. 공격 진행 단계 OnAttackTick() 호출
    // ----------------------
    public void Tick()
    {
        if (!executed) // 경고 단계
        {
            warningTimer += Time.deltaTime; // 시간 누적
            UpdateWarning();                // 경고 표시 처리

            if (warningTimer >= WARNING_TIME)
            {
                Execute();    // 경고 종료 → 공격 실행
                executed = true;
            }
        }
        else
        {
            OnAttackTick(); // 공격 진행 중 처리
        }
    }

    // ----------------------
    // 경고 단계 업데이트
    // 발판, 시각 효과 등 표시
    // ----------------------
    protected abstract void UpdateWarning();

    // ----------------------
    // 공격 실행
    // 경고 종료 후 실제 공격 수행
    // ----------------------
    public abstract void Execute();

    // ----------------------
    // 공격 진행 중 반복 처리
    // (대시 이동, 발사 등)
    // ----------------------
    protected virtual void OnAttackTick() { }

    // ----------------------
    // 공격 종료
    // 발판 반환, 상태 초기화, 보스 이동 상태 복귀 등
    // ----------------------
    public abstract void Exit();

    // ----------------------
    // 공격 완료 여부
    // Tick 호출 외부에서 패턴 종료 판단
    // ----------------------
    public abstract bool IsFinished { get; }
}

// ----------------------
// BossAttackPattern001
// 역할: 단일 패턴 예시 (대시 공격)
//  - 경고 발판 표시
//  - 목표 위치 LOCK 후 돌진
//  - 도착 시 종료
// ----------------------
class BossAttackPattern001 : BossAttackBase
{
    private bool finished;            // 공격 완료
    private bool dashStarted;         // 돌진 시작 여부
    private Vector2 lockedTargetPos;  // 경고 단계에서 잠근 플레이어 위치
    private Vector2 dashDir;          // 돌진 방향

    private const float WARNING_DURATION = 3f;
    private float warningElapsed;

    public override bool IsFinished => finished; // 공격 종료 여부

    public override void Initialize(BossController boss)
    {
        base.Initialize(boss);
        finished = false;
        dashStarted = false;
        warningElapsed = 0f;
    }

    // 실행 가능 조건 체크 (범위 또는 레이 감지)
    public override bool CanExecute()
    {
        if (boss.target == null)
            return false;

        RaycastHit2D hit = boss.attackRay.Fire();

        // 플레이어가 시야 안에 없으면 공격 불가
        if (hit.collider == null)
            return false;

        // 이 순간의 위치를 LOCK
        lockedTargetPos = hit.point;

        // 공격 방향도 이때 결정
        dashDir = (lockedTargetPos - (Vector2)boss.rb.position).normalized;

        return true;
    }


    // 경고 단계: 발판 생성 및 표시
    protected override void UpdateWarning()
    {
        if (padSpawned) return;

        warningPad = boss.GetWarningPad(0);
        if (warningPad == null) return;

        warningPad.transform.position = new Vector3(lockedTargetPos.x, lockedTargetPos.y, -1f);
        warningPad.transform.localScale = Vector3.one * 2f;

        var sr = warningPad.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 100;
            sr.color = new Color(1, 0, 0, 0.5f); // 붉은 경고 표시
        }

        padSpawned = true;
    }

    // 공격 실행
    public override void Execute()
    {
        dashStarted = true;
    }

    // 공격 진행 Tick
    protected override void OnAttackTick()
    {
        if (!dashStarted)
        {
            // 경고 타이머 및 발판 생성 처리
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

        // 돌진 이동
        float speed = boss.CurrentDamageMove;
        Vector2 nextPos = boss.rb.position + dashDir * speed * Time.deltaTime;
        boss.rb.MovePosition(nextPos);

        // 목표 도착 시 종료
        if (Vector2.Distance(boss.rb.position, lockedTargetPos) < 0.1f)
            finished = true;
    }

    // 공격 종료: 발판 반환, 상태 이동으로 복귀
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
