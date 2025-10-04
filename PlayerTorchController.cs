using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTorchController : MonoBehaviour
{
    [Header("Input (assign in Inspector)")]
    [SerializeField] private InputActionReference torchAction;

    [Header("Torch Channel Settings")]
    [SerializeField] private Transform flashOrigin;
    [SerializeField] private Vector2 hitboxSize = new Vector2(2.0f, 1.2f);
    [SerializeField] private LayerMask ghostMask;
    [SerializeField] private GameObject flashVFX; // sprite/light

    [Header("Damage & Timing")]
    [SerializeField] private int damagePerTick = 1;
    [SerializeField] private float damageTickInterval = 1.0f; // once per second

    [Header("Energy")]
    [SerializeField] private float maxEnergy = 3f;
    [SerializeField] private float drainPerSecond = 1f;
    [SerializeField] private float rechargePerSecond = 0.5f;
    [SerializeField] private float minEnergyToStart = 1f;
    [SerializeField] private float rechargeDelayAfterRelease = 0.6f;

    private PlayerController playerController;
    private UIController uiController;
    [SerializeField] private float energy;
    private float tickTimer;
    private bool isChanneling;
    private Vector3 vfxBaseScale;
    private bool outOfEnergy;                
    private float rechargeReadyTime = 0f;    
    private bool prevPressed = false;
    private float previousEnergyNormalized = -1f;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        if (!flashOrigin) flashOrigin = transform;
        if (flashVFX) vfxBaseScale = flashVFX.transform.localScale;
        energy = maxEnergy;
    }

    private void Start()
    {
        uiController = FindFirstObjectByType<UIController>();
        if (uiController == null)
        {
            Debug.LogError("UIController not found in the scene.");
        }
    }

    private void OnEnable() { torchAction.action.Enable(); }
    private void OnDisable() { torchAction.action.Disable(); }

    private void Update()
    {
        float dt = Time.deltaTime;
        bool pressedRaw = torchAction.action.IsPressed();
        bool pressed = pressedRaw && !playerController.isUsingVacuum;

        if (isChanneling)
        {
            // drain first
            energy = Mathf.Max(0f, energy - drainPerSecond * dt);

            // stop if button released or energy empty
            if (!pressed || energy <= 0f)
            {
                bool depleted = energy <= 0f;
                StopChannel();

                if (depleted)
                {
                    outOfEnergy = true;
                }
                else
                {
                    outOfEnergy = false;
                }
            }
            else
            {
                if (flashVFX) { UpdateVFXPose(); if (!flashVFX.activeSelf) flashVFX.SetActive(true); }
                tickTimer += dt;
                if (tickTimer >= damageTickInterval)
                {
                    tickTimer -= damageTickInterval;
                    DamageInAimDirection();
                }
            }
        }
        else
        {
            if (outOfEnergy)
            {
                if (prevPressed && !pressed)
                    rechargeReadyTime = Time.time + rechargeDelayAfterRelease;

                bool canRechargeNow = !pressed && Time.time >= rechargeReadyTime;
                if (canRechargeNow)
                {
                    energy = Mathf.Min(maxEnergy, energy + rechargePerSecond * dt);
                    if (energy > 0f) outOfEnergy = false;
                }
            }
            else
            {
                if (!pressed && energy < maxEnergy)
                    energy = Mathf.Min(maxEnergy, energy + rechargePerSecond * dt);
            }

            bool anyWeaponActive = playerController != null && playerController.isUsingWeapon;
            if (pressed && !anyWeaponActive && !outOfEnergy && energy >= minEnergyToStart)
                StartChannel();


            if (flashVFX && flashVFX.activeSelf) flashVFX.SetActive(false);
        }

        prevPressed = pressed;

        float currentEnergyNormalized = maxEnergy > 0f ? energy / maxEnergy : 0f;
        if (!Mathf.Approximately(currentEnergyNormalized, previousEnergyNormalized))
        {
            uiController?.UpdateTorchDisplay(energy, maxEnergy);
            previousEnergyNormalized = currentEnergyNormalized;
        }
    }

    private void StartChannel()
    {
        // refuse if vacuum is active
        if (playerController.isUsingVacuum) return;

        isChanneling = true;

        playerController.isUsingTorch = true;
        playerController.isUsingWeapon = playerController.isUsingTorch || playerController.isUsingVacuum;

        tickTimer = 0f;
        if (flashVFX) { UpdateVFXPose(); flashVFX.SetActive(true); }
    }

    private void StopChannel()
    {
        isChanneling = false;

        playerController.isUsingTorch = false;
        playerController.isUsingWeapon = playerController.isUsingTorch || playerController.isUsingVacuum;

        tickTimer = 0f;
        if (flashVFX) flashVFX.SetActive(false);
    }

    void DamageInAimDirection()
    {
        ComputeFlashGeometry(out var center, out var size, out _);
        var hits = Physics2D.OverlapBoxAll(center, size, 0f, ghostMask);
        for (int i = 0; i < hits.Length; i++)
            if (hits[i].TryGetComponent<GhostHealth>(out var gh))
                gh.ApplyTorchDamage(damagePerTick);
    }

    void UpdateVFXPose()
    {
        if (!flashVFX) return;
        ComputeFlashGeometry(out var center, out var size, out bool aimUp);

        flashVFX.transform.position = center;
        flashVFX.transform.rotation = Quaternion.Euler(0f, 0f, aimUp ? 90f : 0f);

        if (!aimUp)
        {
            float face = transform.localScale.x >= 0f ? 1f : -1f;
            flashVFX.transform.localScale = new Vector3(
                Mathf.Abs(vfxBaseScale.x) * face,
                vfxBaseScale.y,
                vfxBaseScale.z
            );
        }
        else flashVFX.transform.localScale = vfxBaseScale;
    }

    void ComputeFlashGeometry(out Vector2 center, out Vector2 size, out bool aimUp)
    {
        Vector2 dir = playerController ? playerController.GetAimDir() : Vector2.right;
        aimUp = dir.y > 0.5f;

        size = aimUp ? new Vector2(hitboxSize.y, hitboxSize.x) : hitboxSize;

        center = (Vector2)flashOrigin.position + new Vector2(
            dir.x * (size.x * 0.5f),
            dir.y * (size.y * 0.5f)
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (!flashOrigin) return;
        Vector2 center, size; bool aimUp;
        if (Application.isPlaying) ComputeFlashGeometry(out center, out size, out aimUp);
        else
        {
            bool faceRight = transform.localScale.x >= 0f;
            size = hitboxSize;
            center = (Vector2)flashOrigin.position + new Vector2(size.x * 0.5f * (faceRight ? 1f : -1f), 0f);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
}
