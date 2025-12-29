using UnityEngine;

public class BossManager : MonoBehaviour
{
    public BossScriptsObject[] bossDatas;
    public BossScriptsObject[] BossDatas => bossDatas;
    private int currentBossIndex = -1;

    // 현재 보스 (currentBossIndex에 해당하는) 데이터 get 프로퍼티
    public int CurrentHp => (currentBossIndex >= 0 && currentBossIndex < bossDatas.Length) ? bossDatas[currentBossIndex].hp : 0;
    public float CurrentMoveSpeed => (currentBossIndex >= 0 && currentBossIndex < bossDatas.Length) ? bossDatas[currentBossIndex].move : 0f;

    public float CurrentDetectionRange => (currentBossIndex >= 0 && currentBossIndex < bossDatas.Length) ? bossDatas[currentBossIndex].detectionRange : 0f;
    public float CurrentAppearTime => (currentBossIndex >= 0 && currentBossIndex < bossDatas.Length) ? bossDatas[currentBossIndex].appearTime : 0f;
    public int CurrentKillCountToSpawn => (currentBossIndex >= 0 && currentBossIndex < bossDatas.Length) ? bossDatas[currentBossIndex].killCountToSpawn : 0;
    public int CurrentRequiredPlayerLevel => (currentBossIndex >= 0 && currentBossIndex < bossDatas.Length) ? bossDatas[currentBossIndex].playerLV : 0;

    // 현재 보스 데이터 전체
    public BossScriptsObject CurrentBossData => (currentBossIndex >= 0 && currentBossIndex < bossDatas.Length) ? bossDatas[currentBossIndex] : null;

    // 현재 보스 인덱스 (외부에서 설정/읽기 가능)
    public int CurrentBossIndex
    {
        get => currentBossIndex;
        set
        {
            if (value >= 0 && value < bossDatas.Length)
                currentBossIndex = value;
        }
    }

    // 싱글톤 변수만
    public static BossManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void CheckBossAppear()
    {
        //if (currentBossIndex != -1) return; // 이미 보스 등장

        //float gameTime = GameManager.Instance.GameTime;
        //int playerLevel = GameManager.Instance.levelUpManager.CurrentPlayerLevel;

        //for (int i = 0; i < bossDatas.Length; i++)
        //{
        //    var data = bossDatas[i];

        //    // 킬 수 조건은 무시하고, 시간과 플레이어 레벨만 체크
        //    if (gameTime >= data.appearTime &&
        //        playerLevel >= data.playerLV)
        //    {
        //        currentBossIndex = i; // 기준점으로 현재 보스 결정
        //        Debug.Log($"Boss ready: {data.name}");
        //        break;
        //    }
        //}
    }
}