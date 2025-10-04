using System.Text.RegularExpressions;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class EnemyGhostController : MonoBehaviour
{
    [Header("Room")]
    [SerializeField] private BoxCollider2D roomBounds;   

    [Header("Wander")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float waypointRadius = 0.25f;
    [SerializeField] private Vector2 waitTimeRange = new Vector2(0.4f, 1.2f); // pause at waypoint
    [SerializeField] private float retargetDelay = 4f;   // failsafe: pick a new target if stuck
    [SerializeField] private bool isFacingRight = true;

    [Header("Avoidance")]
    [SerializeField] private LayerMask obstacleMask;      // walls/solid tiles
    [SerializeField] private float lookAheadDistance = 0.8f;     
    [SerializeField] private float avoidDistance = 6f;   

    [Header("Hover")]
    [SerializeField] private float hoverAmplitude = 0.05f;
    [SerializeField] private float hoverSpeed = 3f;

    public bool isCapturable = false;

    private Rigidbody2D rb;
    private Vector2 target;
    private float waitUntil;
    private float retargetTime;
    private float hoverPhase;
    private bool isPaused = false;
    private bool isStunned = false;
    private Collider2D enemyCollider;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();

        if (!roomBounds)
        {
            Debug.LogWarning($"{name}: roomBounds not set on GhostRoam2D.");
        }

        PickNewTarget();
        hoverPhase = Random.value * Mathf.PI * 2f;
    }

    private void FixedUpdate()
    {
        if (isPaused)
        {
            rb.linearVelocity = Vector2.zero;
            isStunned = true;
            isPaused = false;
            return;
        }

        if(isStunned)
        {
            return;
        }

        // Pause at target
        if (Time.time < waitUntil)
        {
            rb.linearVelocity = Vector2.zero + HoverOffset();
            return;
        }

        // Retarget by time or if reached
        if ((target - (Vector2)transform.position).sqrMagnitude <= waypointRadius * waypointRadius ||
            Time.time >= retargetTime)
        {
            StartWait();
            PickNewTarget();
        }

        // Desired velocity toward target
        Vector2 pos = rb.position;
        Vector2 desired = (target - pos).normalized * moveSpeed;

        // Simple obstacle avoidance (one forward ray + two slight side rays)
        Vector2 fwd = desired.normalized;
        Vector2 steer = Vector2.zero;
        steer += AvoidRay(pos, fwd, lookAheadDistance);
        steer += AvoidRay(pos, Rotate(fwd, 20f), lookAheadDistance * 0.85f);
        steer += AvoidRay(pos, Rotate(fwd, -20f), lookAheadDistance * 0.85f);

        Vector2 finalVel = desired + steer * avoidDistance;
        rb.linearVelocity = finalVel + HoverOffset();

        const float eps = 0.3f; // deadzone
        float velocityX = rb.linearVelocity.x;

        if (Mathf.Abs(velocityX) > eps)
        {
            bool faceRight = velocityX > 0f;
            if (faceRight != isFacingRight)
            {
                isFacingRight = faceRight;
                var scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (isFacingRight ? -1f : 1f);
                transform.localScale = scale;
            }
        }
    }

    public void SetCapturable(bool value)
    {
        isCapturable = value;
        isPaused = isCapturable;
        if (isCapturable)
        {
            enemyCollider.isTrigger = true;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void Capture()
    {
        // TODO: play particles / sound / award points
        Destroy(gameObject);
    }

    private void PickNewTarget()
    {
        if (!roomBounds)
        {
            // fallback: small random circle around current pos
            target = (Vector2)transform.position + Random.insideUnitCircle * 2f;
        }
        else
        {
            Bounds b = roomBounds.bounds;
            // Keep some margin from walls
            float mx = Mathf.Min(0.6f, b.extents.x * 0.5f);
            float my = Mathf.Min(0.6f, b.extents.y * 0.5f);
            float x = Random.Range(b.min.x + mx, b.max.x - mx);
            float y = Random.Range(b.min.y + my, b.max.y - my);
            target = new Vector2(x, y);
        }

        retargetTime = Time.time + retargetDelay;
    }

    private void StartWait()
    {
        float waitTime = Random.Range(waitTimeRange.x, waitTimeRange.y);
        waitUntil = Time.time + waitTime;
    }

    private Vector2 AvoidRay(Vector2 origin, Vector2 dir, float length)
    {
        var hit = Physics2D.Raycast(origin, dir, length, obstacleMask);
        if (hit.collider == null) return Vector2.zero;

        // steer away: project away from hit normal and slightly along tangent
        Vector2 away = hit.normal; // points away from obstacle
        Vector2 tangent = new Vector2(-dir.y, dir.x);
        return (away * 1.0f + tangent * 0.2f) * Mathf.Clamp01(1f - hit.distance / length);
    }

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float rotation = degrees * Mathf.Deg2Rad;
        float cosine = Mathf.Cos(rotation); 
        float sine = Mathf.Sin(rotation);
        return new Vector2(vector.x * cosine - vector.y * sine, vector.x * sine + vector.y * cosine);
    }

    private Vector2 HoverOffset()
    {
        hoverPhase += hoverSpeed * Time.fixedDeltaTime;
        return new Vector2(0f, Mathf.Sin(hoverPhase) * hoverAmplitude);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target, waypointRadius);
        if (roomBounds)
        {
            Gizmos.color = new Color(0.2f, 1f, 1f, 0.35f);
            Gizmos.DrawWireCube(roomBounds.bounds.center, roomBounds.bounds.size);
        }
    }
}