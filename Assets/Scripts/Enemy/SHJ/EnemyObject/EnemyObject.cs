using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Enemy/Enemy Data", fileName = "New Enemy")]
public class EnemyObject : ScriptableObject
{

    
    
    [Header("외형")]
    public GameObject prefab;
    [Header("이름")]
    public string EnemyName;     //이름
    [Header("체력")]
    public int EnemyHP;          //현재 체력

    [Header("이동속도")]
    public float moveSpeed;

    [Header("=== 원거리 전용 ===")]
    public bool isRanged = false;                    // 이 적이 원거리인지?
    public GameObject projectilePrefab;              // 발사체 프리팹
    public float attackRange = 8f;                   // 공격 사거리
    public float attackDelay = 2f;                   // 공격 쿨타임
    public Transform firePoint;                      // 발사 위치 (프리팹에 지정하거나 런타임에서 찾음)

    [Header("공격패턴")]
    public WeaponObject[] weapons;
    

}
