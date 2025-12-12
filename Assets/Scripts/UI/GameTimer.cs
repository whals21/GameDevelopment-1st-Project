using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("UI ¿¬°á")]
    [SerializeField] private TextMeshProUGUI timerText;

    private float time;

    void Update()
    {
        time += Time.deltaTime;

        int min = Mathf.FloorToInt(time / 60);
        int sec = Mathf.FloorToInt(time % 60);

        timerText.text = string.Format("{0:D2}:{1:D2}", min, sec);
    }
}