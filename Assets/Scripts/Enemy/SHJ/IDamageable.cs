using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{

    Transform Transform { get; }
    public void TakeDamage(float damage, bool isCritical = false);
}
