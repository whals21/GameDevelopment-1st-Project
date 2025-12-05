using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Enemy/Enemy Data", fileName = "New Enemy")]
public class EnemyObject : ScriptableObject
{
    [Header("����")]
    public GameObject prefab;
    [Header("�̸�")]
    public string EnemyName;     //�̸�
    [Header("ü��")]
    public int EnemyHP;          //���� ü��

    [Header("�̵��ӵ�")]
    public float moveSpeed;

    [Header("������")]
    public float contactDamage;  // ���� ������

    [Header("���迭ġ")]
    public int expValue;  // ġ�� �� ���迭ġ

    [Header("��������")]
    public WeaponObject[] weapons;
}
