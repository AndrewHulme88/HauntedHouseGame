using UnityEngine;

public class EnemySuck : MonoBehaviour
{
    [Header("States")]
    [SerializeField] private bool capturable = false;
    [SerializeField] private float captureProgressMax = 3f;

    private float captureProgress;

    public bool IsCapturable => capturable;

    public void SetCapturable(bool value, float resetProgress = 0f)
    {
        capturable = value;
        if (!value) captureProgress = resetProgress;
    }

    public void ApplyVacuumProgress(float delta)
    {
        if (!capturable) return;
        captureProgress += delta;
        if (captureProgress >= captureProgressMax) Capture();
    }

    private void Capture()
    {
        // Replace with “go to inventory”, particles, etc.
        Destroy(gameObject);
    }
}
