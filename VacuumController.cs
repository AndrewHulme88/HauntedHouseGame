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
    [SerializeField] private LayerMask obstacleMask; // walls/geometry blocking LOS
    [SerializeField] private LayerMask pickupMask;
    [SerializeField] private GameObject beamVFX;
    [SerializeField] private float collectRadius = 0.6f;
    [SerializeField] private float pickupMoveSpeed = 12f;

    private PlayerController playerController;
    private Vector3 beamBaseScale;
    private bool vacuumActive;

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
        vacuumActive = false;
        if (beamVFX && beamVFX.activeSelf) beamVFX.SetActive(false);
    }

    private void Update()
    {
        if (!beamVFX) return;

        if (!vacuumActive)
        {
            if (beamVFX.activeSelf) beamVFX.SetActive(false);
            return;
        }

        if (!beamVFX.activeSelf) beamVFX.SetActive(true);
    }

    private void FixedUpdate()
    {
        bool torchActive = playerController && playerController.isUsingTorch;
        bool pressedRaw = vacuumAction.action.IsPressed();
        bool pressed = pressedRaw && !torchActive;
        vacuumActive = pressed;

        if (!pressed)
        {
            // ensure vacuum is OFF
            if (playerController.isUsingVacuum)
                playerController.isUsingVacuum = false;

            playerController.isUsingWeapon = playerController.isUsingTorch || playerController.isUsingVacuum;

            if (beamVFX && beamVFX.activeSelf) beamVFX.SetActive(false);
            return; 
        }

        // activate vacuum
        playerController.isUsingVacuum = true;
        playerController.isUsingWeapon = playerController.isUsingTorch || playerController.isUsingVacuum;

        Vector2 origin = suctionOrigin.position;
        Vector2 aimDirection = playerController ? playerController.GetAimDir() : Vector2.right;

        float nearestDistance = maxRange;
        Vector2 nearestPoint = origin + aimDirection.normalized * maxRange;

        var ghosts = Physics2D.OverlapCircleAll(origin, maxRange, ghostMask);
        foreach (var col in ghosts)
        {
            if (!col) continue;
            if (!col.TryGetComponent<EnemyGhostController>(out var ghost) || !ghost.isCapturable) continue;

            Vector2 vectorToTarget = (Vector2)col.bounds.center - origin;
            float distance = vectorToTarget.magnitude; if (distance <= 0.001f) continue;
            if (Vector2.Angle(aimDirection, vectorToTarget / distance) > coneAngleDeg * 0.5f) continue;
            if (Physics2D.Raycast(origin, vectorToTarget / distance, distance, obstacleMask)) continue;

            var targetRb = col.attachedRigidbody;
            if (targetRb)
            {
                Vector2 direction = -(vectorToTarget / distance);
                float falloff = Mathf.Clamp01(1f - distance / maxRange);
                targetRb.AddForce(direction * (pullForce * (0.5f + 0.5f * falloff)), ForceMode2D.Force);
            }
            else
            {
                Vector2 dir = (origin - (Vector2)col.transform.position).normalized;
                col.transform.position += (Vector3)(dir * pickupMoveSpeed * Time.fixedDeltaTime);
            }

            if (distance <= collectRadius)
            {
                if (ghost) ghost.Capture(); else Destroy(col.gameObject);
                continue;
            }

            if (distance < nearestDistance) { nearestDistance = distance; nearestPoint = (Vector2)col.bounds.center; }
        }

        var items = Physics2D.OverlapCircleAll(origin, maxRange, pickupMask);
        foreach (var col in items)
        {
            if (!col) continue;

            Vector2 vectorToTarget = (Vector2)col.bounds.center - origin;
            float distance = vectorToTarget.magnitude; if (distance <= 0.001f) continue;
            if (Vector2.Angle(aimDirection, vectorToTarget / distance) > coneAngleDeg * 0.5f) continue;
            if (Physics2D.Raycast(origin, vectorToTarget / distance, distance, obstacleMask)) continue;

            var targetRb = col.attachedRigidbody;
            if (targetRb)
            {
                Vector2 direction = -(vectorToTarget / distance);
                float falloff = Mathf.Clamp01(1f - distance / maxRange);
                targetRb.AddForce(direction * (pullForce * (0.5f + 0.5f * falloff)), ForceMode2D.Force);
            }
            else
            {
                Vector2 direction = (origin - (Vector2)col.transform.position).normalized;
                col.transform.position += (Vector3)(direction * pickupMoveSpeed * Time.fixedDeltaTime);
            }

            if (distance < nearestDistance) { nearestDistance = distance; nearestPoint = (Vector2)col.bounds.center; }
        }

        if (vacuumActive && beamVFX)
            UpdateBeamPose(nearestDistance, nearestPoint);
    }


    private void UpdateBeamPose(float length, Vector2? toPoint = null)
    {
        Vector2 origin = suctionOrigin.position;
        Vector2 aim = playerController ? playerController.GetAimDir() : Vector2.right;

        Vector2 endPoint = toPoint.HasValue ? toPoint.Value : origin + aim.normalized * length;
        Vector2 midPoint = (origin + endPoint) * 0.5f;
        Vector2 aimDirection = (endPoint - origin);
        float angleDeg = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        beamVFX.transform.position = midPoint;
        beamVFX.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

        // Assumes the beam sprite is 1 unit long on X; scale X to length
        beamVFX.transform.localScale = new Vector3(length, beamBaseScale.y, beamBaseScale.z);
    }

    private void OnDrawGizmosSelected()
    {
        if (!suctionOrigin) return;
        var originPosition = (Vector2)suctionOrigin.position;
        Vector2 aimDirection = Application.isPlaying && playerController ? playerController.GetAimDir()
                                                      : new Vector2(transform.localScale.x >= 0f ? 1f : -1f, 0f);

        // Draw cone
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
        float halfConeAngleDegrees = coneAngleDeg * 0.5f;
        Quaternion quaternionLeft = Quaternion.Euler(0, 0, halfConeAngleDegrees);
        Quaternion quaternionRight = Quaternion.Euler(0, 0, -halfConeAngleDegrees);
        Vector2 leftEdgeDirection = (Vector2)(quaternionLeft * (Vector3)aimDirection).normalized * maxRange;
        Vector2 rightEdgeDirection = (Vector2)(quaternionRight * (Vector3)aimDirection).normalized * maxRange;
        Gizmos.DrawLine(originPosition, originPosition + leftEdgeDirection);
        Gizmos.DrawLine(originPosition, originPosition + rightEdgeDirection);
        Gizmos.DrawWireSphere(originPosition, 0.07f);
    }
}
