using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text healthText;

    public void UpdateCoinDisplay(int coinCount)
    {
        coinText.text = "x" + coinCount;
    }

    public void UpdateHealthDisplay(int health)
    {
        healthText.text = "x" + health;
    }
}
