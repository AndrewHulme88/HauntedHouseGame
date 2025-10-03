using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class EnemyGhostController : MonoBehaviour
{
    [Header("Room")]
    [SerializeField] private BoxCollider2D roomBounds;   // assign a BoxCollider2D that outlines the room

    [Header("Wander")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float waypointRadius = 0.25f;
    [SerializeField] private Vector2 dwellTimeRange = new Vector2(0.4f, 1.2f); // pause at waypoint
    [SerializeField] private float retargetEvery = 4f;   // failsafe: pick a new target if stuck
    [SerializeField] private bool isFacingRight = true;

    [Header("Avoidance")]
    [SerializeField] private LayerMask obstacleMask;      // walls/solid tiles
    [SerializeField] private float lookAhead = 0.8f;      // ray length
    [SerializeField] private float avoidStrength = 6f;    // steering strength

    [Header("Hover")]
    [SerializeField] private float hoverAmplitude = 0.05f;
    [SerializeField] private float hoverSpeed = 3f;

    private Rigidbody2D rb;
    private Vector2 target;
    private float dwellUntil;
    private float retargetAt;
    private float hoverPhase;
    private bool isPaused = false;
    private bool isStunned = false;
    private EnemySuck enemySuck;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (!roomBounds)
        {
            Debug.LogWarning($"{name}: roomBounds not set on GhostRoam2D.");
        }

        enemySuck = GetComponent<EnemySuck>();
        PickNewTarget();
        hoverPhase = Random.value * Mathf.PI * 2f;
    }

    private void OnEnable()
    {
        if (enemySuck != null) enemySuck.OnCapturableChanged += HandleCapturableChanged;
    }

    private void OnDisable()
    {
        if (enemySuck != null) enemySuck.OnCapturableChanged -= HandleCapturableChanged;
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

        // Dwell (brief pause at target)
        if (Time.time < dwellUntil)
        {
            rb.linearVelocity = Vector2.zero + HoverOffset();
            return;
        }

        // Retarget by time or if reached
        if ((target - (Vector2)transform.position).sqrMagnitude <= waypointRadius * waypointRadius ||
            Time.time >= retargetAt)
        {
            StartDwell();
            PickNewTarget();
        }

        // Desired velocity toward target
        Vector2 pos = rb.position;
        Vector2 desired = (target - pos).normalized * moveSpeed;

        // Simple obstacle avoidance (one forward ray + two slight side rays)
        Vector2 fwd = desired.normalized;
        Vector2 steer = Vector2.zero;
        steer += AvoidRay(pos, fwd, lookAhead);
        steer += AvoidRay(pos, Rotate(fwd, 20f), lookAhead * 0.85f);
        steer += AvoidRay(pos, Rotate(fwd, -20f), lookAhead * 0.85f);

        Vector2 finalVel = desired + steer * avoidStrength;
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

    private void HandleCapturableChanged(bool isCapturable)
    {
        isPaused = isCapturable;
        if (isPaused)
        {
            // Stop autonomous motion so vacuum can take over
            rb.linearVelocity = Vector2.zero;
        }
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

        retargetAt = Time.time + retargetEvery;
    }

    private void StartDwell()
    {
        float t = Random.Range(dwellTimeRange.x, dwellTimeRange.y);
        dwellUntil = Time.time + t;
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

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        float cs = Mathf.Cos(r), sn = Mathf.Sin(r);
        return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
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