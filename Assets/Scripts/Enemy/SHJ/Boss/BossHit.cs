using System.Collections;
using UnityEngine;

public class BossHit : MonoBehaviour
{
    private BossDie bossDie;
    private SpriteRenderer sr;
    private Color originalColor;

    [SerializeField] private float flashDuration = 0.06f;
    [SerializeField] private int flashCount = 5;

    private void Awake()
    {
        // 부모에서 BossDie 찾기
        bossDie = GetComponentInParent<BossDie>();

        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            originalColor = sr.color;

        if (bossDie == null)
            Debug.LogError("BossHit: BossDie 없음");
    }

    // 플레이어 공격이 직접 호출하는 함수
    public void TakeDamage(float damage, bool isCritical = false)
    {
        Debug.Log($"[BossHit] TakeDamage 호출됨: {damage}");

        if (bossDie == null)
        {
            Debug.LogError("[BossHit] bossDie == null");
            return;
        }

      

        if (sr != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }
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
