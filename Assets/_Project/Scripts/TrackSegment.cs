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
        private List<GameObject> spawnedBuffs = new List<GameObject>();

        private void Awake()
        {
            // Cache all collectibles in this segment so we can quickly reactivate them
            collectibles = GetComponentsInChildren<Collectible>(true); // true to include inactive
        }

        public void ResetSegment()
        {
            // Clear previously spawned buffs to prevent leaks
            foreach (GameObject buff in spawnedBuffs)
            {
                if (buff != null)
                {
                    Destroy(buff);
                }
            }
            spawnedBuffs.Clear();

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

                            // Select and spawn a random buff prefab
                            GameObject selectedBuffPrefab = buffPrefabs[Random.Range(0, buffPrefabs.Length)];
                            if (selectedBuffPrefab != null)
                            {
                                GameObject buffInstance = Instantiate(selectedBuffPrefab, coin.transform.position, Quaternion.identity, transform);
                                spawnedBuffs.Add(buffInstance);
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