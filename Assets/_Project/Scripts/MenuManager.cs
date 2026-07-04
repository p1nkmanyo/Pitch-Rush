using UnityEngine;
using UnityEngine.SceneManagement;

namespace PitchRush
{
    public class MenuManager : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject mainMenuPanel;
        public GameObject shopPanel;

        [Header("Scene Settings")]
        public string gameplaySceneName = "Gameplay";

        private void Start()
        {
            // Убеждаемся, что при старте открыто только главное меню
            ShowMainMenu();
        }

        public void PlayGame()
        {
            // Загрузка игровой сцены
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void ShowShop()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (shopPanel != null) shopPanel.SetActive(true);
        }

        public void ShowMainMenu()
        {
            if (shopPanel != null) shopPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }

        public void QuitGame()
        {
            Debug.Log("Quit Game Requested");
            Application.Quit();
        }
    }
}