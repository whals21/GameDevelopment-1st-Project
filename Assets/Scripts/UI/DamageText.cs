using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private TextMeshProUGUI textMesh;

    [Header("설정")]
    [SerializeField] private float moveSpeed = 2f; // 위로 올라가는 속도
    [SerializeField] private float lifeTime = 1f; // 보여지는 시간

    [Header("스타일 (일반)")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private float normalSize = 30f;

    [Header("스타일 (플레이어 피격)")]
    [SerializeField] private Color playerColor = Color.magenta;

    [Header("스타일 (크리티컬)")]
    [SerializeField] private Color critColor = Color.red;
    [SerializeField] private float critSize = 45f;

    private float timer;
    private Color initialColor;

    // 풀에서 꺼낼 때 호출될 초기화 함수
    public void Init(float damage, bool isCritical, Vector3 position, bool isPlayer = false)
    {
        timer = 0;
        transform.position = position + Vector3.up * 0.5f; // Enemy 머리 위
        gameObject.SetActive(true);

        // 텍스트 설정
        textMesh.text = damage.ToString("F0");

        // 스타일 적용
        if (isPlayer)
        {
            // 플레이어 피격
            textMesh.color = playerColor;
            textMesh.fontSize = normalSize;
            textMesh.fontStyle = FontStyles.Bold;
            initialColor = playerColor;
        }
        else if (isCritical)
        {
            // 적 크리티컬 피격
            textMesh.color = critColor;
            textMesh.fontSize = critSize;
            textMesh.fontStyle = FontStyles.Bold;
            initialColor = critColor;
        }
        else
        {
            // 적 일반 피격
            textMesh.color = normalColor;
            textMesh.fontSize = normalSize;
            textMesh.fontStyle = FontStyles.Normal;
            initialColor = normalColor;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 위로 이동
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 페이드 아웃
        float alpha = Mathf.Lerp(1f, 0f, timer / lifeTime);
        textMesh.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);

        // 임무 끝 반환
        if (timer >= lifeTime)
        {
            // ObjectPoolManager가 있다면 반환, 없다면 그냥 비활성화 (안전장치)
            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.ReturnDamageText(this);
            else
                gameObject.SetActive(false);
        }
    }
}