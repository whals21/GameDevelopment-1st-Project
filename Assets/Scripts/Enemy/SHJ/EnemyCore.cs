using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCore : MonoBehaviour
{
    [Header("ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ SO")]
    [SerializeField] private EnemyObject enemyData;
    public EnemyObject Data => enemyData;


    public int MaxHP => Data.EnemyHP;
    [Header("Ã¼ï¿½ï¿½")]
    [SerializeField] private int currentHP;
    public int CurrentHP 
    { 
        get => currentHP;
        private set
        {
            // ÀÌÀü °ª ÀúÀå (UI °»½Å¿ë)
            int prevHP = currentHP;
            currentHP = Mathf.Clamp(value, 0, MaxHP);

            
        }
    }

    [Header("ï¿½Ìµï¿½")]
    [SerializeField] private float moveSpeed;
    public float Speed => moveSpeed;

    [SerializeField] private Transform target;
    public Transform Target => target;

    
    public Rigidbody2D rb { get; private set; }
    public SpriteRenderer sr { get; private set; }
    public Animator anim { get; private set; }
    public Collider2D col { get; private set; }
    private Ray2D ray;
    private RaycastHit2D hit;


        
    public float AttackDelay => Data.attackDelay;
    public float AttackRange => Data.attackRange;
    public GameObject ProjectilePrefab => Data.projectilePrefab;
    public Transform FirePoint => Data.firePoint != null ? Data.firePoint : transform;
    public bool IsRanged => Data.isRanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        currentHP = MaxHP;
        moveSpeed = Data.moveSpeed;

    }

    private void OnDrawGizmosSelected()
    {
        if (Target == null || AttackRange <= 0f) return;

        Vector3 origin = FirePoint.position;
        Vector3 dir = (Target.position - origin).normalized;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, dir * AttackRange);
        
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
