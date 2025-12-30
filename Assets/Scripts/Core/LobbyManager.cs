using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("설정")]
    public string gameSceneName = "MainScene"; // 이동할 씬 이름
    public GameObject optionPanel;

    [Header("연구소")]
    public GameObject sciencePanel;

    [Header("버튼")]
    public Button continueButton;

    private void Start()
    {
        // 저장된 데이터가 없으면 이어하기 버튼 비활성화 (클릭 불가)
        if (SaveManager.Instance != null && !SaveManager.Instance.HasSaveData())
        {
            continueButton.interactable = false; // 클릭 불가능하게 만듦
        }
        else
        {
            continueButton.interactable = true;
        }
    }

    // 새로 하기
    public void OnClickNewGame()
    {
        if (SaveManager.Instance != null) SaveManager.Instance.DataClear();
        SceneManager.LoadScene(gameSceneName);
    }

    // 이어 하기
    public void OnClickContinue()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.LoadGame())
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.Log("저장된 파일이 없습니다.");
        }
    }

    // 연구소 창 켜기
    public void OnClickOpenScience()
    {
        sciencePanel.SetActive(true); // 켜기
    }

    public void OnClickCloseScience()
    {
        sciencePanel.SetActive(false); // 끄기
    }

    // 설정 창 켜기
    public void OnClickOption()
    {
        optionPanel.SetActive(true);
    }

    // 설정 창 닫기
    public void OnClickCloseOption()
    {
        optionPanel.SetActive(false);
    }

    // 게임 종료
    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}