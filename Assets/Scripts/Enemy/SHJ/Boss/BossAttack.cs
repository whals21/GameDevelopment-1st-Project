using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ----------------------
// 보스 공격 패턴 종류 열거형
// ----------------------
public enum BossAttackType
{
    Pattern001Ray,
    Pattern001Range,
    Pattern001None
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
        { BossAttackType.Pattern001Ray, () => new BossAttackPattern001()} ,  // 예시 1개 등록 
        { BossAttackType.Pattern001Range, () => new BossAttackPattern002()} ,
        { BossAttackType.Pattern001None, () => new BossAttackPattern003()} ,
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
/// <summary>
/// BossAttackBase
/// ----------------------
/// 모든 보스 공격 패턴의 공통 베이스 클래스
/// 역할:
///  1. 경고(Warning) 단계 관리
///  2. 공격 실행(Execute) 단계 관리
///  3. 패턴 진행(Tick)
///  4. 발판/총알 등 공용 리소스 접근
///  5. 공격 종료(Exit) 처리
/// 
/// Base 클래스에서 보스 컨트롤러(OS)를 통해
/// 공격 범위, 레이 길이, 대미지, 발판, 총알 등을
/// 공통으로 관리할 수 있음
/// </summary>
public abstract class BossAttackBase : IBossAttack
{
    // ----------------------
    // 보스 참조
    // ----------------------
    protected BossController boss;           // 패턴이 참조하는 보스
    protected BoosAttackRay attackRay;       // 보스의 공격 레이
    protected BossRange attackRange;         // 보스의 공격 범위

    // ----------------------
    // 경고 / 실행 상태
    // ----------------------
    protected float warningTimer;            // 경고 누적 시간
    protected bool executed;                 // Execute 실행 여부
    protected GameObject warningPad;         // 단일 발판
    protected bool padSpawned;               // 발판 생성 여부
    protected List<GameObject> warningPads = new List<GameObject>();

    protected int warningPadType = 0;
    protected int warningPadCount = 1;

    // ----------------------
    // OS 데이터 (보스를 통해서만 접근)
    // ----------------------
    protected float CurrentAttackRange => boss.CurrentAttackRange;
    protected float CurrentAttackRayLength => boss.CurrentAttackRayLength;
    protected float CurrentDamage => boss.CurrentDamage;
    protected float CurrentDelayAfter => boss.CurrentDelayAfter;
    protected float CurrentDamageMove => boss.CurrentDamageMove;

    // ----------------------
    // 총알 풀 (꺼내 쓰기 전용)
    // ----------------------
    protected BossFirePool firePool;          // 보스가 생성한 풀 참조

    protected Transform patternRoot;
    protected Transform warningPadRoot;
    protected Transform bulletRoot;

    // ----------------------
    // 초기화
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

        //FirePool 연동 + 방어
        firePool = boss.FirePool;
        if (firePool == null)
        {
            Debug.LogError("[BossAttackBase] FirePool이 초기화되지 않음");
        }
    }

    // ----------------------
    // 공격 가능 여부
    // ----------------------
    public abstract bool CanExecute();

