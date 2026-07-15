using System.Collections.Generic;
using UnityEngine;

namespace PitchRush
{
    public class TrackSegment : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The transform located at the exact end of this track segment.")]
        public Transform endPoint;

        [Header("Buff Spawn Settings")]
        [Tooltip("List of buff prefabs that can spawn on the track (e.g. Magnet, Shield, SpeedBoost, ChronoRewind).")]
        public GameObject[] buffPrefabs;
        [Range(0f, 1f)]
        [Tooltip("Probability of replacing a coin slot with a buff (e.g., 0.05 = 5% chance per slot).")]
        public float buffSpawnChance = 0.05f;

        [Header("Obstacle Randomization")]
        [Tooltip("If enabled, obstacles will be randomly activated/deactivated and repositioned each time this segment spawns.")]
        public bool randomizeObstacles = true;
        [Range(0f, 1f)]
        [Tooltip("Chance that ANY obstacles appear on this segment (0 = never, 1 = always).")]
        public float obstacleSpawnChance = 0.75f;
        [Tooltip("Maximum random Z offset for obstacle positions (jitter).")]
        public float obstacleZJitter = 2f;

        [Header("Coin Randomization")]
        [Tooltip("If enabled, coins will be randomly repositioned across lanes each reset.")]
        public bool randomizeCoins = true;
        [Range(0f, 1f)]
        [Tooltip("Chance each individual coin appears (0 = never, 1 = always).")]
        public float coinVisibilityChance = 0.7f;

        private Collectible[] collectibles;
        private Obstacle[] obstacles;
        private List<GameObject> spawnedBuffs = new List<GameObject>();

        // Cached original local positions
        private Vector3[] obstacleOriginalLocalPositions;
        private Vector3[] coinOriginalLocalPositions;

        // Lane X positions for random coin placement
        private float[] laneXPositions = new float[] { -3f, 0f, 3f };

        private void Awake()
        {
            collectibles = GetComponentsInChildren<Collectible>(true);
            obstacles = GetComponentsInChildren<Obstacle>(true);

            // Cache original positions of all obstacles
            if (obstacles != null)
            {
                obstacleOriginalLocalPositions = new Vector3[obstacles.Length];
                for (int i = 0; i < obstacles.Length; i++)
                {
                    obstacleOriginalLocalPositions[i] = obstacles[i].transform.localPosition;
                }
            }

            // Cache original positions of all coins
            if (collectibles != null)
            {
                coinOriginalLocalPositions = new Vector3[collectibles.Length];
                for (int i = 0; i < collectibles.Length; i++)
                {
                    coinOriginalLocalPositions[i] = collectibles[i].transform.localPosition;
                }
            }
        }

