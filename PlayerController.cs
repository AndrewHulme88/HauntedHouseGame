using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Input (assign in Inspector)")]
    [SerializeField] private InputActionReference moveAction;   
    [SerializeField] private InputActionReference jumpAction;   

    [Header("Tuning")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;  
    [SerializeField] private float groundRadius = 0.15f;
    [SerializeField] private LayerMask groundMask;

    private Rigidbody2D rb;
    private bool isFacingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    { 
        moveAction.action.Enable(); jumpAction.action.Enable(); 
    }
    private void OnDisable()
    { 
        moveAction.action.Disable(); jumpAction.action.Disable(); 
    }

    private void FixedUpdate()
    {
        // Read Vector2 and use x only for horizontal move
        Vector2 move = moveAction.action.ReadValue<Vector2>();
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
        // Simple grounded check + jump
        bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundMask);
        if (grounded && jumpAction.action.WasPressedThisFrame())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}