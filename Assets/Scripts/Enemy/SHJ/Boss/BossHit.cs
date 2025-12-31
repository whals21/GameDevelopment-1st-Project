using System.Collections;
using UnityEngine;

public class BossHit : MonoBehaviour
{
    private BossController bossController;
    private BossDie bossDie;
    private SpriteRenderer sr;
    private Color originalColor;

    [SerializeField] private float flashDuration = 0.06f;
    [SerializeField] private int flashCount = 5;

    private void Awake()
    {
        bossController = GetComponent<BossController>();
        bossDie = GetComponent<BossDie>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            originalColor = sr.color;
    }

    public float OnHit(float damage)
    {
        if (bossDie == null) return 0;

        // 실제로 들어가는 데미지
        float actualDamage = Mathf.Min(damage, bossDie.CurrentHp); // CurrentHp는 BossDie에 getter 추가 필요
        bossDie.TakeDamage(damage);

        // 피격 플래시
        if (sr != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }

        return actualDamage;
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            sr.color = Color.white;
            yield return new WaitForSeconds(flashDuration);
            sr.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }
        sr.color = originalColor;
    }
}
