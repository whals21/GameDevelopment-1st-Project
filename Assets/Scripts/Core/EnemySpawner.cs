using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float spawnInterval = 2f; // 스폰 간격
    public int maxEnemies = 100; // 최대 적 수
    public float spawnDistance = 15f; // 플레이어로부터 스폰 거리

    [Header("Enemy Data")]
    [Tooltip("스폰할 적 종류들 (ScriptableObject)")]
    public EnemyObject[] enemyTypes; // 여러 종류의 적 데이터

    private Transform player;
    private float spawnTimer;
    private int currentEnemyCount;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 유효성 검사
        if (enemyTypes == null || enemyTypes.Length == 0)
        {
            Debug.LogError("EnemySpawner: enemyTypes 배열이 비어있습니다! Inspector에서 EnemyObject를 할당하세요.");
        }
    }

    private void Update()
    {
        if (player == null) return;
        if (currentEnemyCount >= maxEnemies) return;
        if (enemyTypes == null || enemyTypes.Length == 0) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnEnemy();
            spawnTimer = 0f;
        }
    }

    private void SpawnEnemy()   //12/19
    {
        int prefabIndex = Random.Range(0, ObjectPoolManager.Instance.enemyPrefabs.Length);

        
        EnemyObject selectedSO = null;
        if (prefabIndex < ObjectPoolManager.Instance.enemyTypes.Length)
            selectedSO = ObjectPoolManager.Instance.enemyTypes[prefabIndex];
        // 위치 설정
        Enemy enemy = ObjectPoolManager.Instance.GetEnemy(prefabIndex);
        if (enemy == null) return;

        // 위치 설정
        Vector2 spawnPos = GetRandomSpawnPosition();
        enemy.transform.position = spawnPos;
        enemy.gameObject.SetActive(true);

        // SO로 초기화 → 여기서 속성값 세팅됨
        if (selectedSO != null)
            enemy.Init(selectedSO);

        currentEnemyCount++;
    }

    private Vector2 GetRandomSpawnPosition()
    {
        // 플레이어 주변 원형 랜덤 위치
        float angle = Random.Range(0f, 360f);
        float rad = angle * Mathf.Deg2Rad;

        Vector2 offset = new Vector2(
            Mathf.Cos(rad) * spawnDistance,
            Mathf.Sin(rad) * spawnDistance
        );

        return (Vector2)player.position + offset;
    }

    // 적 사망 시 카운트 감소 (Enemy.Die()에서 호출 필요)
    public void OnEnemyDied()
    {
        currentEnemyCount--;
    }
}
