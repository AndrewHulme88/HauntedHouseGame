using UnityEngine;

public class PickupCoin : MonoBehaviour
{
    [SerializeField] private int coinValue = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerInventory playerInventory = collision.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.AddCoins(coinValue);
            }
            Destroy(gameObject);
        }
    }
}
