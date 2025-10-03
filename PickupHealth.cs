using UnityEngine;

public class PickupHealth : MonoBehaviour
{
    [SerializeField] private int healthAmount = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            player.Heal(healthAmount);
            Destroy(gameObject);
        }
    }
}
