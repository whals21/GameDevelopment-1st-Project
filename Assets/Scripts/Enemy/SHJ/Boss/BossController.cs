using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ActtackManager;

// ==========================
// BossController
// - ���� ���� ����
// - ���Ϻ� �Ѿ� Ǯ ����
// - ���� Ǯ ����
// ==========================
public class BossController : MonoBehaviour,IDamageable
{
    private BossState currentState;                  // ���� ����
    [SerializeField] private int MonsterNumber;     // ���� ��ȣ
    [SerializeField] private BossPattern bossPattern; // OS ���� ������
    private BossScriptsObject myData;               // ���� �⺻ ������
    public BossScriptsObject Data => myData;

    public Transform target;                         // Ÿ��
    [SerializeField] private LayerMask playerLayer;
    public Rigidbody2D rb;
    public Rigidbody2D RB => rb;
    public Collider2D Col { get; private set; }

    public int CurrentPatternIndex { get; private set; } // ���� ���� �ε���

    // ���� ���� ���� �ɼ�, �Ѿ�, ������, ���� ��
    public BossAttackType CurrentBossAttackOption => bossPattern.BossAttackOption[CurrentPatternIndex];
    public GameObject CurrentBulletPrefab => bossPattern.bulletPrefab[CurrentPatternIndex];
    public int CurrentBulletCount => bossPattern.bulletCount[CurrentPatternIndex];
    public float CurrentAttackRayLength => bossPattern.attackRayLength[CurrentPatternIndex];
    public float CurrentAttackRange => bossPattern.attackRange[CurrentPatternIndex];
    public float CurrentDelayAfter => bossPattern.delayAfter[CurrentPatternIndex];
    public float CurrentDamage => bossPattern.damage[CurrentPatternIndex];
    public float CurrentDamageMove => bossPattern.damageMove[CurrentPatternIndex];

    // ���� ����
    public GameObject[] WarningPadPrefabs => bossPattern.warningPads;
    private BossFirePool[] firePools;
    public int[] WarningPadCounts => bossPattern.warningPadCount;
    private List<GameObject>[] padPools;

    [Header("���� ��Ÿ��")]
    [SerializeField] private float attackCooldown = 5f;
    private float attackTimer = 0f;

    public Transform PatternsRoot { get; private set; }   // ���� ���� ��Ʈ
    public BossAttack attackComp;                         // ���� ������Ʈ
    public BoosAttackRay attackRay;
    public BossRange attackRange;
    public BossFirePool FirePool;                          // �Ѿ� Ǯ

    [Header("Treasure Chest")]
    [Tooltip("보스 사망 시 생성할 보물상자 프리팹")]
    [SerializeField] private GameObject treasureChestPrefab;

    [SerializeField] private float currentHP;
    public float CurrentHP => currentHP;
    public float MaxHP => myData.hp;

