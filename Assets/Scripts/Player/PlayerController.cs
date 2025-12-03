using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("참조")]
    private PlayerStats stats;

    [Header("모바일 조이스틱")]
    // [SerializeField] private Joystick joyStick; 

    private Vector2 inputVec;
    private Rigidbody2D rb;
    private SpriteRenderer spriter;
    private Animator anim;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        inputVec.x = Input.GetAxisRaw("Horizontal");
        inputVec.y = Input.GetAxisRaw("Vertical");

        // 조이스틱
        // if (joyStick != null && (inputVec.x == 0 && inputVec.y == 0))
        // {
        //     inputVec.x = joyStick.Horizontal;
        //     inputVec.y = joyStick.Vertical;
        // }

        if (inputVec.x != 0)
        {
            spriter.flipX = inputVec.x < 0;
        }

        //anim.SetBool("isRun", inputVec.magnitude > 0);
    }

    void FixedUpdate()
    {
        Vector2 nextVec = inputVec.normalized * stats.Speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + nextVec);
    }
}