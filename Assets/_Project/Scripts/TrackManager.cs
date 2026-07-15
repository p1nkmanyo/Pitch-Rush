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

        // Current track direction state (world-space forward for the active player path)
        public Vector3 CurrentDirection { get; private set; } = Vector3.forward;
        public Quaternion CurrentRotation { get; private set; } = Quaternion.identity;
        public Vector3 PivotPoint { get; private set; } = Vector3.zero;

        // Path generation state (used for positioning prefabs ahead of the player)
        private Vector3 genDirection = Vector3.forward;
        private Quaternion genRotation = Quaternion.identity;

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
                    SpawnTrack(0, false); // First track is 100% safe (no obstacles)
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

                // Calculate progress relative to the END of the oldest track segment (prevent early recycling!)
                Vector3 refPosition = oldestTrack.transform.position + oldestTrack.transform.forward * 20f;
                if (segment != null && segment.endPoint != null)
                {
                    refPosition = segment.endPoint.position;
                }

                Vector3 playerToRef = refPosition - playerTransform.position;
                float dot = Vector3.Dot(playerToRef, oldestTrack.transform.forward);

                // Recycle only if the player is safely past the END of the track by safeZone distance
                if (dot < -safeZone)
                {
                    SpawnStraightOrTurn();
                    RecycleOldestTrack();
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
            bool goLeft = Random.value > 0.5f;

            if (turnLeftPrefab != null && turnRightPrefab == null) goLeft = true;
            if (turnRightPrefab != null && turnLeftPrefab == null) goLeft = false;

            int poolIndex = goLeft ? turnLeftIndex : turnRightIndex;
            GameObject turnPrefab = goLeft ? turnLeftPrefab : turnRightPrefab;

            if (turnPrefab == null)
            {
                SpawnTrack(Random.Range(0, trackPrefabs.Length));
                straightCountSinceLastTurn++;
                return;
            }

            GameObject track = GetFromPool(poolIndex, turnPrefab);

            // Position using the generation rotation state (NOT player active rotation!)
            track.transform.position = nextSpawnPosition;
            track.transform.rotation = genRotation;
            track.SetActive(true);

            TrackSegment segment = track.GetComponent<TrackSegment>();
            if (segment != null)
            {
                segment.ResetSegment();
                nextSpawnPosition = segment.endPoint.position;
            }
            else
            {
                nextSpawnPosition += genDirection * 20f;
            }

            // Update path generation direction (rotate 90 degrees left or right around Y axis)
            float yAngle = goLeft ? -90f : 90f;
            genRotation *= Quaternion.Euler(0f, yAngle, 0f);
            genDirection = genRotation * Vector3.forward;

            // Auto-configure TurnTrigger component dynamically so the user doesn't have to do it in editor
            TurnTrigger trigger = track.GetComponentInChildren<TurnTrigger>();
            if (trigger == null)
            {
                Collider col = track.GetComponentInChildren<Collider>();
                if (col != null && col.isTrigger)
                {
                    trigger = col.gameObject.AddComponent<TurnTrigger>();
                }
                else
                {
                    GameObject triggerObj = new GameObject("TurnTrigger");
                    triggerObj.transform.SetParent(track.transform, false);
                    BoxCollider box = triggerObj.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.size = new Vector3(12f, 6f, 3f);
                    // Move it slightly forward to trigger exactly at the center of the turn
                    triggerObj.transform.localPosition = new Vector3(0f, 1f, 0f);
                    trigger = triggerObj.AddComponent<TurnTrigger>();
                }
            }
            trigger.turnAngle = yAngle;

            activeTracks.Add((track, poolIndex));
        }

        public void TriggerTurn(float yAngle, Vector3 newPivotPoint)
        {
            CurrentRotation *= Quaternion.Euler(0f, yAngle, 0f);
            CurrentDirection = CurrentRotation * Vector3.forward;
            PivotPoint = newPivotPoint;

            // Reset player steering to center of the new lane
            if (playerTransform != null)
            {
                PlayerController pc = playerTransform.GetComponent<PlayerController>();
                if (pc != null) pc.ResetLateralPosition();
            }

            Debug.Log($"Turn executed! New Direction: {CurrentDirection}, New Pivot: {PivotPoint}");
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

        private void SpawnTrack(int prefabIndex, bool spawnObstacles = true)
        {
            if (trackPrefabs.Length == 0) return;

            GameObject track = GetFromPool(prefabIndex, trackPrefabs[prefabIndex]);

            track.transform.position = nextSpawnPosition;
            track.transform.rotation = genRotation;
            track.SetActive(true);

            TrackSegment segment = track.GetComponent<TrackSegment>();
            if (segment != null)
            {
                segment.ResetSegment(spawnObstacles);
                nextSpawnPosition = segment.endPoint.position;
            }
            else
            {
                nextSpawnPosition += genDirection * 20f;
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
