using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Input (assign in Inspector)")]
    [SerializeField] private InputActionReference moveAction;   
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference aimAction;

    [Header("Tuning")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField, Range(0f, 1f)] private float aimUpEnter = 0.6f;
    [SerializeField, Range(0f, 1f)] private float aimUpExit = 0.4f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;  
    [SerializeField] private float groundRadius = 0.15f;
    [SerializeField] private LayerMask groundMask;

    private Rigidbody2D rb;
    private Animator anim;
    private bool isFacingRight = true;
    private Vector2 move;
    private bool isAimingUp = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void OnEnable()
    { 
        moveAction.action.Enable(); 
        jumpAction.action.Enable();
        aimAction.action.Enable();
    }
    private void OnDisable()
    { 
        moveAction.action.Disable(); 
        jumpAction.action.Disable();
        aimAction.action.Disable();
    }

    private void FixedUpdate()
    {
        // Read Vector2 and use x only for horizontal move
        move = moveAction.action.ReadValue<Vector2>();
        rb.linearVelocity = new Vector2(move.x * moveSpeed, rb.linearVelocity.y);

        if(move.x != 0f)
        {
            isFacingRight = move.x > 0f;
            Vector3 scale = transform.localScale;
            scale.x = isFacingRight ? 1f : -1f;
            transform.localScale = scale;
        }
    }

    private void Update()
    {
        // Ground check + jump
        bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundMask);

        if (grounded && jumpAction.action.WasPressedThisFrame())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        Vector2 aimDirection = Vector2.zero;
        aimDirection = aimAction.action.ReadValue<Vector2>();

        isAimingUp = aimDirection.y >= aimUpEnter;

        if(isAimingUp)
        {
            isAimingUp = !(aimDirection.y < aimUpExit);
        }
        else
        {
            isAimingUp = aimDirection.y > aimUpEnter;
        }

        SetBoolIfChanged("isAimingUp", isAimingUp);
        SetBoolIfChanged("isGrounded", grounded);
        anim.SetFloat("moveX", Mathf.Abs(rb.linearVelocityX));
        SetBoolIfChanged("isWalking", Mathf.Abs(rb.linearVelocityX) > 0.1f);
    }

    public Vector2 GetAimDir()
    {
        if (isAimingUp) return Vector2.up;
        return new Vector2(transform.localScale.x >= 0f ? 1f : -1f, 0f);
    }

    private void SetBoolIfChanged(string param, bool value)
    {
        if (anim.GetBool(param) != value) anim.SetBool(param, value);
    }

    private void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}