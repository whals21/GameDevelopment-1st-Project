using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCore : MonoBehaviour
{
    [SerializeField] private EnemyObject enemyData;
    public EnemyObject Data => enemyData;
    public int MaxHP => Data.EnemyHP;

    [SerializeField] private int currentHP;
    public int CurrentHP
    {
        get => currentHP;
        private set
        {
            int prevHP = currentHP;
            currentHP = Mathf.Clamp(value, 0, MaxHP);
        }
    }

    [SerializeField] private float moveSpeed;
    public float Speed => moveSpeed;

    [SerializeField] private Transform target;
    public Transform Target => target;

    public Rigidbody2D rb { get; private set; }
    public SpriteRenderer sr { get; private set; }
    public Animator anim { get; private set; }
    public Collider2D col { get; private set; }

    public float AttackDelay => Data.attackDelay;
    public float AttackRange => Data.attackRange;
    public GameObject ProjectilePrefab => Data.projectilePrefab;
    public Transform FirePoint;
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
        if (AttackRange <= 0f) return;

        Transform center = FirePoint != null ? FirePoint : transform;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center.position, AttackRange);
    }

    void Start() { }
    void Update() { }
}