using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{

    float CurrentHP { get; }
    public void TakeDamage(float damage, bool isCritical = false);
}
