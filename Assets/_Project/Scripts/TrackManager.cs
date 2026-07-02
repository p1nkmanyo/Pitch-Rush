using System.Collections.Generic;
using UnityEngine;

namespace PitchRush
{
    public class TrackManager : MonoBehaviour
    {
        [Header("Track Prefabs")]
        public GameObject[] trackPrefabs;

        [Header("Settings")]
        public float trackLength = 20f;
        public int initialTracksToSpawn = 5;
        public Transform playerTransform;

        private float spawnZ = 0f;
        private List<GameObject> activeTracks = new List<GameObject>();
        private float safeZone = 30f; // Distance behind player to destroy track

        void Start()
        {
            // Spawn initial tracks
            for (int i = 0; i < initialTracksToSpawn; i++)
            {
                if (i == 0)
                {
                    // Force the first track to be a safe/empty one (e.g., index 0)
                    SpawnTrack(0);
                }
                else
                {
                    SpawnTrack(Random.Range(0, trackPrefabs.Length));
                }
            }
        }

        void Update()
        {
            if (playerTransform == null) return;

            // Check if we need to spawn a new track ahead
            if (playerTransform.position.z - safeZone > (spawnZ - initialTracksToSpawn * trackLength))
            {
                SpawnTrack(Random.Range(0, trackPrefabs.Length));
                DeleteTrack();
            }
        }

        private void SpawnTrack(int prefabIndex)
        {
            if (trackPrefabs.Length == 0) return;

            GameObject track = Instantiate(trackPrefabs[prefabIndex], transform.forward * spawnZ, transform.rotation);
            activeTracks.Add(track);
            spawnZ += trackLength;
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