    // ----------------------
    // 매 프레임 처리
    // ----------------------
    public void Tick()
    {
        if (!executed)
        {
            warningTimer += Time.deltaTime;
            UpdateWarning();

            if (warningTimer >= CurrentDelayAfter)
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

    // ----------------------
    // 패턴별 구현
    // ----------------------
    protected abstract void UpdateWarning(); // 경고 단계
    public abstract void Execute();          // 공격 실행
    protected virtual void OnAttackTick() { } // 지속 공격 (옵션)
    public abstract void Exit();             // 종료 처리
    public abstract bool IsFinished { get; } // 종료 조건
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
    

    public override bool IsFinished => finished; // 공격 종료 여부

    public override void Initialize(BossController boss)
    {
        base.Initialize(boss);
        finished = false;
        dashStarted = false;
        
       
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

        // ★ 플레이어 충돌 시 데미지
        if (boss.Col != null)
        {
            Collider2D hit = Physics2D.OverlapCircle(
                boss.rb.position,
                0.6f,                         // 판정 반경 (필요하면 조절)
                LayerMask.GetMask("Player")   // 플레이어 레이어
            );

            if (hit != null)
            {
                PlayerStats stats = hit.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    stats.TakeDamage(boss.CurrentDamage); // ★ 데미지 연동
                    finished = true;                      // 한 번 맞히면 종료
                    return;
                }
            }
        }

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

class BossAttackPattern002 : BossAttackBase
{
    private bool finished;
    private Vector2 fireOrigin;
    private Vector2 playerLastPos;
    // 발판 위치 저장용
    private Vector2[] fireTargets;

    public override bool IsFinished => finished;

    // ----------------------
    // 초기화
    // ----------------------
    public override void Initialize(BossController boss)
    {
        base.Initialize(boss);
        finished = false;
        fireTargets = new Vector2[5];
    }

    // ----------------------
    // 실행 가능 조건
    // ----------------------
    public override bool CanExecute()
    {
        if (boss == null || boss.target == null)
            return false;

        float dist = Vector2.Distance(
            (Vector2)boss.transform.position,
            (Vector2)boss.target.position
        );

        if (dist > boss.CurrentAttackRange)
            return false;

        fireOrigin = (Vector2)boss.transform.position;          // 총알 시작점 = 보스
        playerLastPos = (Vector2)boss.target.position;          // ★ 플레이어 위치 고정

        return true;
    }

    // ----------------------
    // 경고 단계 (발판 표시)
    // ----------------------
    protected override void UpdateWarning()
    {
        if (warningPads.Count > 0) return;

        warningPadType = 1;
        warningPadCount = 5;

        warningPads = boss.GetWarningPads(warningPadType, warningPadCount);
        if (warningPads.Count < warningPadCount)
            return;

        Vector2 dir = ((Vector2)boss.target.position - fireOrigin).normalized;
        Vector2 right = Vector2.Perpendicular(dir).normalized;
        float spacing = 5.5f;

        // ★ 발판 중심을 플레이어 위치로 변경
        warningPads[0].transform.position = playerLastPos - right * spacing * 2;
        warningPads[1].transform.position = playerLastPos - right * spacing;
        warningPads[2].transform.position = playerLastPos;
        warningPads[3].transform.position = playerLastPos + right * spacing;
        warningPads[4].transform.position = playerLastPos + right * spacing * 2;

        // ★ 발판 위치 저장
        for (int i = 0; i < 5; i++)
        {
            fireTargets[i] = warningPads[i].transform.position;
        }
    }

    // ----------------------
    // 공격 실행 (발판 위치로 총알 발사)
    // ----------------------
    public override void Execute()
    {
        // 발판 반환
        foreach (var pad in warningPads)
            boss.ReturnWarningPad(pad);
        warningPads.Clear();

        // ★ 발판 위치로 총알 5발 발사
        for (int i = 0; i < fireTargets.Length; i++)
        {
            GameObject bulletObj = firePool.GetBullet();
            if (bulletObj == null)
                continue;

            bulletObj.transform.position = fireOrigin;

            Vector2 dir = (fireTargets[i] - fireOrigin).normalized;

            BossBullet bullet = bulletObj.GetComponent<BossBullet>();
            bullet.FireTargeted(dir,fireTargets[i]);
        }

        finished = true;
    }

    // ----------------------
    // 종료 처리
    // ----------------------
    public override void Exit()
    {
        boss.SetState(new BossMove(boss));
    }
}

class BossAttackPattern003 : BossAttackBase
{
    private bool finished;
    private Vector2[] firePads;      // 발판 위치 저장
    private float damageDelay = 0.1f; // 플레이어 감지 딜레이

    public override bool IsFinished => finished;

    public override void Initialize(BossController boss)
    {
        base.Initialize(boss);
        finished = false;
        firePads = new Vector2[6]; // 발판 6개
    }

    public override bool CanExecute()
    {
        return boss != null;
    }

    protected override void UpdateWarning()
    {
        if (warningPads.Count > 0) return;

        warningPadType = 1;
        warningPadCount = 6;

        warningPads = boss.GetWarningPads(warningPadType, warningPadCount);

        if (warningPads.Count < 6)
        {
            Debug.LogWarning("[BossAttackPattern003] WarningPad 생성 실패");
            return;
        }

        Vector2 center = boss.rb.position;

        float spacingXTopBottom = 3f;
        float spacingYTop = 2f;
        float spacingYCenter = 0f;
        float spacingYBottom = -2f;
        float spacingXCenter = 5f;

        // 1행
        warningPads[0].transform.position = center + new Vector2(-spacingXTopBottom, spacingYTop);
        warningPads[1].transform.position = center + new Vector2(spacingXTopBottom, spacingYTop);

        // 2행
        warningPads[2].transform.position = center + new Vector2(-spacingXCenter, spacingYCenter);
        warningPads[3].transform.position = center + new Vector2(spacingXCenter, spacingYCenter);

        // 3행
        warningPads[4].transform.position = center + new Vector2(-spacingXTopBottom, spacingYBottom);
        warningPads[5].transform.position = center + new Vector2(spacingXTopBottom, spacingYBottom);

        // 발판 위치 저장
        for (int i = 0; i < 6; i++)
        {
            firePads[i] = warningPads[i].transform.position;
        }
    }

    public override void Execute()
    {
        // 발판 반환 (패턴002처럼)
        foreach (var pad in warningPads)
            boss.ReturnWarningPad(pad);
        warningPads.Clear();

        // 발판 위치에서 총알 생성 + 순간 등장
        for (int i = 0; i <= firePads.Length; i++)
        {
            GameObject bulletObj = firePool.GetBullet();
            if (bulletObj == null) continue;

            bulletObj.transform.position = firePads[i];

            BossBullet bullet = bulletObj.GetComponent<BossBullet>();
            if (bullet != null)
            {
                bullet.FireBlink(0.2f); // 0.2초 정도 깜빡임 후 자동 반환
            }
        }

        finished = true;
    }

    // 일정 시간 동안 발판 영역 내 플레이어 감지 및 데미지
    

    protected override void OnAttackTick() { }

    public override void Exit()
    {
        boss.rb.velocity = Vector2.zero;
        boss.SetState(new BossMove(boss));
    }
}
