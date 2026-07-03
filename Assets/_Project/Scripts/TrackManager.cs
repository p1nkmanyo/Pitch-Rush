using System.Collections.Generic;
using UnityEngine;

namespace PitchRush
{
    public class TrackManager : MonoBehaviour
    {
        [Header("Track Prefabs")]
        public GameObject trackPrefab; // Base floor plane

        [Header("Spawning Prefabs")]
        public GameObject[] obstaclePrefabs;
        public GameObject coinPrefab;
        public GameObject[] powerupPrefabs;

        [Header("Settings")]
        public float trackLength = 20f;
        public int initialTracksToSpawn = 5;
        public Transform playerTransform;

        [Header("Lane Settings")]
        private float[] lanes = { -3f, 0f, 3f }; // Left, Center, Right lanes

        private float spawnZ = 0f;
        private List<GameObject> activeTracks = new List<GameObject>();
        private float safeZone = 30f; // Distance behind player to destroy track

        void Start()
        {
            // Spawn initial tracks
            for (int i = 0; i < initialTracksToSpawn; i++)
            {
                SpawnTrack(i == 0); // First track is safe (no obstacles)
            }
        }

        void Update()
        {
            if (playerTransform == null) return;

            // Check if we need to spawn a new track ahead
            if (playerTransform.position.z - safeZone > (spawnZ - initialTracksToSpawn * trackLength))
            {
                SpawnTrack(false);
                DeleteTrack();
            }
        }

        private void SpawnTrack(bool isSafeTrack)
        {
            if (trackPrefab == null) return;

            // Spawn the floor
            GameObject track = Instantiate(trackPrefab, transform.forward * spawnZ, transform.rotation);
            activeTracks.Add(track);

            if (!isSafeTrack)
            {
                SpawnInteractables(track.transform);
            }

            spawnZ += trackLength;
        }

        private void SpawnInteractables(Transform trackParent)
        {
            // Simple procedural generation: Pick a lane for obstacle, maybe spawn coins in another
            int obstacleLaneIndex = Random.Range(0, lanes.Length);

            // Spawn Obstacle
            if (obstaclePrefabs.Length > 0)
            {
                GameObject obsPrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                Vector3 obsPos = new Vector3(lanes[obstacleLaneIndex], 1f, spawnZ + (trackLength / 2f));
                GameObject obs = Instantiate(obsPrefab, obsPos, Quaternion.identity, trackParent);
            }

            // Spawn Coins in a different lane
            int coinLaneIndex = Random.Range(0, lanes.Length);
            while (coinLaneIndex == obstacleLaneIndex)
            {
                coinLaneIndex = Random.Range(0, lanes.Length);
            }

            if (coinPrefab != null)
            {
                // Spawn a line of 3 coins
                for (int i = 0; i < 3; i++)
                {
                    Vector3 coinPos = new Vector3(lanes[coinLaneIndex], 1f, spawnZ + (trackLength * 0.25f) + (i * 2f));
                    Instantiate(coinPrefab, coinPos, Quaternion.identity, trackParent);
                }
            }

            // Rare chance to spawn a powerup
            if (powerupPrefabs.Length > 0 && Random.value < 0.15f) // 15% chance
            {
                int powerupLane = Random.Range(0, lanes.Length);
                GameObject powerup = powerupPrefabs[Random.Range(0, powerupPrefabs.Length)];
                Vector3 powerPos = new Vector3(lanes[powerupLane], 1f, spawnZ + (trackLength * 0.8f));
                Instantiate(powerup, powerPos, Quaternion.identity, trackParent);
            }
        }

        private void DeleteTrack()
        {
            if (activeTracks.Count > 0)
            {
                Destroy(activeTracks[0]);
                activeTracks.RemoveAt(0);
            }
        }
    }
}
