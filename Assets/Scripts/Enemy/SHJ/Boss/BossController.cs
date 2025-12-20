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
    private Transform target;
    public Rigidbody2D rb;
    public Rigidbody2D RB => rb;
    public float[] damage => bossPattern.damage;      //패턴의 데미지
    public float[] attackRange => bossPattern.attackRange; //패턴에 따른 범위
    public float delayAfter => bossPattern.delayAfter;    //이 공격 후 기다릴 시간
    BossAttack[] bossAttacks => bossPattern.bossAttacks;   //공격패턴



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
  
}
