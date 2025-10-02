using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerTorchController : MonoBehaviour
{
    [Header("Input (assign in Inspector)")]
    [SerializeField] private InputActionReference torchAction; // Button
    [SerializeField] private InputActionReference aimAction;   // Vector2

    [Header("Flash Settings")]
    [SerializeField] private Transform flashOrigin;
    [SerializeField] private Vector2 hitboxSize = new Vector2(2.0f, 1.2f);
    [SerializeField] private float flashDuration = 1.0f;
    [SerializeField] private float cooldown = 1.2f;
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask ghostMask;
    [SerializeField] private GameObject flashVFX; // optional (sprite/light)

    private bool isFlashing;
    private float nextReadyTime;
    private Vector3 vfxBaseScale;

    private void Awake()
    {
        if (!flashOrigin) flashOrigin = transform;
        if (flashVFX) vfxBaseScale = flashVFX.transform.localScale;
    }

    private void OnEnable() { torchAction.action.Enable(); aimAction?.action.Enable(); }
    private void OnDisable() { torchAction.action.Disable(); aimAction?.action.Disable(); }

    private void Update()
    {
        if (torchAction.action.WasPressedThisFrame() && Time.time >= nextReadyTime && !isFlashing)
            StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        nextReadyTime = Time.time + cooldown;

        if (flashVFX)
        {
            UpdateVFXPose();      // position/rotate/flip once at start
            flashVFX.SetActive(true);
        }

        DamageInAimDirection();   // one-shot hit

        yield return new WaitForSeconds(flashDuration);

        if (flashVFX) flashVFX.SetActive(false);
        isFlashing = false;
    }

    // -------- Core hit logic (now uses shared geometry) --------
    void DamageInAimDirection()
    {
        ComputeFlashGeometry(out var center, out var size, out _);
        var hits = Physics2D.OverlapBoxAll(center, size, 0f, ghostMask);
        for (int i = 0; i < hits.Length; i++)
            if (hits[i].TryGetComponent<GhostHealth>(out var gh))
                gh.ApplyTorchDamage(damage);
    }

    // -------- VFX pose (center/rotation/flip) --------
    void UpdateVFXPose()
    {
        if (!flashVFX) return;

        ComputeFlashGeometry(out var center, out var size, out bool aimUp);

        // Place the VFX at the center of the hitbox
        flashVFX.transform.position = center;

        // Rotate 0° (forward) or 90° (up)
        float angle = aimUp ? 90f : 0f;
        flashVFX.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Flip horizontally to match facing only when forward
        if (!aimUp)
        {
            float face = transform.localScale.x >= 0f ? 1f : -1f;
            flashVFX.transform.localScale = new Vector3(
                Mathf.Abs(vfxBaseScale.x) * face,
                vfxBaseScale.y,
                vfxBaseScale.z
            );
        }
        else
        {
            flashVFX.transform.localScale = vfxBaseScale;
        }
    }

    // -------- Shared geometry for gizmos/VFX/hit --------
    void ComputeFlashGeometry(out Vector2 center, out Vector2 size, out bool aimUp)
    {
        Vector2 dir = GetAimDir();          // (1,0) or (0,1)
        aimUp = dir.y > 0.5f;

        size = aimUp ? new Vector2(hitboxSize.y, hitboxSize.x) : hitboxSize;

        center = (Vector2)flashOrigin.position + new Vector2(
            dir.x * (size.x * 0.5f),
            dir.y * (size.y * 0.5f)
        );
    }

    Vector2 GetAimDir()
    {
        Vector2 forward = new Vector2(transform.localScale.x >= 0f ? 1f : -1f, 0f);
        if (aimAction == null) return forward;

        Vector2 aim = aimAction.action.ReadValue<Vector2>();
        if (aim.sqrMagnitude < 0.01f) return forward;

        // Snap up if Y dominates and is positive
        if (Mathf.Abs(aim.y) > Mathf.Abs(aim.x) && aim.y > 0f) return Vector2.up;
        return forward;
    }

    private void OnDrawGizmosSelected()
    {
        if (!flashOrigin) return;
        // In edit mode, default to forward; in play, use live aim
        bool playing = Application.isPlaying;
        Vector2 center, size; bool aimUp;

        if (playing) ComputeFlashGeometry(out center, out size, out aimUp);
        else
        {
            // forward-only preview when not playing
            bool faceRight = transform.localScale.x >= 0f;
            size = hitboxSize;
            center = (Vector2)flashOrigin.position + new Vector2(size.x * 0.5f * (faceRight ? 1f : -1f), 0f);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
}
