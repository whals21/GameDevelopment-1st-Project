using UnityEngine;
using UnityEngine.EventSystems;

// 드래그(Drag), 누르기(PointerDown), 떼기(PointerUp) 이벤트를 받겠다고 선언
public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI 연결")]
    [SerializeField] private RectTransform bgRect;
    [SerializeField] private RectTransform handleRect;

    [Header("설정")]
    [SerializeField] private float joystickRadius = 100f; // 핸들이 움직일 수 있는 최대 반경

    // 외부 읽기용
    private Vector2 inputVector;
    public float Horizontal { get { return inputVector.x; } }
    public float Vertical { get { return inputVector.y; } }

    void Start()
    {
        // 시작시 핸들 위치 초기화
        inputVector = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;

        // 화면 터치 좌표를 UI 내부 좌표로 변환
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bgRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            localPoint = Vector2.ClampMagnitude(localPoint, joystickRadius);

            // 입력값 계산 (-1 ~ 1 사이 값)
            inputVector = localPoint / joystickRadius;

            handleRect.anchoredPosition = localPoint;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 핸들과 입력값을 원점으로 복귀
        inputVector = Vector2.zero;
        handleRect.anchoredPosition = Vector2.zero;
    }
}