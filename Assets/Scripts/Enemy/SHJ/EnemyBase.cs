using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("데이터 SO")]
    [SerializeField] private EnemyObject enemyData;
    public EnemyObject Data => enemyData;


    public int MaxHP => Data.EnemyHP;
    [Header("체력")]
    [SerializeField] private int currentHP;
    public int CurrentHP => currentHP;

    [Header("이동")]
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
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        currentHP = MaxHP;
        moveSpeed = Data.moveSpeed;
        
        
        
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
