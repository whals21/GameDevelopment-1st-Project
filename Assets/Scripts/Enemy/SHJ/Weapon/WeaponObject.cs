using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Weapon/EnemyWeapon Data", fileName = "New EnemyWeapon")]
public class WeaponObject : ScriptableObject
{

    
    public ActtackManager.AttackRange attackRange;
    


    [Header("기타 무기 정보")]
    public AttackName
    public float damage;        //데미지
    public float attackDelay;   //공격딜레이
    public float HitLength;     //공격히트
}
