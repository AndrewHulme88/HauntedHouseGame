using UnityEngine;

public class GhostHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    private int hp;
    private EnemySuck enemySuck;

    private void Awake() 
    { 
        hp = maxHealth;
        enemySuck = GetComponent<EnemySuck>();
    }

    public void ApplyTorchDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0) Stun();
    }

    private void Stun()
    {
        if (enemySuck != null)
        {
            enemySuck.SetCapturable(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
