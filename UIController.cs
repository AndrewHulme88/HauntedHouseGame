using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;

    public void UpdateCoinDisplay(int coinCount)
    {
        coinText.text = "x" + coinCount;
    }
}
