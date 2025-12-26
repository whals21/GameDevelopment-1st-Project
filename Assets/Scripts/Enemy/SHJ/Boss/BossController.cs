using System.Collections.Generic;
using UnityEngine;
using static ActtackManager;

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
    public BossAttackType CurrentBossAttackOption =>
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

    [Header("다음 패턴 발동 시간 설정")]
    [SerializeField] private float attackCooldown = 5f;        // 패턴 간 최소 대기 시간
    private float attackTimer = 0f;
    public Transform PatternsRoot { get; private set; }

    private void Awake()
    {
        // 컴포넌트 캐싱
        rb = GetComponent<Rigidbody2D>();
        Col = GetComponent<Collider2D>();

        attackRange = GetComponentInChildren<BossRange>(true);

        if (attackRay == null)
            attackRay = GetComponentInChildren<BoosAttackRay>(true);

        if (attackRay == null)
            Debug.LogError("[BossController] BoosAttackRay NOT FOUND");
        else
            Debug.Log($"[BossController] BoosAttackRay FOUND : {attackRay.name}");

        PatternsRoot = transform.Find("Patterns");

        if (PatternsRoot == null)
        {
            GameObject patterns = new GameObject("Patterns");
            patterns.transform.SetParent(transform);
            PatternsRoot = patterns.transform;
        }


    }


    private void Start()
    {
        InitializePadPool();
        CreateAllPatternRoots();

        if (attackRay != null)
            attackRay.Initialize(this);

        // 보스 데이터 로드
        myData = BossManager.Instance.BossDatas[MonsterNumber];

        // 공격 컴포넌트
        attackComp = GetComponent<BossAttack>();
        if (attackComp == null)
            attackComp = gameObject.AddComponent<BossAttack>();

        attackComp.Initialize(this);

        SetState(new BossMove(this));

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
        if (WarningPadPrefabs == null || WarningPadCounts == null)
        {
            Debug.LogError("[PadPool] WarningPad 데이터 NULL");
            return;
        }

        int typeCount = WarningPadPrefabs.Length;
        padPools = new List<GameObject>[typeCount];

        // 이게 핵심
        int maxCount = 0;
        foreach (int c in WarningPadCounts)
            if (c > maxCount)
                maxCount = c;

        for (int type = 0; type < typeCount; type++)
        {
            padPools[type] = new List<GameObject>();

            GameObject prefab = WarningPadPrefabs[type];
            if (prefab == null)
            {
                Debug.LogError($"[PadPool] type={type} prefab NULL");
                continue;
            }

            // 각 타입마다 "최대 필요 수" 만큼만 생성
            for (int i = 0; i < maxCount; i++)
            {
                var pad = Instantiate(prefab, transform);
                pad.SetActive(false);
                padPools[type].Add(pad);
            }
        }
    }


    // 특정 타입의 사용 가능한 발판 하나 반환
    public List<GameObject> GetWarningPads(int type, int count)
    {
        List<GameObject> result = new List<GameObject>();

        // 0개 요청이면 그냥 빈 리스트 반환
        if (count <= 0)
            return result;

        if (type < 0 || type >= padPools.Length)
        {
            Debug.LogWarning($"[BossController] WarningPad type 범위 오류 type={type}");
            return result;
        }

        var pool = padPools[type];

        int available = 0;
        foreach (var pad in pool)
            if (!pad.activeSelf)
                available++;

        if (available < count)
        {
            Debug.LogWarning(
                $"[BossController] WarningPad 부족 type={type} 요청={count} 실제={available}"
            );
            return result;
        }

        foreach (var pad in pool)
        {
            if (!pad.activeSelf)
            {
                pad.SetActive(true);
                result.Add(pad);
                if (result.Count >= count)
                    break;
            }
        }

        return result;
    }


    // 발판 하나 반환
    public void ReturnWarningPad(GameObject pad)
    {
        if (pad == null) return;
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

    private void CreateAllPatternRoots()
    {
        if (bossPattern?.BossAttackOption == null)
            return;

        foreach (var type in bossPattern.BossAttackOption)
        {
            CreatePatternRootIfMissing(type);
        }
    }
    /// <summary>
    /// PatternsRoot 하위에 특정 패턴 루트와 하위 BulletRoots, PadRoots 생성/확인
    /// </summary>
    public Transform CreatePatternRootIfMissing(BossAttackType type)
    {
        if (PatternsRoot == null)
        {
            PatternsRoot = transform.Find("Patterns");
            if (PatternsRoot == null)
            {
                GameObject patternsObj = new GameObject("Patterns");
                patternsObj.transform.SetParent(transform);
                PatternsRoot = patternsObj.transform;
            }
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

        // 하위 루트 생성
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
    public Transform GetOrCreatePatternRoot(BossAttackType type)
    {
        return CreatePatternRootIfMissing(type);
    }

    public Transform GetPadRoot(BossAttackType type)
    {
        Transform pattern = GetOrCreatePatternRoot(type);
        Transform padRoot = pattern.Find("PadRoots");
        if (padRoot == null)
        {
            GameObject padObj = new GameObject("PadRoots");
            padObj.transform.SetParent(pattern);
            padObj.transform.localPosition = Vector3.zero;
            padRoot = padObj.transform;
        }
        return padRoot;
    }
}
