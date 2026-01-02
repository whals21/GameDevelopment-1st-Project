using System.Collections.Generic;
using UnityEngine;

//보스메니저, 보스 등장타이밍, 보스의 능력치 역할
//메인 시스템이 보스들한테 능력치 및 등장할 타이밍(시간,킬수,LV) 관리
public class BossManager : MonoBehaviour
{
    [Header("보스 데이터 배열")]
    public BossScriptsObject[] bossDatas;
    public BossScriptsObject[] BossDatas => bossDatas;

    private int currentBossIndex = -1;
    private bool bossSpawned = false;
    private float elapsedTime = 0f;

    // 캐싱된 플레이어 정보
    private int cachedPlayerLevel = 0;
    private int cachedKillCount = 0;
    private List<BossController> activeBosses = new List<BossController>();

    // 현재 보스 데이터
    public BossScriptsObject CurrentBossData => (currentBossIndex >= 0 && currentBossIndex < bossDatas.Length) ? bossDatas[currentBossIndex] : null;
    public GameObject CurrentBossPrefab => CurrentBossData != null ? CurrentBossData.bossPrefab : null;
    public int CurrentHp => CurrentBossData != null ? CurrentBossData.hp : 0;
    public float CurrentMoveSpeed => CurrentBossData != null ? CurrentBossData.move : 0f;
    public float CurrentDetectionRange => CurrentBossData != null ? CurrentBossData.detectionRange : 0f;
    public float CurrentAppearTime => CurrentBossData != null ? CurrentBossData.appearTime : 0f;
    public int CurrentKillCountToSpawn => CurrentBossData != null ? CurrentBossData.killCountToSpawn : 0;
    public int CurrentRequiredPlayerLevel => CurrentBossData != null ? CurrentBossData.playerLV : 0;

    public static BossManager Instance { get; private set; }
    private bool[] bossSpawnedFlags;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        bossSpawnedFlags = new bool[bossDatas.Length];
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        // PlayerHUD에서 한 번만 int 캐싱
        if (PlayerHUD.Instance != null)
        {
            cachedPlayerLevel = ParseLevel(PlayerHUD.Instance.levelText.text);
            cachedKillCount = ParseKills(PlayerHUD.Instance.killCountText.text);
        }

        CheckBossAppear();
    }

    private int ParseLevel(string text)
    {
        if (text.StartsWith("Lv."))
        {
            if (int.TryParse(text.Substring(3), out int lv))
                return lv;
        }
        return 0;
    }

    private int ParseKills(string text)
    {
        string[] parts = text.Split(':');
        if (parts.Length == 2)
        {
            if (int.TryParse(parts[1].Trim(), out int kills))
                return kills;
        }
        return 0;
    }

    private void CheckBossAppear()
    {
        if (bossDatas == null || bossDatas.Length == 0) return;

        for (int i = 0; i < bossDatas.Length; i++)
        {
            var bossData = bossDatas[i];
            if (bossData == null) continue;

            // 이미 스폰된 보스면 건너뛰기
            if (bossSpawnedFlags[i]) continue;

            

            if (cachedPlayerLevel >= bossData.playerLV &&
                cachedKillCount >= bossData.killCountToSpawn &&
                elapsedTime >= bossData.appearTime)
            {
                bossSpawnedFlags[i] = true; // 이 보스는 이제 스폰됨
                currentBossIndex = i;
                
                SpawnBoss(bossData);
            }
        }
    }

    private void SpawnBoss(BossScriptsObject bossData)
    {
        if (bossData.bossPrefab == null) return;

        Vector3 managerPos = transform.position;
        float range = bossData.detectionRange;
        Vector3 spawnPos = managerPos + new Vector3(Random.Range(-range, range), 0f, Random.Range(-range, range));

        GameObject bossObj = Instantiate(bossData.bossPrefab, spawnPos, Quaternion.identity);

        // BossController 가져오기
        BossController bossCtrl = bossObj.GetComponent<BossController>();
        if (bossCtrl != null)
        {
            activeBosses.Add(bossCtrl);
           
        }
    }
}
