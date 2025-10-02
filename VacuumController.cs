using UnityEngine;
using UnityEngine.InputSystem;

public class VacuumController : MonoBehaviour
{
    [Header("Input (assign in Inspector)")]
    [SerializeField] private InputActionReference vacuumAction; // Button (hold)

    [Header("Geometry")]
    [SerializeField] private Transform suctionOrigin;           // where the beam starts (e.g., hand)
    [SerializeField] private float maxRange = 5f;
    [SerializeField, Range(1f, 120f)] private float coneAngleDeg = 40f;

    [Header("Effects")]
    [SerializeField] private float pullForce = 25f;             // physics pull toward origin
    [SerializeField] private float captureDps = 1.2f;           // progress per second while in cone
    [SerializeField] private LayerMask ghostMask;
    [SerializeField] private LayerMask obstacleMask;            // walls/geometry blocking LOS
    [SerializeField] private GameObject beamVFX;                // optional: a line/quad sprite

    private PlayerController player;
    private Rigidbody2D rb; // optional, if you need player data
    private Vector3 beamBaseScale;

    private void Awake()
    {
        if (!suctionOrigin) suctionOrigin = transform;
        player = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        if (beamVFX) beamBaseScale = beamVFX.transform.localScale;
    }

    private void OnEnable() { vacuumAction.action.Enable(); }
    private void OnDisable() { vacuumAction.action.Disable(); }

    private void Update()
    {
        bool active = vacuumAction.action.IsPressed();
        if (beamVFX) beamVFX.SetActive(active);

        if (active && beamVFX) UpdateBeamPose(maxRange); // visual to max, trimmed per-target below
    }

    private void FixedUpdate()
    {
        if (!vacuumAction.action.IsPressed()) return;

        Vector2 aim = player ? player.GetAimDir() : Vector2.right;
        Vector2 origin = suctionOrigin.position;

        // Broadphase: circle overlap
        Collider2D[] cols = Physics2D.OverlapCircleAll(origin, maxRange, ghostMask);
        if (cols.Length == 0) return;

        float bestDist = Mathf.Infinity;
        Vector2 bestPoint = origin;

        foreach (var col in cols)
        {
            if (!col || !col.TryGetComponent<EnemySuck>(out var ghost) || !ghost.IsCapturable)
                continue;

            Vector2 toGhost = (Vector2)col.bounds.center - origin;
            float dist = toGhost.magnitude;
            if (dist <= 0.001f) continue;

            // Cone check
            float ang = Vector2.Angle(aim, toGhost.normalized);
            if (ang > coneAngleDeg * 0.5f) continue;

            // LOS check
            var hit = Physics2D.Raycast(origin, toGhost.normalized, dist, obstacleMask);
            if (hit.collider) continue;

            // Pull physics (if ghost has RB)
            if (col.attachedRigidbody)
            {
                // Toward origin; scale by distance a bit for feel
                Vector2 dir = toGhost.normalized * -1f;
                float falloff = Mathf.Clamp01(1f - (dist / maxRange));
                col.attachedRigidbody.AddForce(dir * (pullForce * (0.5f + 0.5f * falloff)), ForceMode2D.Force);
            }

            // Apply capture progress
            ghost.ApplyVacuumProgress(captureDps * Time.fixedDeltaTime);

            // Track closest for beam shortening
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPoint = (Vector2)col.bounds.center;
            }
        }

        // Shorten the beam to the closest valid target
        if (beamVFX && bestDist < Mathf.Infinity)
            UpdateBeamPose(bestDist, bestPoint);
    }

    // --- Beam pose helpers (simple quad/sprite stretched from origin toward aim) ---
    private void UpdateBeamPose(float length, Vector2? toPoint = null)
    {
        Vector2 origin = suctionOrigin.position;
        Vector2 aim = player ? player.GetAimDir() : Vector2.right;

        Vector2 end = toPoint.HasValue ? toPoint.Value : origin + aim.normalized * length;
        Vector2 mid = (origin + end) * 0.5f;
        Vector2 dir = (end - origin);
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        beamVFX.transform.position = mid;
        beamVFX.transform.rotation = Quaternion.Euler(0f, 0f, ang);

        // Assumes the beam sprite is 1 unit long on X; scale X to length
        beamVFX.transform.localScale = new Vector3(length, beamBaseScale.y, beamBaseScale.z);
    }

    private void OnDrawGizmosSelected()
    {
        if (!suctionOrigin) return;
        var origin = (Vector2)suctionOrigin.position;
        Vector2 aim = Application.isPlaying && player ? player.GetAimDir()
                                                      : new Vector2(transform.localScale.x >= 0f ? 1f : -1f, 0f);

        // Draw cone
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
        float half = coneAngleDeg * 0.5f;
        Quaternion qL = Quaternion.Euler(0, 0, half);
        Quaternion qR = Quaternion.Euler(0, 0, -half);
        Vector2 vL = (Vector2)(qL * (Vector3)aim).normalized * maxRange;
        Vector2 vR = (Vector2)(qR * (Vector3)aim).normalized * maxRange;
        Gizmos.DrawLine(origin, origin + vL);
        Gizmos.DrawLine(origin, origin + vR);
        Gizmos.DrawWireSphere(origin, 0.07f);
    }
}
