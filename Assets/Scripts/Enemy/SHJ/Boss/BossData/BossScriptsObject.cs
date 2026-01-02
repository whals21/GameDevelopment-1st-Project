using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Enemy/Boss", fileName = "New Boss")]
public class BossScriptsObject : ScriptableObject
{
    [Header("등장시킬 오브젝트 필수")]
    public GameObject bossPrefab;
    [Header("체력 및 이동속도설정")]
    public int hp;      //체력
    public float move;  //이동속도
    [Header("등장할 범위 및 등장조건 설정")]
    public float detectionRange; //범위
    public float appearTime;//보스가 등장하는 시간
    public int killCountToSpawn;//플레이어가 몹들을 일정 이상으로 잡을 때 등장하는 조건 
    public int playerLV;     //플레이어의 LV에 따른 보스 등장
}
