using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ActtackManager;


public class Enemy : MonoBehaviour
{
    [Header("������ SO")]
    [SerializeField] private EnemyObject enemyData;
    public EnemyObject Data => enemyData;


    public int MaxHP => Data.EnemyHP;
    [Header("ü��")]
    [SerializeField] private int currentHP;
    public int CurrentHP => currentHP;

    [Header("�̵�")]
    [SerializeField] private float moveSpeed;
    public float Speed => moveSpeed;

    [SerializeField] private Transform target;
    public Transform Target => target;

    [Header("전투")]
    private float contactDamage;
    public float ContactDamage => contactDamage;

    private int expValue;
    public int ExpValue => expValue;

    public Rigidbody2D rb { get; private set; }
    public SpriteRenderer sr { get; private set; }
    public Animator anim { get; private set; }
    public Collider2D col { get; private set; }

    [SerializeField] public float AttackDelay;
    public float Delay=> Data.attackDelay;
    [SerializeField] private float AttackRange;
    public float Range => Data.attackRange;
    public GameObject ProjectilePrefab => Data.projectilePrefab;
    public Transform FirePoint => Data.firePoint != null ? Data.firePoint : transform;
    public bool IsRanged => Data.isRanged;

    public bool IsRange => Data.attackRange > 0f;

    [SerializeField] private float stopDistance = 3f;
    public float StopDistance => stopDistance;

    public Transform point;
    [SerializeField] private int tier;

    public int PoolIndex { get; private set; }

    GameManager gameManager;
    DataManager dataManager;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        col = GetComponent<Collider2D>();

        // enemyData가 할당되어 있는 경우에만 초기화
        if (enemyData != null)
        {
            currentHP = MaxHP;
            moveSpeed = Data.moveSpeed;
        }

        // 플레이어 자동 찾기 (null 체크)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else
        {
            Debug.LogError("Enemy: Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }

        point = transform;
    }
    void Start()
    {

    }

    // 오브젝트 풀에서 가져올 때 호출 
    public void Init(EnemyObject data)
    {
        PoolIndex = data.poolIndex; // ★ 이 줄이 핵심

        enemyData = data;
        currentHP = data.EnemyHP;
        moveSpeed = data.moveSpeed;
        //contactDamage = data.contactDamage;
        //expValue = data.expValue;

        AttackRange = data.attackRange;
        AttackDelay = data.attackDelay;
        // 플레이어 재확인 (풀에서 재사용 시)
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        point = transform;
        FindPlayer();

        EnemyShooter shooter = GetComponent<EnemyShooter>();
        
    }
    public void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
        }
        else
        {
            target = null;
            Debug.LogWarning("Enemy: Player를 찾을 수 없습니다!");
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (target != null && target.gameObject == null)
        {
            target = null;
        }

       
       


    }

    public bool IsPlayerInAttackRange()
    {
        if (target == null) return false;
        float distance = Vector2.Distance(FirePoint.position, target.position);
        return distance <= Range;
    }
    // 데미지 받기
    public void TakeDamage(float damage, bool isCritical = false)
    {
        currentHP -= (int)damage;

        if (ObjectPoolManager.Instance != null)
        {
            // 매니저에게 요청
            DamageText text = ObjectPoolManager.Instance.GetDamageText();

            // 텍스트가 정상적으로 왔다면?
            if (text != null)
            {
                // 데미지 값, 크리티컬 여부, 위치를 알려주며 초기화
                text.Init(damage, isCritical, transform.position);
            }
        }

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    // 사망 처리
    private void Die()
    {
        PlayerHUD.Instance?.AddKill();

        if (PlayerHUD.Instance != null)
        {
            PlayerHUD.Instance.AddKill();
        }

        // EnemySpawner에 알림
        if (GameManager.Instance != null)
        {
            int tier = GameManager.Instance.currentTierIndex;

            ExpGem gem = ObjectPoolManager.Instance.GetExpGem(tier);
            if (gem != null)
            {
                gem.transform.position = transform.position;
            }
        }



       
      



        // 경험치 드롭 (나중에 구현)
        // ExpGem gem = ObjectPoolManager.Instance.GetExpGem();
        // gem.transform.position = transform.position;
        // gem.Init(expValue);

        // 풀로 반환    
        ObjectPoolManager.Instance.ReturnEnemy(this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!IsRange) return;

        Transform center = point != null ? point : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center.position, Range);
    }
}
