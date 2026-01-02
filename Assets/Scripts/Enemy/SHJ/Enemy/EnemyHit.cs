using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHit : MonoBehaviour
{
    private EnemyCore core;
    private Color originalColor;

    [SerializeField] private float flashDuration = 0.06f;
    [SerializeField] private int flashCount = 5;

    private void Awake()
    {
        core = GetComponent<EnemyCore>();
        originalColor = core.sr.color;
    }

    public int OnHit(int incomingDamage)
    {
        if (core.CurrentHP <= 0) return 0;  // 이미 죽었으면 데미지 0

        int actualDamage = Mathf.Min(incomingDamage, core.CurrentHP);  // 초과 데미지 방지
       

        StopAllCoroutines();
        StartCoroutine(FlashRoutine());

        return actualDamage;  // 실제로 들어간 데미지만 리턴 (점수, 피해량 표시용)
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            core.sr.color = Color.white;
            yield return new WaitForSeconds(flashDuration);
            core.sr.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }
        core.sr.color = originalColor;
    }
}