        public void ResetSegment()
        {
            // Clear previously spawned buffs using ObjectPool (0 GC Alloc!)
            foreach (GameObject buff in spawnedBuffs)
            {
                if (buff != null && ObjectPool.Instance != null)
                {
                    ObjectPool.Instance.Despawn(buff);
                }
                else if (buff != null)
                {
                    Destroy(buff);
                }
            }
            spawnedBuffs.Clear();

            // Reset and randomize obstacles
            if (obstacles != null)
            {
                for (int i = 0; i < obstacles.Length; i++)
                {
                    if (obstacles[i] != null)
                    {
                        obstacles[i].ResetObstacle();
                        obstacles[i].gameObject.SetActive(true);
                        obstacles[i].transform.localPosition = obstacleOriginalLocalPositions[i];
                    }
                }

                if (randomizeObstacles)
                {
                    RandomizeObstacles();
                }
            }

            // Reset and randomize coins
            if (collectibles != null)
            {
                // First reset all coins to original positions and activate
                for (int i = 0; i < collectibles.Length; i++)
                {
                    if (collectibles[i] != null)
                    {
                        collectibles[i].transform.localPosition = coinOriginalLocalPositions[i];
                        collectibles[i].gameObject.SetActive(true);
                    }
                }

                // Then randomize positions and visibility
                if (randomizeCoins)
                {
                    RandomizeCoins();
                }

                // Finally, roll buff replacements on visible coins
                foreach (Collectible coin in collectibles)
                {
                    if (coin != null && coin.gameObject.activeSelf)
                    {
                        if (buffPrefabs != null && buffPrefabs.Length > 0 && Random.value < buffSpawnChance)
                        {
                            coin.gameObject.SetActive(false);

                            GameObject selectedBuffPrefab = buffPrefabs[Random.Range(0, buffPrefabs.Length)];
                            if (selectedBuffPrefab != null)
                            {
                                GameObject buffInstance = null;
                                if (ObjectPool.Instance != null)
                                {
                                    buffInstance = ObjectPool.Instance.Spawn(selectedBuffPrefab, coin.transform.position, Quaternion.identity);
                                    buffInstance.transform.SetParent(transform);
                                }
                                else
                                {
                                    buffInstance = Instantiate(selectedBuffPrefab, coin.transform.position, Quaternion.identity, transform);
                                }

                                if (buffInstance != null)
                                {
                                    spawnedBuffs.Add(buffInstance);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RandomizeCoins()
        {
            if (collectibles == null || collectibles.Length == 0) return;

            // Pick a random lane for this coin group (all coins in one line = satisfying to collect)
            int chosenLane = Random.Range(0, laneXPositions.Length);
            float laneX = laneXPositions[chosenLane];

            // Sometimes scatter coins across different lanes instead of one line
            bool scatterMode = Random.value > 0.6f; // 40% chance of scattered coins

            // Calculate segment length from endPoint
            float segmentLength = 20f;
            if (endPoint != null)
            {
                segmentLength = Mathf.Abs(endPoint.localPosition.z);
                if (segmentLength < 5f) segmentLength = 20f;
            }

            float coinSpacing = segmentLength / (collectibles.Length + 1);

            for (int i = 0; i < collectibles.Length; i++)
            {
                if (collectibles[i] == null) continue;

                // Roll visibility chance per coin
                if (Random.value > coinVisibilityChance)
                {
                    collectibles[i].gameObject.SetActive(false);
                    continue;
                }

                collectibles[i].gameObject.SetActive(true);

                // Calculate new position
                float coinX;
                if (scatterMode)
                {
                    coinX = laneXPositions[Random.Range(0, laneXPositions.Length)];
                }
                else
                {
                    coinX = laneX;
                }

                float coinZ = coinSpacing * (i + 1) + Random.Range(-0.5f, 0.5f);
                float coinY = coinOriginalLocalPositions[i].y; // Keep original height

                collectibles[i].transform.localPosition = new Vector3(coinX, coinY, coinZ);
            }
        }

        private void RandomizeObstacles()
        {
            if (obstacles == null || obstacles.Length == 0) return;

            // Roll: should ANY obstacles appear this time?
            if (Random.value > obstacleSpawnChance)
            {
                // No obstacles this run — disable all
                foreach (Obstacle obs in obstacles)
                {
                    if (obs != null) obs.gameObject.SetActive(false);
                }
                return;
            }

            // Choose a random layout pattern
            // Patterns ensure at least 1 lane is always open for the player to pass
            int patternCount = 7;
            int pattern = Random.Range(0, patternCount);

            // Map obstacles by name to lanes (Left, Center, Right)
            // Works with any number of obstacles, but optimized for 3 (Left, Center, Right)
            bool[] activeState = new bool[obstacles.Length];

            switch (pattern)
            {
                case 0: // Only left
                    SetLanePattern(activeState, true, false, false);
                    break;
                case 1: // Only center
                    SetLanePattern(activeState, false, true, false);
                    break;
                case 2: // Only right
                    SetLanePattern(activeState, false, false, true);
                    break;
                case 3: // Left + Center (right lane open)
                    SetLanePattern(activeState, true, true, false);
                    break;
                case 4: // Center + Right (left lane open)
                    SetLanePattern(activeState, false, true, true);
                    break;
                case 5: // Left + Right (center lane open)
                    SetLanePattern(activeState, true, false, true);
                    break;
                case 6: // All three (player must jump or use heavy iron to smash)
                    SetLanePattern(activeState, true, true, true);
                    break;
            }

            // Apply active states and jitter Z positions
            for (int i = 0; i < obstacles.Length; i++)
            {
                if (obstacles[i] == null) continue;

                obstacles[i].gameObject.SetActive(activeState[i]);

                if (activeState[i] && obstacleZJitter > 0f)
                {
                    // Add random Z offset to original position for variety
                    Vector3 jitteredPos = obstacleOriginalLocalPositions[i];
                    jitteredPos.z += Random.Range(-obstacleZJitter, obstacleZJitter);
                    obstacles[i].transform.localPosition = jitteredPos;
                }
            }
        }

        private void SetLanePattern(bool[] states, bool left, bool center, bool right)
        {
            // Assign based on obstacle index order (assumes Left=0, Center=1, Right=2)
            // Gracefully handles fewer or more obstacles
            for (int i = 0; i < states.Length; i++)
            {
                if (i == 0) states[i] = left;
                else if (i == 1) states[i] = center;
                else if (i == 2) states[i] = right;
                else states[i] = Random.value > 0.5f; // Extra obstacles: 50/50
            }
        }
    }
}