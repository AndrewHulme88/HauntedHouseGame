using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Slider torchSlider;

    public void UpdateCoinDisplay(int coinCount)
    {
        coinText.text = "x" + coinCount;
    }

    public void UpdateHealthDisplay(int health)
    {
        healthText.text = "x" + health;
    }

    public void UpdateTorchDisplay(float energy, float maxEnergy)
    {
        if (torchSlider)
        {
            torchSlider.value = energy / maxEnergy;
        }
    }
}
