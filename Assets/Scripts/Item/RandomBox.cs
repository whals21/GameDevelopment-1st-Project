using UnityEngine;

public class RandomBox : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private GameObject[] rewardItems; // 나올 아이템들
    [SerializeField] private float lifeTime = 10f; // 10초 뒤에 사라짐

    // 활성화될 때마다 실행
    private void OnEnable()
    {
        // 일정 시간 뒤에 스스로 사라지게 예약
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
        // 아이템 목록이 비어있으면 에러 방지
        if (rewardItems.Length > 0)
        {
            // 랜덤으로 하나 뽑기
            int index = Random.Range(0, rewardItems.Length);
            GameObject selectedItem = rewardItems[index];

            // 상자 위치에 아이템 소환
            Instantiate(selectedItem, transform.position, Quaternion.identity);

            Debug.Log($"상자 오픈 {selectedItem.name} 나왔다");
        }

        // 상자는 사라짐
        Despawn();
    }

    private void Despawn()
    {
        gameObject.SetActive(false);

    }
}