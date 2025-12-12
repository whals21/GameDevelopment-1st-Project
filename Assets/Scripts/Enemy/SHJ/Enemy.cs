using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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

    public float AttackDelay => Data.attackDelay;
    public float AttackRange => Data.attackRange;
    public GameObject ProjectilePrefab => Data.projectilePrefab;
    public Transform FirePoint => Data.firePoint != null ? Data.firePoint : transform;
    public bool IsRanged => Data.isRanged;

    public bool IsRange => Data.attackRange > 0f;

    public Transform point;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();

        // // enemyData가 할당되어 있는 경우에만 초기화
        // if (enemyData != null)
        // {
        //     currentHP = MaxHP;
        //     moveSpeed = Data.moveSpeed;
        // }

        // // 플레이어 자동 찾기 (null 체크)
        // GameObject player = GameObject.FindGameObjectWithTag("Player");
        // if (player != null)
        // {
        //     target = player.transform;
        // }
        // else
        // {
        //     Debug.LogError("Enemy: Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        // }

       
    }
    void Start()
    {

    }

    // 오브젝트 풀에서 가져올 때 호출 
    public void Init(EnemyObject data)
    {
        enemyData = data;
        currentHP = data.EnemyHP;
        moveSpeed = data.moveSpeed;
        //contactDamage = data.contactDamage;
        //expValue = data.expValue;

        // 플레이어 재확인 (풀에서 재사용 시)
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj == null)
        {
            target = null;
            return;
        }

        // FirePoint 기준으로 거리 계산 → 범위가 몬스터/FirePoint 따라감
        float distance = Vector2.Distance(FirePoint.position, playerObj.transform.position);

        if (distance <= AttackRange)
        {
            target = playerObj.transform;  // 범위 안: target = 플레이어
        }
        else
        {
            target = null;  // 범위 밖: target 해제
        }
    }
   

    // 데미지 받기
    public void TakeDamage(float damage)
    {
        currentHP -= (int)damage;

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    // 사망 처리
    private void Die()
    {
        // EnemySpawner에 알림
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.OnEnemyDied();
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
        Gizmos.DrawWireSphere(center.position, AttackRange);
    }
}
