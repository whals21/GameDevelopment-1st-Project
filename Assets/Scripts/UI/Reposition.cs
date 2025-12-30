using UnityEngine;

public class Reposition : MonoBehaviour
{
    private Collider2D collider;

    void Awake()
    {
        collider = GetComponent<Collider2D>();
    }

    void Update()
    {
        // 플레이어 위치 가져오기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector3 playerPos = player.transform.position;
        Vector3 myPos = transform.position;

        // x축, y축 거리 차이 계산
        float diffX = Mathf.Abs(playerPos.x - myPos.x);
        float diffY = Mathf.Abs(playerPos.y - myPos.y);

        // 맵의 실제 크기 가져오기
        float width = collider.bounds.size.x;
        float height = collider.bounds.size.y;

        // 가로 이동
        if (diffX > width)
        {
            float dirX = playerPos.x > myPos.x ? 1 : -1;
            transform.position = transform.position + Vector3.right * dirX * width * 2;
        }

        // 세로 이동
        if (diffY > height)
        {
            float dirY = playerPos.y > myPos.y ? 1 : -1;
            transform.position = transform.position + Vector3.up * dirY * height * 2;
        }
    }
}