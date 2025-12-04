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

    [Header("공격패턴")]
    public WeaponObject[] weapons;

    

}
