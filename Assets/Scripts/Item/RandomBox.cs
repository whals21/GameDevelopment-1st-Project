using UnityEngine;

public class RandomBox : MonoBehaviour
{
    [Header("보상 설정")]
    [SerializeField] private GameObject[] rewardItems; // 나올 아이템들
    [SerializeField] private float lifeTime = 20f;     // 안 부수면 사라지는 시간

    [Header("체력 설정")]
    [SerializeField] private float maxHp = 50f;
    private bool isBroken = false; // 중복 파괴 방지용

    [SerializeField] private float currentHp;

    [Header("피격 효과 (선택)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        // 상태 초기화
        currentHp = maxHp;
        isBroken = false;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        // 시간 지나면 사라짐
        CancelInvoke("Despawn");
        Invoke("Despawn", lifeTime);
    }

    public void TakeDamage(float damage)
    {
        if (isBroken) return;

        currentHp -= damage;

        // 피격 효과
        StartCoroutine(HitFlashRoutine());

        // 체력이 0이 되면 상자 오픈
        if (currentHp <= 0)
        {
            OpenBox();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        int layer = collision.gameObject.layer;

        // 9: Projectile, 14: Projectile2, 0: Default
        if (layer == 9 || layer == 14 || layer == 0)
        {
            TakeDamage(10f); // 1회피격 10데미지
            collision.gameObject.SetActive(false);
        }
    }

    private System.Collections.IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red; // 빨간색
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
    }

    private void OpenBox()
    {
        isBroken = true;

        if (rewardItems.Length > 0)
        {
            int index = Random.Range(0, rewardItems.Length);
            GameObject selectedItem = rewardItems[index];

            // 아이템 소환
            Instantiate(selectedItem, transform.position, Quaternion.identity);
        }

        Despawn();
    }

    private void Despawn()
    {
        // 매니저가 있으면 반납, 없으면 비활성화
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnRandomBox(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}