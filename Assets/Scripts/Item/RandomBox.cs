using UnityEngine;

public class RandomBox : MonoBehaviour
{
    [Header("���� ����")]
    [SerializeField] private GameObject[] rewardItems; // ���� �����۵�
    [SerializeField] private float lifeTime = 20f;     // �� �μ��� ������� �ð�

    [Header("ü�� ����")]
    [SerializeField] private float maxHp = 50f;
    private bool isBroken = false; // �ߺ� �ı� ������

    [SerializeField] private float currentHp;

    [Header("�ǰ� ȿ�� (����)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        // ���� �ʱ�ȭ
        currentHp = maxHp;
        isBroken = false;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        // �ð� ������ �����
        CancelInvoke("Despawn");
        Invoke("Despawn", lifeTime);
    }

    public void TakeDamage(float damage)
    {
        if (isBroken) return;

        currentHp -= damage;

        // �ǰ� ȿ��
        StartCoroutine(HitFlashRoutine());

        // ü���� 0�� �Ǹ� ���� ����
        if (currentHp <= 0)
        {
            OpenBox();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        int layer = collision.gameObject.layer;

        // 9: Projectile, 14: Projectile2, 0: Default
        if (layer == 9 || layer == 0)
        {
            TakeDamage(10f); // 1ȸ�ǰ� 10������
            collision.gameObject.SetActive(false);
        }
    }

    private System.Collections.IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red; // ������
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

            // ������ ��ȯ
            Instantiate(selectedItem, transform.position, Quaternion.identity);
        }

        Despawn();
    }

    private void Despawn()
    {
        // �Ŵ����� ������ �ݳ�, ������ ��Ȱ��ȭ
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