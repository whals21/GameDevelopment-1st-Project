using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Weapon/EnemyWeapon Data", fileName = "New EnemyWeapon")]
public class WeaponObject : ScriptableObject
{
    [Header("외형")]
    public GameObject prefab;
    [Header("이름")]
    public string EnemyName;     //이름
    [Header("체력")]
    public int EnemyHP;          //현재 체력

    [Header("이동속도")]
    public float moveSpeed;
}
