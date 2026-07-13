using System.Collections.Generic;
using UnityEngine;

namespace PitchRush
{
    public class TrackManager : MonoBehaviour
    {
        [Header("Track Prefabs")]
        public GameObject[] trackPrefabs;

        [Header("Settings")]
        public int initialTracksToSpawn = 5;
        public int poolSizePerPrefab = 3;
        public Transform playerTransform;
        public float safeZone = 30f; // Distance behind player to recycle track

        // Track the current end position where the next segment should spawn
        private Vector3 nextSpawnPosition = Vector3.zero;

        // activeTracks stores a tuple of the segment object and its prefab index
        private List<(GameObject trackObj, int prefabIndex)> activeTracks = new List<(GameObject, int)>();

        // The Object Pool dictionary: Key is prefabIndex, Value is Queue of inactive GameObjects
        private Dictionary<int, Queue<GameObject>> trackPool = new Dictionary<int, Queue<GameObject>>();

        void Start()
        {
            InitializePool();

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

            // Check if we need to spawn a new track ahead.
            // We use the distance from player to the *first* active track's end point roughly.
            // A simpler logic is: if the player is far enough past the first active track, recycle it and spawn a new one.
            if (activeTracks.Count > 0)
            {
                GameObject oldestTrack = activeTracks[0].trackObj;
                TrackSegment segment = oldestTrack.GetComponent<TrackSegment>();

                // If the player has passed the end of the oldest track by safeZone distance
                if (segment != null && playerTransform.position.z - safeZone > segment.endPoint.position.z)
                {
                    SpawnTrack(Random.Range(0, trackPrefabs.Length));
                    RecycleOldestTrack();
                }
                else if (segment == null && playerTransform.position.z - safeZone > oldestTrack.transform.position.z + 20f)
                {
                    // Fallback if TrackSegment is missing
                    SpawnTrack(Random.Range(0, trackPrefabs.Length));
                    RecycleOldestTrack();
                }
            }
        }

        private void InitializePool()
        {
            for (int i = 0; i < trackPrefabs.Length; i++)
            {
                Queue<GameObject> queue = new Queue<GameObject>();
                for (int j = 0; j < poolSizePerPrefab; j++)
                {
                    GameObject obj = Instantiate(trackPrefabs[i], Vector3.zero, Quaternion.identity);
                    obj.SetActive(false);
                    // Parent to this manager to keep hierarchy clean
                    obj.transform.SetParent(transform);
                    queue.Enqueue(obj);
                }
                trackPool.Add(i, queue);
            }
        }

        private void SpawnTrack(int prefabIndex)
        {
            if (trackPrefabs.Length == 0) return;

            GameObject track = null;

            // Try to get from pool
            if (trackPool.ContainsKey(prefabIndex) && trackPool[prefabIndex].Count > 0)
            {
                track = trackPool[prefabIndex].Dequeue();
            }
            else
            {
                // If pool is empty (shouldn't happen with correct poolSizePerPrefab), instantiate a new one
                track = Instantiate(trackPrefabs[prefabIndex], Vector3.zero, Quaternion.identity);
                track.transform.SetParent(transform);
            }

            // Position and activate
            track.transform.position = nextSpawnPosition;
            track.transform.rotation = Quaternion.identity;
            track.SetActive(true);

            // Reset collectibles/obstacles
            TrackSegment segment = track.GetComponent<TrackSegment>();
            if (segment != null)
            {
                segment.ResetSegment();

                nextSpawnPosition = segment.endPoint.position;
            }
            else
            {
                // Fallback if no segment script is attached (assumes length of 20)
                nextSpawnPosition += Vector3.forward * 20f;
            }

            activeTracks.Add((track, prefabIndex));
        }

        private void RecycleOldestTrack()
        {
            if (activeTracks.Count > 0)
            {
                var oldestTrackInfo = activeTracks[0];
                GameObject trackObj = oldestTrackInfo.trackObj;
                int prefabIndex = oldestTrackInfo.prefabIndex;

                trackObj.SetActive(false);

                // Add back to pool
                if (trackPool.ContainsKey(prefabIndex))
                {
                    trackPool[prefabIndex].Enqueue(trackObj);
                }
                else
                {
                    Destroy(trackObj); // Safety fallback
                }

                activeTracks.RemoveAt(0);
            }
        }
    }
}
