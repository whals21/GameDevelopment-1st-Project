using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ActtackManager;

// ==========================
// BossController
// - 랜덤 패턴 선택
// - 패턴별 총알 풀 연동
// - 발판 풀 관리
// ==========================
public class BossController : MonoBehaviour
{
    private BossState currentState;                  // 현재 상태
    [SerializeField] private int MonsterNumber;     // 보스 번호
    [SerializeField] private BossPattern bossPattern; // OS 패턴 데이터
    private BossScriptsObject myData;               // 보스 기본 데이터
    public BossScriptsObject Data => myData;

    public Transform target;                         // 타겟
    [SerializeField] private LayerMask playerLayer;
    public Rigidbody2D rb;
    public Rigidbody2D RB => rb;
    public Collider2D Col { get; private set; }

    public int CurrentPatternIndex { get; private set; } // 현재 패턴 인덱스

    // 현재 패턴 공격 옵션, 총알, 데미지, 범위 등
    public BossAttackType CurrentBossAttackOption => bossPattern.BossAttackOption[CurrentPatternIndex];
    public GameObject CurrentBulletPrefab => bossPattern.bulletPrefab[CurrentPatternIndex];
    public int CurrentBulletCount => bossPattern.bulletCount[CurrentPatternIndex];
    public float CurrentAttackRayLength => bossPattern.attackRayLength[CurrentPatternIndex];
    public float CurrentAttackRange => bossPattern.attackRange[CurrentPatternIndex];
    public float CurrentDelayAfter => bossPattern.delayAfter[CurrentPatternIndex];
    public float CurrentDamage => bossPattern.damage[CurrentPatternIndex];
    public float CurrentDamageMove => bossPattern.damageMove[CurrentPatternIndex];

    // 발판 관련
    public GameObject[] WarningPadPrefabs => bossPattern.warningPads;
    private BossFirePool[] firePools;
    public int[] WarningPadCounts => bossPattern.warningPadCount;
    private List<GameObject>[] padPools;

    [Header("패턴 쿨타임")]
    [SerializeField] private float attackCooldown = 5f;
    private float attackTimer = 0f;

    public Transform PatternsRoot { get; private set; }   // 하위 패턴 루트
    private BossAttack attackComp;                         // 공격 컴포넌트
    public BoosAttackRay attackRay;
    public BossRange attackRange;
    public BossFirePool FirePool;                          // 총알 풀
    public BossDie bossDie;



    [SerializeField] private float currentHP;
    public float CurrentHP => currentHP;
    public float MaxHP => myData.hp;

    // ==========================
    // Awake: 컴포넌트 캐싱 및 패턴 루트 생성
    // ==========================
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Col = GetComponent<Collider2D>();
        attackRange = GetComponentInChildren<BossRange>(true);
        if (attackRay == null)
            attackRay = GetComponentInChildren<BoosAttackRay>(true);

