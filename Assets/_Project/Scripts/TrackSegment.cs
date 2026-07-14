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

        private Collectible[] collectibles;
        private Obstacle[] obstacles;
        private List<GameObject> spawnedBuffs = new List<GameObject>();

        private void Awake()
        {
            // Cache all collectibles and obstacles in this segment so we can quickly reactivate/reset them
            collectibles = GetComponentsInChildren<Collectible>(true);
            obstacles = GetComponentsInChildren<Obstacle>(true);
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

            // Reset all obstacles to their intact/active state for mobile recycling (0 GC Alloc!)
            if (obstacles != null)
            {
                foreach (Obstacle obs in obstacles)
                {
                    if (obs != null) obs.ResetObstacle();
                }
            }

            // Reactivate collectibles or replace them with buffs
            if (collectibles != null)
            {
                foreach (Collectible coin in collectibles)
                {
                    if (coin != null)
                    {
                        // Roll a chance to replace coin with a buff if prefabs are configured
                        if (buffPrefabs != null && buffPrefabs.Length > 0 && Random.value < buffSpawnChance)
                        {
                            coin.gameObject.SetActive(false); // Hide the coin

                            // Select and spawn a random buff prefab via ObjectPool
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
                            coin.gameObject.SetActive(true); // Spawn standard coin
                        }
                    }
                }
            }
        }
    }
}