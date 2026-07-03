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
        public Text feverText; // Display fever status/multiplier

        [Header("References")]
        public Transform playerTransform;

        [Header("Fever Mode Settings")]
        public int coinsForFever = 10;
        public float feverDuration = 5f;

        private int feverCoinsCount = 0;
        private bool isFeverMode = false;
        private float feverTimer = 0f;

        private int currentCoins = 0;
        private float score = 0f;
        private bool isGameOver = false;

        // PowerUp timer logic
        private PlayerController activePlayerController;
        private float powerUpTimer = 0f;
        private bool isPowerUpActive = false;

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
            if (playerTransform.position.z > score)
            {
                score = playerTransform.position.z;
                UpdateUI();
            }

            HandlePowerUpTimer();
            HandleFeverTimer();
        }

        private void HandlePowerUpTimer()
        {
            if (isPowerUpActive && activePlayerController != null && !isFeverMode)
            {
                powerUpTimer -= Time.deltaTime;
                if (powerUpTimer <= 0)
                {
                    isPowerUpActive = false;
                    activePlayerController.ChangeState(BallState.Normal);
                }
            }
        }

        private void HandleFeverTimer()
        {
            if (isFeverMode)
            {
                feverTimer -= Time.deltaTime;
                if (feverText != null) feverText.text = $"PITCH RUSH! {(int)feverTimer}s";

                if (feverTimer <= 0)
                {
                    isFeverMode = false;
                    feverCoinsCount = 0;
                    if (feverText != null) feverText.text = "";
                    if (activePlayerController != null)
                    {
                        activePlayerController.ChangeState(BallState.Normal);
                    }
                }
            }
        }

        public void StartPowerUpTimer(PlayerController player, float duration)
        {
            // Pitch Rush mode overrides normal powerups
            if (isFeverMode) return;

            activePlayerController = player;
            powerUpTimer = duration;
            isPowerUpActive = true;
        }

        public void AddCoin(int amount)
        {
            if (isGameOver) return;

            int multiplier = isFeverMode ? 2 : 1;
            currentCoins += (amount * multiplier);

            if (!isFeverMode)
            {
                feverCoinsCount++;
                if (feverCoinsCount >= coinsForFever)
                {
                    ActivateFeverMode();
                }
            }

            UpdateUI();
        }

        private void ActivateFeverMode()
        {
            isFeverMode = true;
            feverTimer = feverDuration;
            isPowerUpActive = false; // Cancel normal powerups

            if (playerTransform != null)
            {
                activePlayerController = playerTransform.GetComponent<PlayerController>();
                if (activePlayerController != null)
                {
                    // HeavyIron state makes the ball invincible and smash things!
                    activePlayerController.ChangeState(BallState.HeavyIron);
                }
            }
        }

        public void GameOver()
        {
            // In Pitch Rush (Fever) mode or Heavy Iron mode, you shouldn't die normally,
            // but Obstacle.cs handles the logic for HeavyIron breaking walls.
            // Just in case it gets here while invincible:
            if (isFeverMode) return;

            if (isGameOver) return;

            isGameOver = true;
            Time.timeScale = 0f; // Pause the game

            // Save coins for shop
            ShopManager.Instance?.AddCoins(currentCoins);

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
