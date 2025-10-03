using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int coins = 0;

    void Start()
    {
        
    }

    public void AddCoins(int amount)
    {
        coins += amount;
    }
}
