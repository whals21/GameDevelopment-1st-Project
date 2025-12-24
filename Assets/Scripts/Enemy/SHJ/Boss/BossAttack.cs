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
    protected BossController boss;        // 패턴이 참조하는 보스
    protected BoosAttackRay attackRay;    // 공격용 레이
    protected BossRange attackRange;      // 공격 범위

    protected float warningTimer;         // 경고 단계 시간 누적
    protected bool executed;              // Execute 호출 여부

    protected GameObject warningPad;      // 경고용 발판 단일
    protected bool padSpawned;            // 발판 생성 여부 확인
    protected const float WARNING_TIME = 3f;

    protected List<GameObject> warningPads = new List<GameObject>(); // 공격용 발판 리스트

    [Header("Warning Pad Settings")]
    protected int warningPadType = 0;
    protected int warningPadCount = 1;

    // ----------------------
    // 패턴 루트/발판/총알 관리용 Transform
    // - 각 패턴 별로 Pattern001, Pattern002 등 생성
    // - 하위에 WarningPads, Bullets 생성
    // ----------------------
    protected Transform patternRoot;
    protected Transform warningPadRoot;
    protected Transform bulletRoot;

    

    // ----------------------
    // 초기화: 보스 참조 전달 + 패턴 루트 생성
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
    // 공격 가능 조건 체크 (범위/레이 등)
    // ----------------------
    public abstract bool CanExecute();

    // ----------------------
    // 매 프레임 호출: 경고 및 공격 진행
    // ----------------------
    public void Tick()
    {
        if (!executed) // 경고 단계
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
            OnAttackTick(); // 공격 진행 단계
        }
    }

    // ----------------------
    // 경고 단계 업데이트 (발판/시각 효과)
    // ----------------------
    protected abstract void UpdateWarning();

    // ----------------------
    // 공격 실행 단계
    // ----------------------
    public abstract void Execute();

    // ----------------------
    // 공격 진행 중 반복 Tick
    // ----------------------
    protected virtual void OnAttackTick() { }

    // ----------------------
    // 공격 종료 처리
    // ----------------------
    public abstract void Exit();

    // ----------------------
    // 공격 완료 여부
    // ----------------------
    public abstract bool IsFinished { get; }
}

// ----------------------
// BossAttackPattern001
// 역할: 단일 패턴 예시 (대시 공격)
// - 경고 발판 표시
// - 레이 감지로 플레이어 위치 LOCK
// - Execute 시점에서 실제 공격용 발판 생성
// - 돌진 후 도착 시 공격 종료
// ----------------------

class BossAttackPattern001 : BossAttackBase
{
    private bool finished;            // 공격 완료
    private bool dashStarted;         // 돌진 시작 여부
    private Vector2 lockedTargetPos;  // 경고 단계에서 잠근 플레이어 위치
    private Vector2 dashDir;          // 돌진 방향
    private Vector2 warningOriginPos;

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
        if (boss == null)
        {
            Debug.LogError("[BossAttackPattern001] boss is NULL");
            return false;
        }

        if (boss.attackRay == null)
        {
            Debug.LogError("[BossAttackPattern001] attackRay is NULL");
            return false;
        }

        if (boss.rb == null)
        {
            Debug.LogError("[BossAttackPattern001] Rigidbody2D is NULL");
            return false;
        }

        if (boss.target == null)
        {
            Debug.Log("[BossAttackPattern001] target 없음");
            return false;
        }

        // 눈 방향 갱신 (중요)
        boss.attackRay.UpdateDirection();

        RaycastHit2D hit = boss.attackRay.Fire();

        if (hit.collider == null)
        {
            Debug.Log("[BossAttackPattern001] 레이 감지 실패");
            return false;
        }

        // 감지 성공
        Debug.Log($"[BossAttackPattern001] 레이 감지 성공 : {hit.collider.name}");

        lockedTargetPos = hit.point;
        dashDir = (lockedTargetPos - (Vector2)boss.rb.position).normalized;

        warningOriginPos = lockedTargetPos;

        return true;
    }


    // 경고 단계: 발판 생성 및 표시
    protected override void UpdateWarning()
    {
        warningElapsed += Time.deltaTime;

        if (warningPads.Count == 0)
        {
            warningPadType = 0;
            warningPadCount = 1;

            warningPads = boss.GetWarningPads(warningPadType, warningPadCount);

            if (warningPads.Count == 0)
            {
                Debug.LogWarning("[BossAttackPattern001] WarningPad 생성 실패");
                return;
            }

            float spacing = 1.2f;

            for (int i = 0; i < warningPads.Count; i++)
            {
                Vector2 pos =
                    warningOriginPos + dashDir * spacing * i;

                warningPads[i].transform.position = pos;
            }
        }
    }

    // 공격 실행
    public override void Execute()
    {
        // 경고 종료 → 발판 제거
        foreach (var pad in warningPads)
            boss.ReturnWarningPad(pad);

        warningPads.Clear();

        //공격 시작
        dashStarted = true;
    }

    // 공격 진행 Tick
    protected override void OnAttackTick()
    {
        if (!dashStarted) return;

        float dashSpeed = boss.CurrentDamageMove;
        boss.rb.velocity = dashDir * dashSpeed;

        float distance = Vector2.Distance(boss.rb.position, lockedTargetPos);
        if (distance < 0.5f)
        {
            finished = true;
        }
    }

    // 공격 종료: 발판 반환, 상태 이동으로 복귀
    public override void Exit()
    {
        boss.rb.velocity = Vector2.zero;
        boss.SetState(new BossMove(boss));
    }
}
