using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EnumList
{
    public enum AttackType
    {
        None,
        Slash,          // 근접 기본
        Stab,           // 찌르기
        SpinAttack,     // 회전베기

        Shoot,          // 원거리 기본
        ArrowRain,      // 화살 비
        Fireball,       // 파이어볼

        Charge,         // 돌진 공격
        Grenade,        // 수류탄
        Laser           // 레이저
        // 나중에 100개가 돼도 여기만 추가하면 끝!
    }

}
