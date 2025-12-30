using UnityEngine;

/// <summary>
/// 플레이어 컨트롤러
/// 게임 내에서 단 하나만 존재하는 전역 플레이어 객체입니다.
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region Singleton
    /// <summary>
    /// 전역 플레이어 인스턴스
    /// 스킬 시스템 등에서 빠른 참조를 위해 사용합니다.
    /// </summary>
    public static PlayerController Instance { get; private set; }
    #endregion

    [Header("����")]
    private PlayerStats stats;

    [Header("�ð� ȿ��")]
    public Transform bodyTransform;

    [Header("����� ���̽�ƽ")]
    [SerializeField] private VirtualJoystick joyStick;

    // 패시브 스킬 관련
    private float movementSpeedMultiplier = 1f;

    private Vector2 inputVec;
    private Rigidbody2D rb;
    private SpriteRenderer spriter;
    private Animator anim;

    void Awake()
    {
        // 싱글톤 패턴
        if (Instance != null)
        {
            Debug.LogWarning("[PlayerController] 여러 PlayerController가 감지되었습니다. 기존 인스턴스를 파기합니다.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        inputVec.x = Input.GetAxisRaw("Horizontal");
        inputVec.y = Input.GetAxisRaw("Vertical");

        if (joyStick != null && (joyStick.Horizontal != 0 || joyStick.Vertical != 0))
        {
            inputVec.x = joyStick.Horizontal;
            inputVec.y = joyStick.Vertical;
        }

        if (inputVec.x != 0)
        {
            Vector3 scale = bodyTransform.localScale;
            scale.x = inputVec.x < 0 ? -1 : 1;
            bodyTransform.localScale = scale;
        }

        // if (anim != null)
        // {
        //     anim.SetBool("isRun", inputVec.magnitude > 0);
        // }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            stats.TakeDamage(10f * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        Vector2 moveDist = Vector2.ClampMagnitude(inputVec, 1f);

        Vector2 nextVec = moveDist * stats.Speed * movementSpeedMultiplier * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + nextVec);
    }

    //12-22 조민희가 추가함
    #region Passive Skill Integration 
    /// <summary>
    /// 이동속도 배수 설정 (패시브 스킬용)
    /// </summary>
    /// <param name="multiplier">이동속도 배수 (1.0 = 100%)</param>
    public void SetMovementSpeedMultiplier(float multiplier)
    {
        movementSpeedMultiplier = Mathf.Max(0.1f, multiplier);
        Debug.Log($"[PlayerController] 이동속도 배수가 {movementSpeedMultiplier:F2}(으)로 설정됨");
    }

    /// <summary>
    /// 현재 이동속도 배수 반환
    /// </summary>
    /// <returns>이동속도 배수</returns>
    public float GetMovementSpeedMultiplier()
    {
        return movementSpeedMultiplier;
    }
    #endregion
}