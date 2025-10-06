using UnityEngine;
using System.Collections;

public class EnemyPatroller : MonoBehaviour
{
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float groundCheckDistance = 0.1f;
    [SerializeField] Transform wallCheck;
    [SerializeField] float wallCheckDistance = 0.5f;
    [SerializeField] float pauseTime = 0.5f;

    private Rigidbody2D rb;
    private bool isMovingRight = true;
    private bool isTurning = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (isTurning) return;

        rb.linearVelocity = new Vector2((isMovingRight ? 1 : -1) * moveSpeed, rb.linearVelocity.y);
        RaycastHit2D groundHit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheck.position, isMovingRight ? Vector2.right : Vector2.left, wallCheckDistance, groundLayer);

        if (!groundHit.collider || wallHit.collider)
        {
            StartCoroutine(FlipAfterPause());
        }
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
