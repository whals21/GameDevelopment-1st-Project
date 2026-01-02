using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{

    float CurrentHP { get; }
    // source 파라미터 포함_조민희 01/01 추가
    void TakeDamage(float damage, SkillBase source = null, bool isCritical = false);

}