        PatternsRoot = transform.Find("Patterns");
        if (PatternsRoot == null)
        {
            GameObject patterns = new GameObject("Patterns");
            patterns.transform.SetParent(transform);
            PatternsRoot = patterns.transform;
        }
        InitializeComponents();
        SetTargetAutomatically();
        bossDie = GetComponent<BossDie>();

    }

    // ==========================
    // Start: 초기화
    // ==========================
    private void Start()
    {
        if (attackRay != null) attackRay.Initialize(this);

        myData = BossManager.Instance.BossDatas[MonsterNumber];

        attackComp = GetComponent<BossAttack>() ?? gameObject.AddComponent<BossAttack>();
        attackComp.Initialize(this);

        SetState(new BossMove(this));

        InitializePadPool();       // 발판 풀 초기화
        CreateAllPatternRoots();   // 패턴 루트 생성
        InitializeAllFirePools();           // FirePool 초기화
        bossDie.Initialize();
        currentHP = myData.hp;
    }

    // ==========================
    // Update: 상태 업데이트 및 랜덤 공격
    // ==========================
    private void Update()
    {
        attackTimer += Time.deltaTime;

        if (attackComp != null && attackComp.IsAttacking)
        {
            attackComp.Tick();
            return;
        }

        if (attackTimer >= attackCooldown)
        {
            TryStartRandomAttack();
            attackTimer = 0f;
        }

        currentState?.UpdateState();
    }

    private void FixedUpdate() => currentState?.FixedUpdateState();

    public void SetState(BossState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    // ==========================
    // 패턴 변경
    // - CurrentPatternIndex 변경
    // - 총알 풀 초기화
    // ==========================
    public void SetPattern(int index)
    {
        CurrentPatternIndex = index;

        GameObject prefab = CurrentBulletPrefab;
        int count = CurrentBulletCount;

        // 이 패턴은 총알 안 씀 → 무시
        if (prefab == null || count <= 0)
        {
            Debug.Log($"[BossController] Pattern {index} 총알 없음 → 풀 생성 안함");
            return;
        }

        // FirePool 없으면 생성
        if (FirePool == null)
        {
            GameObject obj = new GameObject("FirePool");
            obj.transform.SetParent(transform);
            FirePool = obj.AddComponent<BossFirePool>();
        }

        //  핵심: 패턴 인덱스에 맞는 값으로 풀 초기화
        FirePool.Initialize(prefab, count, this);

        Debug.Log($"[BossController] Pattern {index} 총알 풀 준비 완료 ({prefab.name} x {count})");
    }

    // ==========================
    // 발판 풀 초기화
    // ==========================
    private void InitializePadPool()
    {
        if (WarningPadPrefabs == null || WarningPadCounts == null) return;

        int typeCount = WarningPadPrefabs.Length;
        padPools = new List<GameObject>[typeCount];

        int maxCount = 0;
        foreach (int c in WarningPadCounts)
            if (c > maxCount) maxCount = c;

        for (int type = 0; type < typeCount; type++)
        {
            padPools[type] = new List<GameObject>();
            GameObject prefab = WarningPadPrefabs[type];
            if (prefab == null) continue;

            for (int i = 0; i < maxCount; i++)
            {
                var pad = Instantiate(prefab, transform);
                pad.SetActive(false);
                padPools[type].Add(pad);
            }
        }
    }

    // ==========================
    // 발판 가져오기/반환
    // ==========================
    public List<GameObject> GetWarningPads(int type, int count)
    {
        List<GameObject> result = new List<GameObject>();
        if (count <= 0) return result;

        if (type < 0 || type >= padPools.Length)
        {
            Debug.LogWarning($"WarningPad type 범위 오류 type={type}");
            return result;
        }

        var pool = padPools[type];
        int available = 0;
        foreach (var pad in pool)
            if (!pad.activeSelf) available++;

        if (available < count)
        {
            Debug.LogWarning($"WarningPad 부족 type={type} 요청={count} 실제={available}");
            return result;
        }

        foreach (var pad in pool)
        {
            if (!pad.activeSelf)
            {
                pad.SetActive(true);
                result.Add(pad);
                if (result.Count >= count) break;
            }
        }

        return result;
    }

    public void ReturnWarningPad(GameObject pad) { if (pad == null) return; pad.SetActive(false); }
    public void ReturnAllWarningPads() { foreach (var pool in padPools) foreach (var pad in pool) pad.SetActive(false); }

    private void InitializeAllFirePools()
    {
        int patternCount = bossPattern.BossAttackOption.Length;
        firePools = new BossFirePool[patternCount];

        for (int i = 0; i < patternCount; i++)
        {
            GameObject prefab = bossPattern.bulletPrefab[i];
            int count = bossPattern.bulletCount[i];

            if (prefab == null || count <= 0)
                continue;

            GameObject obj = new GameObject($"FirePool_Pattern_{i}");
            obj.transform.SetParent(transform);

            BossFirePool pool = obj.AddComponent<BossFirePool>();
            pool.Initialize(prefab, count, this); // 여기서 BossController 넘김

            firePools[i] = pool;
        }
    }

    // ==========================
    // 현재 패턴 총알 불려오기
    // ==========================
    public GameObject GetCurrentPatternBullet()
    {
        BossFirePool pool = firePools[CurrentPatternIndex];

        if (pool == null)
        {
            Debug.LogWarning($"Pattern {CurrentPatternIndex} 총알 풀 없음");
            return null;
        }

        return pool.GetBullet();
    }

    // ==========================
    // 랜덤 공격 실행
    // ==========================
    private void TryStartRandomAttack()
    {
        if (bossPattern.BossAttackOption == null || bossPattern.BossAttackOption.Length == 0)
        {
            Debug.LogError("BossAttackOption not set in BossPattern!");
            return;
        }

        int patternCount = bossPattern.BossAttackOption.Length;
        SetPattern(Random.Range(0, patternCount)); // 패턴 선택 + FirePool 초기화

        attackComp.TryStartAttack(CurrentBossAttackOption);
    }

    // ==========================
    // 패턴 루트 생성
    // ==========================
    private void CreateAllPatternRoots()
    {
        if (bossPattern?.BossAttackOption == null) return;
        foreach (var type in bossPattern.BossAttackOption) CreatePatternRootIfMissing(type);
    }

    public Transform CreatePatternRootIfMissing(BossAttackType type)
    {
        if (PatternsRoot == null)
        {
            PatternsRoot = transform.Find("Patterns") ?? new GameObject("Patterns").transform;
            PatternsRoot.SetParent(transform);
        }

        string patternName = type.ToString();
        Transform pattern = PatternsRoot.Find(patternName);
        if (pattern == null)
        {
            GameObject patternObj = new GameObject(patternName);
            patternObj.transform.SetParent(PatternsRoot);
            patternObj.transform.localPosition = Vector3.zero;
            pattern = patternObj.transform;
        }

        if (pattern.Find("BulletRoots") == null)
        {
            GameObject bulletObj = new GameObject("BulletRoots");
            bulletObj.transform.SetParent(pattern);
            bulletObj.transform.localPosition = Vector3.zero;
        }

        if (pattern.Find("PadRoots") == null)
        {
            GameObject padObj = new GameObject("PadRoots");
            padObj.transform.SetParent(pattern);
            padObj.transform.localPosition = Vector3.zero;
        }

        return pattern;
    }

    //보스 데미지 연결
    private void InitializeComponents()
    {
        // BossDie 연결
        bossDie = GetComponent<BossDie>();
        if (bossDie == null)
        {
            bossDie = gameObject.AddComponent<BossDie>();
        }

        // 패턴 루트 연결
        PatternsRoot = transform.Find("Patterns");
        if (PatternsRoot == null)
        {
            GameObject patterns = new GameObject("Patterns");
            patterns.transform.SetParent(transform);
            PatternsRoot = patterns.transform;
        }
    }


    public void SetTargetAutomatically()
    {
        if (target != null) return; // 이미 설정되어 있으면 무시

        if (PlayerStats.Instance != null)
        {
            target = PlayerStats.Instance.transform;
            Debug.Log($"[{name}] 타겟 자동 설정 완료: {target.name}");
        }
        else
        {
            Debug.LogWarning($"[{name}] PlayerStats.Instance가 존재하지 않아 타겟 설정 실패");
        }
    }

 
}


