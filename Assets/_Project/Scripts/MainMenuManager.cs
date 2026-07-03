using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace PitchRush
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI coinsText;
        public GameObject shopPanel;
        public GameObject mainPanel;

        private void Start()
        {
            UpdateCoinsDisplay();
            CloseShop(); // Ensure shop is closed initially
        }

        public void PlayGame()
        {
            // Assuming Gameplay scene is at index 1 or named "Gameplay"
            SceneManager.LoadScene("Gameplay");
        }

        public void OpenShop()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (shopPanel != null) shopPanel.SetActive(true);
        }

        public void CloseShop()
        {
            if (shopPanel != null) shopPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
            UpdateCoinsDisplay(); // Update coins in case purchases were made
        }

        public void UpdateCoinsDisplay()
        {
            if (coinsText != null)
            {
                int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
                coinsText.text = "Coins: " + totalCoins.ToString();
            }
        }
    }
}
