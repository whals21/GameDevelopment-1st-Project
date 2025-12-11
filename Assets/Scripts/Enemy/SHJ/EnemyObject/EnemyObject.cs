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

    [Header("=== ���Ÿ� ���� ===")]
    public bool isRanged = false;                    // �� ���� ���Ÿ�����?
    public GameObject projectilePrefab;              // �߻�ü ������
    public float attackRange = 8f;                   // ���� ��Ÿ�
    public float attackDelay = 2f;                   // ���� ��Ÿ��
    public Transform firePoint;                      // �߻� ��ġ (�����տ� �����ϰų� ��Ÿ�ӿ��� ã��)

    [Header("��������")]
    public WeaponObject[] weapons;
    

}
