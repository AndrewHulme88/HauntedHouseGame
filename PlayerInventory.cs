using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int coins = 0;

    private UIController uiController;

    void Start()
    {
        uiController = FindFirstObjectByType<UIController>();
    }

    public void AddCoins(int amount)
    {
        coins += amount;

        if (uiController != null)
        {
            uiController.UpdateCoinDisplay(coins);
        }
    }
}
