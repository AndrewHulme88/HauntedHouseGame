using UnityEngine;
using UnityEngine.InputSystem;

public class VacuumController : MonoBehaviour
{
    [Header("Input (assign in Inspector)")]
    [SerializeField] private InputActionReference vacuumAction; 

    [Header("Geometry")]
    [SerializeField] private Transform suctionOrigin;
    [SerializeField] private float maxRange = 5f;
    [SerializeField, Range(1f, 120f)] private float coneAngleDeg = 40f;

    [Header("Effects")]
    [SerializeField] private float pullForce = 25f;            
    [SerializeField] private LayerMask ghostMask;
    [SerializeField] private LayerMask obstacleMask;            // walls/geometry blocking LOS
    [SerializeField] private LayerMask pickupMask;
    [SerializeField] private GameObject beamVFX;
    [SerializeField] private float collectRadius = 0.6f;
    [SerializeField] private float pickupMoveSpeed = 12f;

    private PlayerController playerController;
    private Vector3 beamBaseScale;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (!suctionOrigin)
        {
            suctionOrigin = transform;
        }

        if (beamVFX)
        {
            beamBaseScale = beamVFX.transform.localScale;
        }
    }

    private void OnEnable() 
    { 
        vacuumAction.action.Enable(); 
    }

    private void OnDisable() 
    { 
        vacuumAction.action.Disable(); 
    }

    private void Update()
    {
        bool active = vacuumAction.action.IsPressed();

        if (beamVFX)
        {
            beamVFX.SetActive(active);
        }

        if (active && beamVFX)
        {
            UpdateBeamPose(maxRange);
        }
    }

    private void FixedUpdate()
    {
        if (!vacuumAction.action.IsPressed()) return;

        Vector2 origin = suctionOrigin.position;
        Vector2 aim = playerController ? playerController.GetAimDir() : Vector2.right;

        float bestDist = Mathf.Infinity;
        Vector2 bestPoint = origin;

        // ---- Stunned ghosts: pull-only (same as pickups) ----
        var ghosts = Physics2D.OverlapCircleAll(origin, maxRange, ghostMask);
        foreach (var col in ghosts)
        {
            if (!col) continue;
            if (!col.TryGetComponent<EnemyGhostController>(out var ghost) || !ghost.isCapturable) continue;

            Vector2 to = (Vector2)col.bounds.center - origin;
            float dist = to.magnitude; if (dist <= 0.001f) continue;
            if (Vector2.Angle(aim, to / dist) > coneAngleDeg * 0.5f) continue;
            if (Physics2D.Raycast(origin, to / dist, dist, obstacleMask)) continue;

            var rb2d = col.attachedRigidbody;
            if (rb2d)
            {
                Vector2 dir = -(to / dist);
                float falloff = Mathf.Clamp01(1f - dist / maxRange);
                rb2d.AddForce(dir * (pullForce * (0.5f + 0.5f * falloff)), ForceMode2D.Force);
            }
            else
            {
                Vector2 dir = (origin - (Vector2)col.transform.position).normalized;
                col.transform.position += (Vector3)(dir * pickupMoveSpeed * Time.fixedDeltaTime);
            }

            // >>> Capture when close enough to the suction origin
            if (dist <= collectRadius)
            {
                // Prefer calling a method on the ghost so you can play VFX/score, etc.
                if (ghost) ghost.Capture(); else Destroy(col.gameObject);
                continue; // this collider is gone now
            }

            if (dist < bestDist) { bestDist = dist; bestPoint = (Vector2)col.bounds.center; }
        }

        // ---- Pickups (pull-only) ----
        var items = Physics2D.OverlapCircleAll(origin, maxRange, pickupMask);
        foreach (var col in items)
        {
            if (!col) continue;

            Vector2 to = (Vector2)col.bounds.center - origin;
            float dist = to.magnitude; if (dist <= 0.001f) continue;
            if (Vector2.Angle(aim, to / dist) > coneAngleDeg * 0.5f) continue;
            if (Physics2D.Raycast(origin, to / dist, dist, obstacleMask)) continue;

            var rb2d = col.attachedRigidbody;
            if (rb2d)
            {
                Vector2 dir = -(to / dist);
                float falloff = Mathf.Clamp01(1f - dist / maxRange);
                rb2d.AddForce(dir * (pullForce * (0.5f + 0.5f * falloff)), ForceMode2D.Force);
            }
            else
            {
                Vector2 dir = (origin - (Vector2)col.transform.position).normalized;
                col.transform.position += (Vector3)(dir * pickupMoveSpeed * Time.fixedDeltaTime);
            }

            if (dist < bestDist) { bestDist = dist; bestPoint = (Vector2)col.bounds.center; }
        }

        // ---- Beam shortening works for either ghosts or pickups ----
        if (beamVFX && bestDist < Mathf.Infinity)
            UpdateBeamPose(bestDist, bestPoint);
    }


    private void UpdateBeamPose(float length, Vector2? toPoint = null)
    {
        Vector2 origin = suctionOrigin.position;
        Vector2 aim = playerController ? playerController.GetAimDir() : Vector2.right;

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
        Vector2 aim = Application.isPlaying && playerController ? playerController.GetAimDir()
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
