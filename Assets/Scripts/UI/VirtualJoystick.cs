using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI ����")]
    [SerializeField] private RectTransform containerRect;
    [SerializeField] private RectTransform bgRect;
    [SerializeField] private RectTransform handleRect;

    [Header("����")]
    [SerializeField] private float joystickRadius = 100f;

    // [�ܺ� ���ٿ�]
    private Vector2 inputVector;
    public float Horizontal { get { return inputVector.x; } }
    public float Vertical { get { return inputVector.y; } }

    void Start()
    {
        // ���̽�ƽ �����
        bgRect.gameObject.SetActive(false);
        inputVector = Vector2.zero;
    }

    // ȭ�� ���� ��ġ�� ��ȯ
    public void OnPointerDown(PointerEventData eventData)
    {
        // ���̽�ƽ ����
        bgRect.gameObject.SetActive(true);

        // ��ġ�� ��ġ�� �̵���Ű��
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            bgRect.anchoredPosition = localPoint;
        }

        // �ڵ� ��ġ �ʱ�ȭ (�߾�)
        handleRect.anchoredPosition = Vector2.zero;

        OnDrag(eventData);
    }

    // ��ġ ���� ��
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

    // ��ġ �����
    public void OnPointerUp(PointerEventData eventData)
    {
        // ���̽�ƽ �����
        bgRect.gameObject.SetActive(false);

        inputVector = Vector2.zero;
        handleRect.anchoredPosition = Vector2.zero;
    }
}