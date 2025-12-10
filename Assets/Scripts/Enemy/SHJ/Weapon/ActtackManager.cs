using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActtackManager : MonoBehaviour
{
    // enum을 여기서 선언
    // 만들려고 하는 결과는 Ex) Close설정, 등록된 
    public enum AttackRange
    {
        Close,
        Ranged
    }

    [Header("무기 데이터 할당")]
    [SerializeField] private WeaponObject weaponData;

    // 공격 스크립트들
    private CloseAttack closeAttack;
    private RangedAttack rangedAttack;

    private void Awake()
    {
        closeAttack = GetComponent<CloseAttack>();
        rangedAttack = GetComponent<RangedAttack>();

        UpdateAttackScripts();
    }

    private void OnValidate()
    {
        UpdateAttackScripts();
    }

  

    private void UpdateAttackScripts()
    {
        if (weaponData == null || closeAttack == null || rangedAttack == null) return;

        bool isClose = weaponData.attackRange == AttackRange.Close;
        bool isRanged = weaponData.attackRange == AttackRange.Ranged;

        closeAttack.enabled = isClose;
        rangedAttack.enabled = isRanged;

        Debug.Log($"{gameObject.name} 공격 타입: {weaponData.attackRange}");
    }

    public void SetWeapon(WeaponObject newWeapon)
    {
        weaponData = newWeapon;
        UpdateAttackScripts();
    }
}
