using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace PitchRush
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("UI Elements")]
        public Text scoreText;
        public Text coinsText;
        public GameObject gameOverPanel;

        [Header("References")]
        public Transform playerTransform;

        private int currentCoins = 0;
        private float score = 0f;
        private bool isGameOver = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            Time.timeScale = 1f; // Ensure time is running
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
            UpdateUI();
        }

        private void Update()
        {
            if (isGameOver || playerTransform == null) return;

            // Score increases based on distance traveled (Z axis)
            // Assuming player starts at Z=0
            if (playerTransform.position.z > score)
            {
                score = playerTransform.position.z;
                UpdateUI();
            }
        }

        public void AddCoin(int amount)
        {
            if (isGameOver) return;
            currentCoins += amount;
            UpdateUI();
        }

        public void GameOver()
        {
            if (isGameOver) return;

            isGameOver = true;
            Time.timeScale = 0f; // Pause the game

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            Debug.Log($"Game Over! Score: {Mathf.FloorToInt(score)}, Coins: {currentCoins}");
        }

        public void RestartGame()
        {
            // Reload the current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void UpdateUI()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {Mathf.FloorToInt(score)}";
            }

            if (coinsText != null)
            {
                coinsText.text = $"Coins: {currentCoins}";
            }
        }
    }
}
