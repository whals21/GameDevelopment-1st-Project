using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Enemy/Pattern", fileName = "New Pattern")]
public class BossPattern : ScriptableObject
{
    public GameObject[] bulletPrefab;
    public int[] bulletCount;     //총알 개수
    public float[] attackRayLength;   //레이충돌길이
    public float[] damage;      //패턴의 데미지
    public float[] attackRange; //패턴에 따른 범위
    public float delayAfter;    //이 공격 후 기다릴 시간
    public BossAttack[] bossAttacks;   //공격패턴
}