    // ==========================
    // Awake: ������Ʈ ĳ�� �� ���� ��Ʈ ����
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
        SetTargetAutomatically();
    }

    // ==========================
    // Start: �ʱ�ȭ
    // ==========================
    private void Start()
    {
        if (attackRay != null) attackRay.Initialize(this);

        myData = BossManager.Instance.BossDatas[MonsterNumber];

        attackComp = GetComponent<BossAttack>() ?? gameObject.AddComponent<BossAttack>();
        attackComp.Initialize(this);

        SetState(new BossMove(this));

        InitializePadPool();       // ���� Ǯ �ʱ�ȭ
        CreateAllPatternRoots();   // ���� ��Ʈ ����
        InitializeAllFirePools();           // FirePool �ʱ�ȭ
        currentHP = myData.hp;
    }

    // ==========================
    // Update: ���� ������Ʈ �� ���� ����
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
    // ���� ����
    // - CurrentPatternIndex ����
    // - �Ѿ� Ǯ �ʱ�ȭ
    // ==========================
    public void SetPattern(int index)
    {
        CurrentPatternIndex = index;

        GameObject prefab = CurrentBulletPrefab;
        int count = CurrentBulletCount;

        // �� ������ �Ѿ� �� �� �� ����
        if (prefab == null || count <= 0)
        {
            Debug.Log($"[BossController] Pattern {index} �Ѿ� ���� �� Ǯ ���� ����");
            return;
        }

        // FirePool ������ ����
        if (FirePool == null)
        {
            GameObject obj = new GameObject("FirePool");
            obj.transform.SetParent(transform);
            FirePool = obj.AddComponent<BossFirePool>();
        }

        //  �ٽ�: ���� �ε����� �´� ������ Ǯ �ʱ�ȭ
        FirePool.Initialize(prefab, count, this);

        Debug.Log($"[BossController] Pattern {index} �Ѿ� Ǯ �غ� �Ϸ� ({prefab.name} x {count})");
    }

    // ==========================
    // ���� Ǯ �ʱ�ȭ
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
    // ���� ��������/��ȯ
    // ==========================
    public List<GameObject> GetWarningPads(int type, int count)
    {
        List<GameObject> result = new List<GameObject>();
        if (count <= 0) return result;

        if (type < 0 || type >= padPools.Length)
        {
            Debug.LogWarning($"WarningPad type ���� ���� type={type}");
            return result;
        }

        var pool = padPools[type];
        int available = 0;
        foreach (var pad in pool)
            if (!pad.activeSelf) available++;

        if (available < count)
        {
            Debug.LogWarning($"WarningPad ���� type={type} ��û={count} ����={available}");
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
            pool.Initialize(prefab, count, this); // ���⼭ BossController �ѱ�

            firePools[i] = pool;
        }
    }

    // ==========================
    // ���� ���� �Ѿ� �ҷ�����
    // ==========================
    public GameObject GetCurrentPatternBullet()
    {
        BossFirePool pool = firePools[CurrentPatternIndex];

        if (pool == null)
        {
            Debug.LogWarning($"Pattern {CurrentPatternIndex} �Ѿ� Ǯ ����");
            return null;
        }

        return pool.GetBullet();
    }

    // ==========================
    // ���� ���� ����
    // ==========================
    private void TryStartRandomAttack()
    {
        if (bossPattern.BossAttackOption == null || bossPattern.BossAttackOption.Length == 0)
        {
            Debug.LogError("BossAttackOption not set in BossPattern!");
            return;
        }

        int patternCount = bossPattern.BossAttackOption.Length;
        SetPattern(Random.Range(0, patternCount)); // ���� ���� + FirePool �ʱ�ȭ

        attackComp.TryStartAttack(CurrentBossAttackOption);
    }

    // ==========================
    // ���� ��Ʈ ����
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

    public void SetTargetAutomatically()
    {
        if (target != null) return; // �̹� �����Ǿ� ������ ����

        if (PlayerStats.Instance != null)
        {
            target = PlayerStats.Instance.transform;
            Debug.Log($"[{name}] Ÿ�� �ڵ� ���� �Ϸ�: {target.name}");
        }
        else
        {
            Debug.LogWarning($"[{name}] PlayerStats.Instance�� �������� �ʾ� Ÿ�� ���� ����");
        }
    }

    public void TakeDamage(float damage, SkillBase source = null, bool isCritical = false)
    {
        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0f);

        // [조민희 추가] 스킬 통계 기록 (소스가 있을 때만)
        if (source != null && source.Data != null && StatisticsManager.Instance != null)
        {
            StatisticsManager.Instance.RecordDamage(source.Data.skillName, damage);
        }

        // 데미지 텍스트
        if (ObjectPoolManager.Instance != null)
        {
            DamageText text = ObjectPoolManager.Instance.GetDamageText();
            if (text != null)
                text.Init(damage, isCritical, transform.position);
        }

        // 사망 체크
        if (currentHP <= 0f)
        {
            Die();
        }
    }

    // 0101 조민희 추가
    /// <summary>
    /// 보스 사망 처리
    /// </summary>
    private void Die()
    {
        // 보물상자 스폰
        SpawnTreasureChest();

        // 여기서 끄지 마라
        // gameObject.SetActive(false);

        // BossDie에게 위임
        GetComponent<BossDie>()?.Die();
    }

    /// <summary>
    /// 보물상자 생성
    /// </summary>
    private void SpawnTreasureChest()
    {
        if (treasureChestPrefab == null)
        {
            return;
        }

        // 보스 위치에 보물상자 생성
        Vector3 spawnPosition = transform.position;
        Instantiate(treasureChestPrefab, spawnPosition, Quaternion.identity);
    }
}


