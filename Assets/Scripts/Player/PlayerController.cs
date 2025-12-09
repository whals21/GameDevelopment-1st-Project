using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("ï¿½ï¿½ï¿½ï¿½")]
    private PlayerStats stats;

    [Header("ï¿½Ã°ï¿½ È¿ï¿½ï¿½")]
    public Transform bodyTransform;

<<<<<<< HEAD
    [Header("ï¿½ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½Ì½ï¿½Æ½")]
    // [SerializeField] private Joystick joyStick; 
=======
    [Header("¸ð¹ÙÀÏ Á¶ÀÌ½ºÆ½")]
    [SerializeField] private VirtualJoystick joyStick;
>>>>>>> ec2f37cc668ac1985ad4568396afc5afa73be997

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

<<<<<<< HEAD
        // ï¿½ï¿½ï¿½Ì½ï¿½Æ½
        // if (joyStick != null && (inputVec.x == 0 && inputVec.y == 0))
        // {
        //     inputVec.x = joyStick.Horizontal;
        //     inputVec.y = joyStick.Vertical;
        // }
=======
        if (joyStick != null && (joyStick.Horizontal != 0 || joyStick.Vertical != 0))
        {
            inputVec.x = joyStick.Horizontal;
            inputVec.y = joyStick.Vertical;
        }
>>>>>>> ec2f37cc668ac1985ad4568396afc5afa73be997

        if (inputVec.x != 0)
        {
            Vector3 scale = bodyTransform.localScale;
            scale.x = inputVec.x < 0 ? -1 : 1;
            bodyTransform.localScale = scale;
        }

        if (anim != null)
        {
            anim.SetBool("isRun", inputVec.magnitude > 0);
        }
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

        Vector2 nextVec = moveDist * stats.Speed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + nextVec);
    }
}