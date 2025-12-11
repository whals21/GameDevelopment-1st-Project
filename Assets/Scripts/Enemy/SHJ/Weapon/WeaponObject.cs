using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Weapon/EnemyWeapon Data", fileName = "New EnemyWeapon")]
public class WeaponObject : ScriptableObject
{


    



    [Header("기타 무기 정보")]

    public float damage;        //데미지
    public float attackDelay;   //공격딜레이
    public float HitLength;     //공격히트

    public ActtackManager.AttackRange attackRange;

    [Header("이 무기가 사용할 기본 공격")]
    public EnumList.AttackType defaultAttack;   // 이거 하나만 지정!
}
