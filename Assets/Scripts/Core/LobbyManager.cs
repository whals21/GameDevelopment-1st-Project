using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("설정")]
    public string gameSceneName = "MainScene"; // 이동할 씬 이름
    public GameObject optionPanel;

    // 새로 하기
    public void OnClickNewGame()
    {
        // 나중에 여기서 '데이터 초기화' 진행
        SceneManager.LoadScene(gameSceneName);
    }

    // 이어 하기
    public void OnClickContinue()
    {
        // 나중에 세이브 로드 기능 구현 예정
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