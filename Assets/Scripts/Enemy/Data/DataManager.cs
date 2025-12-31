using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    [Header("경험치")]
    public ExpDropObject expDropObject;

    [Header("골드 관리")]
    public ScienceData myData; // 연구소 데이터 형식
    private string savePath;

    void Awake()
    {
        // 싱글톤
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 데이터 불러오기
        savePath = Application.persistentDataPath + "/ScienceData.json";
        LoadData();
    }

    // 골드 추가 함수
    public void AddGold(int amount)
    {
        myData.gold += amount;
        SaveData(); // 저장

        // UI표시
        if (PlayerHUD.Instance != null)
        {
            //PlayerHUD.Instance.UpdateGold(myData.gold);/12/31 오류가 떠서 잠시 주석으로 만듬/
        }
    }

    // 저장 & 불러오기 기능
    public void SaveData()
    {
        File.WriteAllText(savePath, JsonUtility.ToJson(myData, true));
    }

    public void LoadData()
    {
        if (File.Exists(savePath))
        {
            myData = JsonUtility.FromJson<ScienceData>(File.ReadAllText(savePath));
        }
        else
        {
            myData = new ScienceData(); // 파일 없으면 새로 만들기
        }
    }
}

