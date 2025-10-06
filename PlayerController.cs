using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Input (assign in Inspector)")]
    [SerializeField] private InputActionReference moveAction;   
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference aimAction;
    [SerializeField] private InputActionReference shootAction;

    [Header("Tuning")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField, Range(0f, 1f)] private float aimUpEnter = 0.6f;
    [SerializeField, Range(0f, 1f)] private float aimUpExit = 0.4f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;  
    [SerializeField] private float groundRadius = 0.15f;
    [SerializeField] private LayerMask groundMask;

    [Header("Weapon")]
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform firePoint;

    [Header("Damage")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float hitInvincibilityDuration = 0.5f;

    public int Health;
    public int maxHealth = 3;
    public bool isUsingWeapon = false;
    public bool isUsingVacuum = false;
    public bool isUsingTorch = false;
    

    private Rigidbody2D rb;
    private Animator anim;
    private bool isFacingRight = true;
    private Vector2 move;
    private bool isAimingUp = false;
    private UIController uiController;
    private float hitInvincibilityTimer;
    private bool isKnockedBack = false;

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
        shootAction.action.Enable();
    }
    private void OnDisable()
    { 
        moveAction.action.Disable(); 
        jumpAction.action.Disable();
        aimAction.action.Disable();
        shootAction.action.Disable();
    }

    private void Start()
    {
        Health = maxHealth;
        hitInvincibilityTimer = 0f;

        uiController = FindFirstObjectByType<UIController>();
        if (uiController == null)
        {
            Debug.LogError("UIController not found in the scene.");
        }
        else
        {
            uiController.UpdateHealthDisplay(Health);
        }
    }

    private void FixedUpdate()
    {
        if (isKnockedBack)
        {
            return;
        }

        move = moveAction.action.ReadValue<Vector2>();
        rb.linearVelocity = new Vector2(move.x * moveSpeed, rb.linearVelocity.y);

        if(move.x != 0f  && !isUsingWeapon)
        {
            isFacingRight = move.x > 0f;
            Vector3 scale = transform.localScale;
            scale.x = isFacingRight ? 1f : -1f;
            transform.localScale = scale;
        }
    }

    private void Update()
    {
        if (hitInvincibilityTimer > 0f)
        {
            hitInvincibilityTimer -= Time.deltaTime;
        }
        
        if(isKnockedBack)
        {
            return;
        }

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

        if (shootAction.action.WasPressedThisFrame())
        {
            Shoot(aimDirection);
        }

        SetBoolIfChanged("isAimingUp", isAimingUp);
        SetBoolIfChanged("isGrounded", grounded);
        anim.SetFloat("moveX", Mathf.Abs(rb.linearVelocity.x));
        SetBoolIfChanged("isWalking", Mathf.Abs(rb.linearVelocity.x) > 0.1f);
    }

    private void Shoot(Vector2 aimDirection)
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.GetComponent<PlayerProjectile>().Initialize(aimDirection.normalized);
    }

    public void TakeDamage(int damage)
    {
        if (hitInvincibilityTimer > 0f) return;

        hitInvincibilityTimer = hitInvincibilityDuration;
        Health -= damage;

        if (Health <= 0)
        {
            Health = 0;
            uiController.UpdateHealthDisplay(Health);
            Debug.Log("Player has died.");
        }
        else
        {
            uiController.UpdateHealthDisplay(Health);
        }
    }

    private void ApplyKnockback(Vector2 sourcePosition)
    {
        if (!isKnockedBack)
        {
            StartCoroutine(KnockbackCoroutine((transform.position - (Vector3)sourcePosition).normalized));
        }
    }

    IEnumerator KnockbackCoroutine(Vector2 knockbackDir)
    {
        isKnockedBack = true;
        anim.SetTrigger("hit");
        anim.SetBool("isHit", true);
        rb.linearVelocity = Vector2.zero; // Clear existing velocity
        rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        isKnockedBack = false;
        anim.SetBool("isHit", false);
    }

    public void Heal(int amount)
    {
        if (Health >= maxHealth) return;
        Health += amount;
        uiController.UpdateHealthDisplay(Health);
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyGhostController enemy = collision.gameObject.GetComponent<EnemyGhostController>();

            if (enemy != null && enemy.isCapturable)
            {
                return;
            }

            ApplyKnockback(collision.transform.position);
            int damage = collision.gameObject.GetComponent<DamagePlayer>().GetDamageAmount();
            TakeDamage(damage);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyGhostController enemy = collision.gameObject.GetComponent<EnemyGhostController>();

            if (enemy != null && enemy.isCapturable)
            {
                return;
            }

            ApplyKnockback(collision.transform.position);
            int damage = collision.gameObject.GetComponent<DamagePlayer>().GetDamageAmount();
            TakeDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}