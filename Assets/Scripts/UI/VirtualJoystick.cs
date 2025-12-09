using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI 연결")]
    [SerializeField] private RectTransform containerRect;
    [SerializeField] private RectTransform bgRect;
    [SerializeField] private RectTransform handleRect;

    [Header("설정")]
    [SerializeField] private float joystickRadius = 100f;

    // [외부 접근용]
    private Vector2 inputVector;
    public float Horizontal { get { return inputVector.x; } }
    public float Vertical { get { return inputVector.y; } }

    void Start()
    {
        // 조이스틱 숨기기
        bgRect.gameObject.SetActive(false);
        inputVector = Vector2.zero;
    }

    // 화면 어디든 터치시 소환
    public void OnPointerDown(PointerEventData eventData)
    {
        // 조이스틱 오픈
        bgRect.gameObject.SetActive(true);

        // 터치한 위치로 이동시키기
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            bgRect.anchoredPosition = localPoint;
        }

        // 핸들 위치 초기화 (중앙)
        handleRect.anchoredPosition = Vector2.zero;

        OnDrag(eventData);
    }

    // 터치 중일 때
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bgRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            localPoint = Vector2.ClampMagnitude(localPoint, joystickRadius);

            inputVector = localPoint / joystickRadius;

            handleRect.anchoredPosition = localPoint;
        }
    }

    // 터치 종료시
    public void OnPointerUp(PointerEventData eventData)
    {
        // 조이스틱 숨기기
        bgRect.gameObject.SetActive(false);

        inputVector = Vector2.zero;
        handleRect.anchoredPosition = Vector2.zero;
    }
}