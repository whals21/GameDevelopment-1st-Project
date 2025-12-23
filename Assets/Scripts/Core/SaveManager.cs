using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    public GameData nowPlayer = new GameData(); // 저장 공간
    public bool isContinue = false; // "이어하기" 판독

    string path; // 저장될 경로

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            path = Application.persistentDataPath + "/SaveData.json"; // 경로 설정
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 저장하기
    public void SaveGame()
    {
        // 현재 게임 상태를 저장
        if (GameManager.Instance != null)
        {
            nowPlayer.level = GameManager.Instance.level;
            nowPlayer.currentExp = GameManager.Instance.exp;
            nowPlayer.maxExp = GameManager.Instance.maxExp;
            nowPlayer.killCount = GameManager.Instance.killCount;
        }

        // 파일로 저장
        string json = JsonUtility.ToJson(nowPlayer, true);
        File.WriteAllText(path, json);

        Debug.Log("저장 완료: " + path);
    }

    // 불러오기
    public bool LoadGame()
    {
        if (!File.Exists(path)) return false; // 파일 없으면 실패

        string json = File.ReadAllText(path);
        nowPlayer = JsonUtility.FromJson<GameData>(json);

        isContinue = true; // "이어하기"라고 표시
        return true;
    }

    // 파일이 있는지 확인 (버튼 활성화용)
    public bool HasSaveData()
    {
        return File.Exists(path);
    }

    // 데이터 초기화 (새로 하기용)
    public void DataClear()
    {
        nowPlayer = new GameData();
        isContinue = false;
    }
}