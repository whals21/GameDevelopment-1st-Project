using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Enemy/Boss", fileName = "New Boss")]
public class BossScriptsObject : ScriptableObject
{
    public GameObject bossPrefab;
    

   
    public int hp;      //체력
    public float move;  //이동속도
    public float detectionRange; //범위
    public float appearTime;//보스가 등장하는 시간
    public int killCountToSpawn;//플레이어가 몹들을 일정 이상으로 잡을 때 등장하는 조건 
    public int playerLV;     //플레이어의 LV에 따른 보스 등장
}
