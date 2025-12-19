using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartCleaner : MonoBehaviour
{
    // 이 스크립트가 켜질 때 "씬 로딩 감지기"를 킴
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 이 스크립트가 꺼질 때 감지기를 끔
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 게임이 시작되거나 리스타트 될 때마다 이 함수가 자동 실행
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (ObjectPoolManager.Instance != null)
        {
            // 매니저의 자식 전부 검사
            foreach (Transform child in ObjectPoolManager.Instance.transform)
            {
                // 켜져 있는 놈들 강제 끔
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }
}