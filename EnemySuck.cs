using System;
using UnityEngine;

public class EnemySuck : MonoBehaviour
{
    [Header("States")]
    [SerializeField] private bool capturable = false;
    [SerializeField] private float captureProgressMax = 3f;
    public bool IsCapturable => capturable;

    public event Action<bool> OnCapturableChanged;
    private float captureProgress;


    public void SetCapturable(bool value, float resetProgress = 0f)
    {
        if (capturable == value) return;
        capturable = value;
        if (!value) captureProgress = resetProgress;
        OnCapturableChanged?.Invoke(capturable);
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
