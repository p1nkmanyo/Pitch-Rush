using System.Collections.Generic;
using UnityEngine;

namespace PitchRush
{
    public class TrackManager : MonoBehaviour
    {
        [Header("Track Prefabs")]
        [Tooltip("Straight track segment prefabs (randomly chosen).")]
        public GameObject[] trackPrefabs;

        [Header("Turn Settings")]
        [Tooltip("If assigned, turn segments will spawn periodically.")]
        public GameObject turnLeftPrefab;
        public GameObject turnRightPrefab;
        [Range(3, 12)]
        [Tooltip("Spawn a turn every N straight segments (randomized within range).")]
        public int minStraightBeforeTurn = 4;
        public int maxStraightBeforeTurn = 8;

        [Header("Settings")]
        public int initialTracksToSpawn = 5;
        public int poolSizePerPrefab = 3;
        public Transform playerTransform;
        public float safeZone = 30f;

        // Current track direction state (world-space forward for the runner)
        public Vector3 CurrentDirection { get; private set; } = Vector3.forward;
        public Quaternion CurrentRotation { get; private set; } = Quaternion.identity;

        private Vector3 nextSpawnPosition = Vector3.zero;
        private List<(GameObject trackObj, int prefabIndex)> activeTracks = new List<(GameObject, int)>();
        private Dictionary<int, Queue<GameObject>> trackPool = new Dictionary<int, Queue<GameObject>>();

        // Turn tracking
        private int straightCountSinceLastTurn = 0;
        private int nextTurnAfter = 5;

        // Special pool indices for turn prefabs
        private int turnLeftIndex = -1;
        private int turnRightIndex = -1;

        void Start()
        {
            // Assign turn prefab pool indices after the straight prefabs
            if (turnLeftPrefab != null)
            {
                turnLeftIndex = trackPrefabs.Length;
            }
            if (turnRightPrefab != null)
            {
                turnRightIndex = trackPrefabs.Length + (turnLeftPrefab != null ? 1 : 0);
            }

            nextTurnAfter = Random.Range(minStraightBeforeTurn, maxStraightBeforeTurn + 1);
            InitializePool();

            for (int i = 0; i < initialTracksToSpawn; i++)
            {
                if (i == 0)
                {
                    SpawnTrack(0);
                }
                else
                {
                    SpawnStraightOrTurn();
                }
            }
        }

        void Update()
        {
            if (playerTransform == null) return;

            if (activeTracks.Count > 0)
            {
                GameObject oldestTrack = activeTracks[0].trackObj;
                TrackSegment segment = oldestTrack.GetComponent<TrackSegment>();

                float playerProgress = Vector3.Dot(playerTransform.position, CurrentDirection);
                float trackEnd = 0f;

                if (segment != null && segment.endPoint != null)
                {
                    trackEnd = Vector3.Dot(segment.endPoint.position, CurrentDirection);

                    // Also check distance on the previous direction axis if the track was a turn
                    float distBehind = playerProgress - trackEnd;
                    float distOldAxis = Vector3.Distance(
                        Vector3.Project(playerTransform.position, CurrentDirection),
                        Vector3.Project(oldestTrack.transform.position, CurrentDirection)
                    );

                    if (distBehind > safeZone || distOldAxis > safeZone * 2f)
                    {
                        SpawnStraightOrTurn();
                        RecycleOldestTrack();
                    }
                }
                else
                {
                    // Fallback
                    float fallbackEnd = Vector3.Dot(oldestTrack.transform.position, CurrentDirection) + 20f;
                    if (playerProgress - safeZone > fallbackEnd)
                    {
                        SpawnStraightOrTurn();
                        RecycleOldestTrack();
                    }
                }
            }
        }

        private void SpawnStraightOrTurn()
        {
            bool canTurn = (turnLeftPrefab != null || turnRightPrefab != null);

            if (canTurn && straightCountSinceLastTurn >= nextTurnAfter)
            {
                SpawnTurnSegment();
                straightCountSinceLastTurn = 0;
                nextTurnAfter = Random.Range(minStraightBeforeTurn, maxStraightBeforeTurn + 1);
            }
            else
            {
                SpawnTrack(Random.Range(0, trackPrefabs.Length));
                straightCountSinceLastTurn++;
            }
        }

