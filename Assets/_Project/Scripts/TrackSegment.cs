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

        private Collectible[] collectibles;
        private Obstacle[] obstacles;
        private List<GameObject> spawnedBuffs = new List<GameObject>();

        // Cached original local positions for each obstacle (for resetting + jittering)
        private Vector3[] obstacleOriginalLocalPositions;

        private void Awake()
        {
            collectibles = GetComponentsInChildren<Collectible>(true);
            obstacles = GetComponentsInChildren<Obstacle>(true);

            // Cache the original local positions of all obstacles
            if (obstacles != null)
            {
                obstacleOriginalLocalPositions = new Vector3[obstacles.Length];
                for (int i = 0; i < obstacles.Length; i++)
                {
                    obstacleOriginalLocalPositions[i] = obstacles[i].transform.localPosition;
                }
            }
        }

        public void ResetSegment()
        {
            // Clear previously spawned buffs using ObjectPool instead of Destroy (0 GC Alloc!)
            foreach (GameObject buff in spawnedBuffs)
            {
                if (buff != null && ObjectPool.Instance != null)
                {
                    ObjectPool.Instance.Despawn(buff);
                }
                else if (buff != null)
                {
                    Destroy(buff); // Fallback
                }
            }
            spawnedBuffs.Clear();

            // Reset and randomize obstacles
            if (obstacles != null)
            {
                // First reset all obstacles to intact/active state
                for (int i = 0; i < obstacles.Length; i++)
                {
                    if (obstacles[i] != null)
                    {
                        obstacles[i].ResetObstacle();
                        obstacles[i].gameObject.SetActive(true);

                        // Reset to original position
                        obstacles[i].transform.localPosition = obstacleOriginalLocalPositions[i];
                    }
                }

                // Then randomize if enabled
                if (randomizeObstacles)
                {
                    RandomizeObstacles();
                }
            }

            // Reactivate collectibles or replace them with buffs
            if (collectibles != null)
            {
                foreach (Collectible coin in collectibles)
                {
                    if (coin != null)
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
                        else
                        {
                            coin.gameObject.SetActive(true);
                        }
                    }
                }
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