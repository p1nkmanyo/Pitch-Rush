using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace PitchRush
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("UI Elements")]
        public TMP_Text scoreText;
        public TMP_Text coinsText;
        public GameObject gameOverPanel;

        [Header("Player Spawning")]
        public GameObject[] playerSkinPrefabs; // Expected names: "Normal", "HeavyIron", "LightPingPong"
        public Vector3 playerSpawnPosition = new Vector3(0, 1f, 0); // Slight elevation to avoid clipping

        [Header("Progression Settings")]
        public float baseSpeed = 10f;
        public float maxSpeed = 30f;
        public float accelerationRate = 0.5f;

        [Header("References")]
        public Transform playerTransform; // Dynamically assigned now
        public CameraFollow cameraFollow;
        public TrackManager trackManager;

        public float CurrentSpeed { get; private set; }
        public bool IsGameActive => !isGameOver;

        private int currentCoins = 0;
        private float score = 0f;
        private bool isGameOver = false;
        private float targetProgressionSpeed;
        private Vector3 coinsTextOriginalScale;
        private Vector3 scoreTextOriginalScale;

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
            targetProgressionSpeed = baseSpeed;
            CurrentSpeed = baseSpeed;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (coinsText != null) coinsTextOriginalScale = coinsText.transform.localScale;
            if (scoreText != null) scoreTextOriginalScale = scoreText.transform.localScale;

            SpawnPlayer();
            UpdateUI();
        }

        private void SpawnPlayer()
        {
            string selectedSkin = PlayerPrefs.GetString("SelectedSkin", "Normal");
            GameObject prefabToSpawn = null;

            if (playerSkinPrefabs != null && playerSkinPrefabs.Length > 0)
            {
                foreach (GameObject prefab in playerSkinPrefabs)
                {
                    if (prefab != null && prefab.name == selectedSkin)
                    {
                        prefabToSpawn = prefab;
                        break;
                    }
                }

                // Fallback to first prefab if not found
                if (prefabToSpawn == null)
                {
                    prefabToSpawn = playerSkinPrefabs[0];
                }
            }

            if (prefabToSpawn != null)
            {
                GameObject playerObj = Instantiate(prefabToSpawn, playerSpawnPosition, Quaternion.identity);
                playerTransform = playerObj.transform;

                if (cameraFollow == null) cameraFollow = FindFirstObjectByType<CameraFollow>();
                if (trackManager == null) trackManager = FindFirstObjectByType<TrackManager>();

                if (cameraFollow != null) cameraFollow.target = playerTransform;
                if (trackManager != null) trackManager.playerTransform = playerTransform;
            }
            else
            {
                Debug.LogWarning("No player skin prefabs assigned in GameManager!");
            }
        }

        private void Update()
        {
            if (isGameOver || playerTransform == null) return;

            // Speed progression
            targetProgressionSpeed = Mathf.MoveTowards(targetProgressionSpeed, maxSpeed, accelerationRate * Time.deltaTime);

            float speedMultiplier = 1f;
            BuffManager buffManager = playerTransform.GetComponent<BuffManager>();
            if (buffManager != null && buffManager.IsBuffActive(BuffType.SpeedBoost))
            {
                speedMultiplier = 2f; // Upto 2x speed for Speed Boost
            }

            CurrentSpeed = targetProgressionSpeed * speedMultiplier;

            // Score increases based on distance traveled (Z axis)
            // Assuming player starts at Z=0
            if (playerTransform.position.z > score)
            {
                score = playerTransform.position.z;
                UpdateUI();
                
                // Small pulse for every 10 points threshold
                if (scoreText != null && Mathf.FloorToInt(score) % 10 == 0)
                {
                    scoreText.transform.localScale = scoreTextOriginalScale * 1.15f;
                }
            }

            // Smoothly restore UI text scales for juiciness
            if (coinsText != null && coinsTextOriginalScale != Vector3.zero)
            {
                coinsText.transform.localScale = Vector3.MoveTowards(coinsText.transform.localScale, coinsTextOriginalScale, Time.deltaTime * 3f);
            }
            if (scoreText != null && scoreTextOriginalScale != Vector3.zero)
            {
                scoreText.transform.localScale = Vector3.MoveTowards(scoreText.transform.localScale, scoreTextOriginalScale, Time.deltaTime * 3f);
            }
        }

        public void AddCoin(int amount)
        {
            if (isGameOver) return;
            currentCoins += amount;
            UpdateUI();

            // Punch the scale of the coin UI text for visual reward feedback
            if (coinsText != null && coinsTextOriginalScale != Vector3.zero)
            {
                coinsText.transform.localScale = coinsTextOriginalScale * 1.35f;
            }
        }

        public void GameOver()
        {
            if (isGameOver) return;

            if (playerTransform != null)
            {
                BuffManager buffManager = playerTransform.GetComponent<BuffManager>();
                if (buffManager != null)
                {
                    if (buffManager.IsInvincible())
                    {
                        return; // Ignore damage entirely during boost/rewind
                    }

                    if (buffManager.IsBuffActive(BuffType.Shield))
                    {
                        buffManager.ConsumeShield();
                        return; // Shield absorbed hit
                    }

                    if (buffManager.IsBuffActive(BuffType.MercuryFlow))
                    {
                        PlayerController controller = playerTransform.GetComponent<PlayerController>();
                        if (controller != null && controller.ShiftToSurvivingClone())
                        {
                            return; // Saved by shifting to a clone
                        }
                    }

                    if (buffManager.TriggerChronoRewind())
                    {
                        return; // Rewind activated, save player
                    }
                }
            }

            isGameOver = true;
            Time.timeScale = 0f; // Pause the game

            // Save coins
            int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
            totalCoins += currentCoins;
            PlayerPrefs.SetInt("TotalCoins", totalCoins);
            PlayerPrefs.Save();

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            Debug.Log($"Game Over! Score: {Mathf.FloorToInt(score)}, Coins: {currentCoins}. Total Coins: {totalCoins}");
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
