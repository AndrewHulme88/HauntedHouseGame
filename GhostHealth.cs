using UnityEngine;

public class GhostHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    private int hp;

    private void Awake() { hp = maxHealth; }

    public void ApplyTorchDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0) Die();
    }

    private void Die()
    {
        // Replace with your ghost “capturable/stunned” state later
        Destroy(gameObject);
    }
}