        private void SpawnTurnSegment()
        {
            // Randomly choose left or right turn
            bool goLeft = Random.value > 0.5f;

            // If only one turn prefab exists, use that one
            if (turnLeftPrefab != null && turnRightPrefab == null) goLeft = true;
            if (turnRightPrefab != null && turnLeftPrefab == null) goLeft = false;

            int poolIndex = goLeft ? turnLeftIndex : turnRightIndex;
            GameObject turnPrefab = goLeft ? turnLeftPrefab : turnRightPrefab;

            if (turnPrefab == null)
            {
                // Fallback to straight
                SpawnTrack(Random.Range(0, trackPrefabs.Length));
                straightCountSinceLastTurn++;
                return;
            }

            GameObject track = GetFromPool(poolIndex, turnPrefab);

            track.transform.position = nextSpawnPosition;
            track.transform.rotation = CurrentRotation;
            track.SetActive(true);

            TrackSegment segment = track.GetComponent<TrackSegment>();
            if (segment != null)
            {
                segment.ResetSegment();
                nextSpawnPosition = segment.endPoint.position;
            }
            else
            {
                nextSpawnPosition += CurrentDirection * 20f;
            }

            // Update direction: rotate 90 degrees left or right around Y axis
            float yAngle = goLeft ? -90f : 90f;
            CurrentRotation *= Quaternion.Euler(0f, yAngle, 0f);
            CurrentDirection = CurrentRotation * Vector3.forward;

            // Reset player steering to center after turn
            if (playerTransform != null)
            {
                PlayerController pc = playerTransform.GetComponent<PlayerController>();
                if (pc != null) pc.ResetLateralPosition();
            }

            activeTracks.Add((track, poolIndex));
        }

        private void InitializePool()
        {
            // Pool straight prefabs
            for (int i = 0; i < trackPrefabs.Length; i++)
            {
                Queue<GameObject> queue = new Queue<GameObject>();
                for (int j = 0; j < poolSizePerPrefab; j++)
                {
                    GameObject obj = Instantiate(trackPrefabs[i], Vector3.zero, Quaternion.identity);
                    obj.SetActive(false);
                    obj.transform.SetParent(transform);
                    queue.Enqueue(obj);
                }
                trackPool.Add(i, queue);
            }

            // Pool turn prefabs
            if (turnLeftPrefab != null)
            {
                Queue<GameObject> queue = new Queue<GameObject>();
                for (int j = 0; j < 2; j++)
                {
                    GameObject obj = Instantiate(turnLeftPrefab, Vector3.zero, Quaternion.identity);
                    obj.SetActive(false);
                    obj.transform.SetParent(transform);
                    queue.Enqueue(obj);
                }
                trackPool.Add(turnLeftIndex, queue);
            }
            if (turnRightPrefab != null)
            {
                Queue<GameObject> queue = new Queue<GameObject>();
                for (int j = 0; j < 2; j++)
                {
                    GameObject obj = Instantiate(turnRightPrefab, Vector3.zero, Quaternion.identity);
                    obj.SetActive(false);
                    obj.transform.SetParent(transform);
                    queue.Enqueue(obj);
                }
                trackPool.Add(turnRightIndex, queue);
            }
        }

        private GameObject GetFromPool(int poolIndex, GameObject prefab)
        {
            if (trackPool.ContainsKey(poolIndex) && trackPool[poolIndex].Count > 0)
            {
                return trackPool[poolIndex].Dequeue();
            }
            else
            {
                GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                obj.transform.SetParent(transform);
                return obj;
            }
        }

        private void SpawnTrack(int prefabIndex)
        {
            if (trackPrefabs.Length == 0) return;

            GameObject track = GetFromPool(prefabIndex, trackPrefabs[prefabIndex]);

            track.transform.position = nextSpawnPosition;
            track.transform.rotation = CurrentRotation;
            track.SetActive(true);

            TrackSegment segment = track.GetComponent<TrackSegment>();
            if (segment != null)
            {
                segment.ResetSegment();
                nextSpawnPosition = segment.endPoint.position;
            }
            else
            {
                nextSpawnPosition += CurrentDirection * 20f;
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

                if (trackPool.ContainsKey(prefabIndex))
                {
                    trackPool[prefabIndex].Enqueue(trackObj);
                }
                else
                {
                    Destroy(trackObj);
                }

                activeTracks.RemoveAt(0);
            }
        }
    }
}
