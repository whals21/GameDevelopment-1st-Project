using UnityEngine;

/// <summary>
/// 플레이어 주변에 적을 주기적으로 스폰하고 최대 적 수를 관리하는 스포너
/// 장애물을 피하는 안전한 스폰 위치를 자동 탐색
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    #region Serialized Fields
    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxEnemies = 100;
    [SerializeField] private float spawnDistance = 15f;

    [Header("Layer Settings")]
    [Tooltip("장애물로 간주할 레이어 (벽, 바위 등)")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Enemy Data")]
    [Tooltip("스폰할 적 종류들 (ScriptableObject)")]
    [SerializeField] private EnemyObject[] enemyTypes;
    #endregion

    #region Private Fields
    private Transform _playerTransform;
    private float _spawnTimer;
    private int _currentEnemyCount;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        // [수정 1] FindWithTag 제거, 싱글톤 패턴 사용
        if (PlayerController.Instance != null)
        {
            _playerTransform = PlayerController.Instance.transform;
        }
        else
        {
            Debug.LogError("[EnemySpawner] PlayerController를 찾을 수 없습니다.");
        }

        // 데이터 유효성 검사
        if (enemyTypes == null || enemyTypes.Length == 0)
        {
            Debug.LogError("[EnemySpawner] 적 데이터가 설정되지 않았습니다.");
        }
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        _spawnTimer += Time.deltaTime;

        // [수정 2] 최대 적 수를 넘어가면 스폰 중지
        if (_currentEnemyCount >= maxEnemies) return;

        if (enemyTypes == null || enemyTypes.Length == 0) return;

        if (_spawnTimer >= spawnInterval)
        {
            SpawnEnemy();
            _spawnTimer = 0f;
        }
    }
    #endregion

    #region Private Methods
    private void SpawnEnemy()
    {
        // [수정 4] ObjectPoolManager Null 체크
        if (ObjectPoolManager.Instance == null)
        {
            Debug.LogError("[EnemySpawner] ObjectPoolManager를 찾을 수 없습니다.");
            return;
        }

        // 적 데이터 선택
        int randomIndex = Random.Range(0, enemyTypes.Length);
        EnemyObject selectedEnemyData = enemyTypes[randomIndex];

        if (selectedEnemyData == null)
        {
            Debug.LogWarning($"[EnemySpawner] 인덱스 {randomIndex}의 적 데이터가 null입니다.");
            return;
        }

        // [수정 3] 안전한 스폰 위치 탐색 (장애물 회피)
        Vector2 spawnPos = GetSafeSpawnPosition();

        // 풀에서 적 가져오기
        Enemy enemy = ObjectPoolManager.Instance.GetEnemy(randomIndex);

        // 풀이 비어있는지 체크
        if (enemy == null)
        {
            Debug.LogWarning("[EnemySpawner] 풀이 비어있어 적 생성 실패");
            return;
        }

        // 적 초기화 및 배치
        enemy.transform.position = spawnPos;
        enemy.Init(selectedEnemyData);

        // 스폰 성공 시에만 카운트 증가
        _currentEnemyCount++;
    }

    /// <summary>
    /// 장애물을 피하는 안전한 스폰 위치를 찾습니다.
    /// 최대 10번 시도하여 장애물이 없는 위치를 반환합니다.
    /// </summary>
    private Vector2 GetSafeSpawnPosition()
    {
        const int MAX_ATTEMPTS = 10;

        for (int i = 0; i < MAX_ATTEMPTS; i++)
        {
            float angle = Random.Range(0f, 360f);
            float rad = angle * Mathf.Deg2Rad;

            Vector2 offset = new Vector2(
                Mathf.Cos(rad) * spawnDistance,
                Mathf.Sin(rad) * spawnDistance
            );

            Vector2 checkPos = (Vector2)_playerTransform.position + offset;

            // 장애물 체크 (obstacleLayer에 있는 충돌체 검사)
            Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, 0.5f, obstacleLayer);

            if (hits.Length == 0)
            {
                return checkPos; // 장애물 없는 위치 반환
            }
        }

        // 모든 방향이 막혀있으면 플레이어 위치 반환 (폴백)
        Debug.LogWarning("[EnemySpawner] 안전한 스폰 위치를 찾지 못해 플레이어 위치로 반환");
        return _playerTransform.position;
    }
    #endregion

    #region Public API
    /// <summary>
    /// 적 사망 시 호출됨 (Enemy.OnDisable 등에서 호출)
    /// </summary>
    public void OnEnemyDied()
    {
        // 카운트 감소 (음수 방지)
        if (_currentEnemyCount > 0)
        {
            _currentEnemyCount--;
        }
    }

    /// <summary>
    /// 현재 활성화된 적 수 반환 (디버그용)
    /// </summary>
    public int GetCurrentEnemyCount()
    {
        return _currentEnemyCount;
    }
    #endregion
}
