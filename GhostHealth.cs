using UnityEngine;

public class GhostHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;

    private int hp;
    private EnemyGhostController enemyGhostController;
    private DamagePlayer damagePlayer;

    private void Awake() 
    { 
        hp = maxHealth;
        enemyGhostController = GetComponent<EnemyGhostController>();
        damagePlayer = GetComponent<DamagePlayer>();
    }

    public void ApplyTorchDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0)
        {
            damagePlayer.damageAmount = 0;
            Stun();
        }
    }

    private void Stun()
    {
        if (enemyGhostController != null)
        {
            enemyGhostController.SetCapturable(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
