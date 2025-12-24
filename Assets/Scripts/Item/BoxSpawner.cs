using UnityEngine;
using System.Collections;

public class BoxSpawner : MonoBehaviour
{
    [Header("상자 소환 설정")]
    [SerializeField] private float spawnInterval = 30f;
    [SerializeField] private float spawnRadius = 5f; // 반경

    private Transform playerTransform;

    private void Start()
    {
        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // 타이머 시작
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true) // 무한 반복
        {
            // 시간 기다리기
            yield return new WaitForSeconds(spawnInterval);

            // 플레이어가 있고, 풀 매니저가 준비되었을 때만
            if (playerTransform != null && ObjectPoolManager.Instance != null)
            {
                // 풀에서 상자 꺼냄
                RandomBox box = ObjectPoolManager.Instance.GetRandomBox();

                if (box != null)
                {
                    // 플레이어 주변 랜덤 위치 계산
                    Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
                    Vector3 spawnPos = playerTransform.position + (Vector3)randomPos;

                    // 위치 지정
                    box.transform.position = spawnPos;
                }
            }
        }
    }
}