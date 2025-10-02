using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerTorchController : MonoBehaviour
{
    [Header("Input (assign in Inspector)")]
    [SerializeField] private InputActionReference torchAction;   // Button (e.g., E / South)

    [Header("Flash Settings")]
    [SerializeField] private Transform flashOrigin;              // empty child near hands/chest
    [SerializeField] private Vector2 hitboxSize = new Vector2(2.0f, 1.2f);
    [SerializeField] private float flashDuration = 1.0f;         // active time (seconds)
    [SerializeField] private float cooldown = 1.2f;              // seconds between uses
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask ghostMask;                // set to your "Ghost" layer(s)

    // Optional simple visual toggle (assign a sprite or light to briefly show the flash)
    [SerializeField] private GameObject flashVFX;                // optional

    private bool isFlashing;
    private float nextReadyTime;

    private void OnEnable() { torchAction.action.Enable(); }
    private void OnDisable() { torchAction.action.Disable(); }

    private void Update()
    {
        if (torchAction.action.WasPressedThisFrame() && Time.time >= nextReadyTime && !isFlashing)
            StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        nextReadyTime = Time.time + cooldown;

        if (flashVFX) flashVFX.SetActive(true);

        // One-shot damage application at start of flash (simple & arcadey).
        DamageInFront();

        // If you want lingering damage during the whole second, uncomment:
        // float tEnd = Time.time + flashDuration;
        // while (Time.time < tEnd) { DamageInFront(); yield return null; }

        yield return new WaitForSeconds(flashDuration);

        if (flashVFX) flashVFX.SetActive(false);
        isFlashing = false;
    }

    private void DamageInFront()
    {
        if (!flashOrigin) flashOrigin = transform;

        // Face from localScale.x sign (matches your flip logic)
        float dir = transform.localScale.x >= 0f ? 1f : -1f;

        // Center the box slightly in front of the player
        Vector2 center = (Vector2)flashOrigin.position + new Vector2((hitboxSize.x * 0.5f) * dir, 0f);

        // OverlapBox (axis-aligned) – simple and cheap
        var hits = Physics2D.OverlapBoxAll(center, hitboxSize, 0f, ghostMask);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].TryGetComponent<GhostHealth>(out var gh))
                gh.ApplyTorchDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!flashOrigin) return;
        float dir = transform.localScale.x >= 0f ? 1f : -1f;
        Vector2 center = (Vector2)flashOrigin.position + new Vector2((hitboxSize.x * 0.5f) * dir, 0f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, hitboxSize);
    }
}
