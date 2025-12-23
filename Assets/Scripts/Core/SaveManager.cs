using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    public GameData nowPlayer = new GameData(); // ���� ����
    public bool isContinue = false; // "�̾��ϱ�" �ǵ�

    string path; // ����� ���

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            path = Application.persistentDataPath + "/SaveData.json"; // ��� ����
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // �����ϱ�
    public void SaveGame()
    {
        // 12/23 코드 비활성화_조민희
        // // ���� ���� ���¸� ����
        // if (GameManager.Instance != null)
        // {
        //     nowPlayer.level = GameManager.Instance.level;
        //     nowPlayer.currentExp = GameManager.Instance.exp;
        //     nowPlayer.maxExp = GameManager.Instance.maxExp;
        //     nowPlayer.killCount = GameManager.Instance.killCount;
        // }

        // // ���Ϸ� ����
        // string json = JsonUtility.ToJson(nowPlayer, true);
        // File.WriteAllText(path, json);

        // Debug.Log("���� �Ϸ�: " + path);
    }

    // �ҷ�����
    public bool LoadGame()
    {
        if (!File.Exists(path)) return false; // ���� ������ ����

        string json = File.ReadAllText(path);
        nowPlayer = JsonUtility.FromJson<GameData>(json);

        isContinue = true; // "�̾��ϱ�"��� ǥ��
        return true;
    }

    // ������ �ִ��� Ȯ�� (��ư Ȱ��ȭ��)
    public bool HasSaveData()
    {
        return File.Exists(path);
    }

    // ������ �ʱ�ȭ (���� �ϱ��)
    public void DataClear()
    {
        nowPlayer = new GameData();
        isContinue = false;
    }
}