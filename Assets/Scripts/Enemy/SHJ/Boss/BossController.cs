using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{


    private BossState currentState;          // 현재 보스 상태 (Move, Attack 등)
    [SerializeField] private int MonsterNumber;

    [SerializeField] private BossPattern bossPattern; // OS(ScriptableObject) 패턴 데이터
    private BossScriptsObject myData;
    public BossScriptsObject Data => myData;

    public Transform target;                 // 타겟(플레이어)
    public Rigidbody2D rb;
    public Rigidbody2D RB => rb;
    public Collider2D Col { get; private set; }

    public int CurrentPatternIndex { get; private set; } // 현재 패턴 인덱스



    public GameObject CurrentBulletPrefab => bossPattern.bulletPrefab[CurrentPatternIndex];
    public int CurrentBulletCount => bossPattern.bulletCount[CurrentPatternIndex];
    public float CurrentAttackRayLength => bossPattern.attackRayLength[CurrentPatternIndex];
    public float CurrentAttackRange => bossPattern.attackRange[CurrentPatternIndex];
    public float CurrentDelayAfter => bossPattern.delayAfter[CurrentPatternIndex];
   
    public float CurrentDamage => bossPattern.damage[CurrentPatternIndex];
    public float CurrentDamageMove => bossPattern.damageMove[CurrentPatternIndex];

    // 현재 패턴에서 사용할 공격 타입(enum)
    BossAttackType CurrentBossAttackOption =>
        bossPattern.BossAttackOption[CurrentPatternIndex];


    // OS에서 설정한 발판 프리팹 배열
    public GameObject[] WarningPadPrefabs => bossPattern.warningPads;

    // OS에서 설정한 발판 개수 배열
    public int[] WarningPadCounts => bossPattern.warningPadCount;

    // 발판 타입별 풀 (warningPads 인덱스 = 타입)
    private List<GameObject>[] padPools;



    private BossAttack attackComp;            // 공격 컴포넌트
    public BoosAttackRay attackRay;
    public BossRange attackRange;

    private float attackCooldown = 2f;        // 패턴 간 최소 대기 시간
    private float attackTimer = 0f;


    private void Start()
    {
        // 보스 데이터 로드
        myData = BossManager.Instance.BossDatas[MonsterNumber];

        // 컴포넌트 캐싱
        rb = GetComponent<Rigidbody2D>();
        Col = GetComponent<Collider2D>();

        // 기본 상태 설정
        SetState(new BossMove(this));

        // 발판 Pool 초기화
        InitializePadPool();

        // 공격 컴포넌트 세팅
        attackComp = GetComponent<BossAttack>();
        if (attackComp == null)
            attackComp = gameObject.AddComponent<BossAttack>();

        attackComp.Initialize(this);
    }

    private void Update()
    {
        attackTimer += Time.deltaTime;

        // 공격 중이면 상태머신 정지, 공격 Tick만 수행
        if (attackComp != null && attackComp.IsAttacking)
        {
            attackComp.Tick();
            return;
        }

        // 공격 쿨타임이 지나면 랜덤 패턴 실행
        if (attackTimer >= attackCooldown)
        {
            TryStartRandomAttack();
            attackTimer = 0f;
        }

        // 상태머신 업데이트
        currentState?.UpdateState();
    }

    private void FixedUpdate()
    {
        currentState?.FixedUpdateState();
    }

    public void SetState(BossState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    // 현재 사용할 패턴 인덱스 설정
    public void SetPattern(int index)
    {
        CurrentPatternIndex = index;
    }

    // OS 기준으로 타입별 발판 풀 생성
    private void InitializePadPool()
    {
        int typeCount = WarningPadPrefabs.Length;
        padPools = new List<GameObject>[typeCount];

        for (int i = 0; i < typeCount; i++)
        {
            padPools[i] = new List<GameObject>();

            int count = WarningPadCounts[i];
            GameObject prefab = WarningPadPrefabs[i];

            for (int j = 0; j < count; j++)
            {
                GameObject pad = Instantiate(prefab, transform); // 컨트롤러 자식으로
                pad.SetActive(false);
                padPools[i].Add(pad);
            }
        }
    }

   
    // 특정 타입의 사용 가능한 발판 하나 반환
    public GameObject GetWarningPad(int type)
    {
        if (type < 0 || type >= padPools.Length) return null;

        foreach (var pad in padPools[type])
        {
            if (!pad.activeInHierarchy)
            {
                pad.SetActive(true);
                return pad;
            }
        }
        return null;
    }

    // 발판 하나 반환
    public void ReturnWarningPad(GameObject pad)
    {
        if (pad != null)
            pad.SetActive(false);
    }

    // 모든 발판 비활성화 (패턴 종료 시)
    public void ReturnAllWarningPads()
    {
        foreach (var pool in padPools)
        {
            foreach (var pad in pool)
                pad.SetActive(false);
        }
    }

 
    // OS에 등록된 공격 패턴 중 랜덤 실행
    private void TryStartRandomAttack()
    {
        if (bossPattern.BossAttackOption == null ||
            bossPattern.BossAttackOption.Length == 0)
        {
            Debug.LogError("BossAttackOption not set in BossPattern!");
            return;
        }

        int patternCount = bossPattern.BossAttackOption.Length;
        SetPattern(Random.Range(0, patternCount));

        attackComp.TryStartAttack(CurrentBossAttackOption);
    }
}
