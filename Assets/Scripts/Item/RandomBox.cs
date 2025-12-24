using UnityEngine;

public class RandomBox : MonoBehaviour
{
    [Header("����")]
    [SerializeField] private GameObject[] rewardItems; // ���� �����۵�
    [SerializeField] private float lifeTime = 10f; // 10�� �ڿ� �����

    // Ȱ��ȭ�� ������ ����
    private void OnEnable()
    {
        // ���� �ð� �ڿ� ������ ������� ����
        CancelInvoke("Despawn");
        Invoke("Despawn", lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            OpenBox();
        }
    }

    private void OpenBox()
    {
        // ������ ����� ��������� ���� ����
        if (rewardItems.Length > 0)
        {
            // �������� �ϳ� �̱�
            int index = Random.Range(0, rewardItems.Length);
            GameObject selectedItem = rewardItems[index];

            // ���� ��ġ�� ������ ��ȯ
            Instantiate(selectedItem, transform.position, Quaternion.identity);

            Debug.Log($"���� ���� {selectedItem.name} ���Դ�");
        }

        // ���ڴ� �����
        Despawn();
    }

    private void Despawn()
    {
        // 오브젝트 풀에 반환 - 12/24 조민희 추가
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