using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("참조")]
    private PlayerStats stats;

    [Header("시각 효과")]
    public Transform bodyTransform;

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
        anim = GetComponentInChildren<Animator>(); 
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
            Vector3 scale = bodyTransform.localScale;
            scale.x = inputVec.x < 0 ? -1 : 1;
            bodyTransform.localScale = scale;
        }

        //anim.SetBool("isRun", inputVec.magnitude > 0);
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
        Vector2 nextVec = inputVec.normalized * stats.Speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + nextVec);
    }
}