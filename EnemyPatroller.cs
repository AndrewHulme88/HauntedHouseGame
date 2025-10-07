using UnityEngine;
using System.Collections;
using Unity.Hierarchy;

public class EnemyPatroller : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float pauseTime = 0.5f;
    [SerializeField] private float hitDuration = 0.2f;

    public int maxHealth = 3;

    private Rigidbody2D rb;
    private bool isMovingRight = true;
    private bool isTurning = false;
    private Animator anim;
    private float hitTimer = 0f;
    private int currentHealth;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        if (hitTimer > 0)
        {
            rb.linearVelocity = Vector2.zero;
            hitTimer -= Time.fixedDeltaTime;
            return;
        }

        if (isTurning) return;

        rb.linearVelocity = new Vector2((isMovingRight ? 1 : -1) * moveSpeed, rb.linearVelocity.y);
        RaycastHit2D groundHit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheck.position, isMovingRight ? Vector2.right : Vector2.left, wallCheckDistance, groundLayer);

        if (!groundHit.collider || wallHit.collider)
        {
            StartCoroutine(FlipAfterPause());
        }
    }

    private void Update()
    {
        anim.SetFloat("moveX", Mathf.Abs(rb.linearVelocity.x));
    }

    public void TakeDamage(int damageAmount)
    {
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }

        if (hitTimer > 0) return;

        hitTimer = hitDuration;
        currentHealth -= damageAmount;
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("hit");
    }

    private IEnumerator FlipAfterPause()
    {
        isTurning = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        yield return new WaitForSeconds(pauseTime);
        Flip();
        isTurning = false;
    }

    private void Flip()
    {
        isMovingRight = !isMovingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
