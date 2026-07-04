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

        [Header("Player Spawning")]
        public GameObject[] playerSkinPrefabs; // Expected names: "Normal", "HeavyIron", "LightPingPong"
        public Vector3 playerSpawnPosition = new Vector3(0, 1f, 0); // Slight elevation to avoid clipping

        [Header("References")]
        public Transform playerTransform; // Dynamically assigned now
        public CameraFollow cameraFollow;
        public TrackManager trackManager;

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
