using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class BossController : MonoBehaviour
{
    private BossState currentState;
    [SerializeField] int MonsterNumber;
    
    [SerializeField] private BossPattern bossPattern;
    private BossScriptsObject myData;
    public BossScriptsObject Data => myData;
    private SpriteRenderer sr;
    public Transform target;
    public Rigidbody2D rb;
    public Rigidbody2D RB => rb;
    public int CurrentPatternIndex { get; private set; }            //나 없으면 보스 패턴 몇번째 놈 쓰는지 니들 모름

    public GameObject CurrentBulletPrefab => bossPattern.bulletPrefab[CurrentPatternIndex];

    public float CurrentAttackRayLength => bossPattern.attackRayLength[CurrentPatternIndex];
    public int CurrentBulletCount => bossPattern.bulletCount[CurrentPatternIndex];
    public float CurrentAttackRange => bossPattern.attackRange[CurrentPatternIndex]; //패턴에 따른 범위
    public float CurrentDamage =>  bossPattern.damage[CurrentPatternIndex];    //이 공격 후 기다릴 시간
    BossAttack bossAttacks => bossPattern.bossAttacks[CurrentPatternIndex];   //공격패턴



    // Start is called before the first frame update
    private void Start()
    {
        myData = BossManager.Instance.BossDatas[MonsterNumber];
       
        rb = GetComponent<Rigidbody2D>();

        SetState(new BossMove(this));
        
    }
    void Update()
    {
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
        currentState.Enter();
    }

    // Update is called once per frame
    public void SetPattern(int index)
    {
        CurrentPatternIndex = index;
    }
}
