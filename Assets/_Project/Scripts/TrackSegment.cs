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

        // Cached original local positions and scales
        private Vector3[] obstacleOriginalLocalPositions;
        private Vector3[] coinOriginalLocalPositions;
        private Vector3[] obstacleOriginalLocalScales;
        private Vector3[] coinOriginalLocalScales;

        // Lane X positions for random coin placement
        private float[] laneXPositions = new float[] { -3f, 0f, 3f };

        private void Awake()
        {
            collectibles = GetComponentsInChildren<Collectible>(true);
            obstacles = GetComponentsInChildren<Obstacle>(true);

            // Cache original positions and scales of all obstacles
            if (obstacles != null)
            {
                obstacleOriginalLocalPositions = new Vector3[obstacles.Length];
                obstacleOriginalLocalScales = new Vector3[obstacles.Length];
                for (int i = 0; i < obstacles.Length; i++)
                {
                    obstacleOriginalLocalPositions[i] = obstacles[i].transform.localPosition;
                    obstacleOriginalLocalScales[i] = obstacles[i].transform.localScale;
                }
            }

            // Cache original positions and scales of all coins
            if (collectibles != null)
            {
                coinOriginalLocalPositions = new Vector3[collectibles.Length];
                coinOriginalLocalScales = new Vector3[collectibles.Length];
                for (int i = 0; i < collectibles.Length; i++)
                {
                    coinOriginalLocalPositions[i] = collectibles[i].transform.localPosition;
                    coinOriginalLocalScales[i] = collectibles[i].transform.localScale;
                }
            }
        }

        public void ResetSegment(bool spawnObstacles = true)
        {
            // Reset any TurnTrigger components on this segment (0 GC Alloc!)
            TurnTrigger[] turnTriggers = GetComponentsInChildren<TurnTrigger>(true);
            foreach (TurnTrigger tt in turnTriggers)
            {
                if (tt != null) tt.ResetTrigger();
            }

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
                        obstacles[i].gameObject.SetActive(spawnObstacles);
                        
                        // Reset to original positions and scales, correcting for parent scaling
                        obstacles[i].transform.localPosition = new Vector3(
                            obstacleOriginalLocalPositions[i].x / transform.localScale.x,
                            obstacleOriginalLocalPositions[i].y / transform.localScale.y,
                            obstacleOriginalLocalPositions[i].z / transform.localScale.z
                        );
                        obstacles[i].transform.localScale = new Vector3(
                            obstacleOriginalLocalScales[i].x / transform.localScale.x,
                            obstacleOriginalLocalScales[i].y / transform.localScale.y,
                            obstacleOriginalLocalScales[i].z / transform.localScale.z
                        );
                    }
                }

                if (spawnObstacles && randomizeObstacles)
                {
                    RandomizeLayout();
                }
                else
                {
                    // If no obstacles, place all coins in the center lane (safe zone)
                    PlaceCoinsInLane(0f);
                }
            }
        }

        private void RandomizeLayout()
        {
            if (obstacles == null || obstacles.Length < 3) return;

            // Choose a random obstacle pattern (0 to 5)
            // Left=0, Center=1, Right=2
            int pattern = Random.Range(0, 6);
            
            bool spawnLeft = false;
            bool spawnCenter = false;
            bool spawnRight = false;
            int safeLane = 1; // Default center is safe

            switch (pattern)
            {
                case 0: // Left lane blocked
                    spawnLeft = true;
                    safeLane = Random.value > 0.5f ? 1 : 2; // Center or Right is safe
                    break;
                case 1: // Center lane blocked
                    spawnCenter = true;
                    safeLane = Random.value > 0.5f ? 0 : 2; // Left or Right is safe
                    break;
                case 2: // Right lane blocked
                    spawnRight = true;
                    safeLane = Random.value > 0.5f ? 0 : 1; // Left or Center is safe
                    break;
                case 3: // Left + Center blocked (Right is safe)
                    spawnLeft = true;
                    spawnCenter = true;
                    safeLane = 2;
                    break;
                case 4: // Center + Right blocked (Left is safe)
                    spawnCenter = true;
                    spawnRight = true;
                    safeLane = 0;
                    break;
                case 5: // Left + Right blocked (Center is safe)
                    spawnLeft = true;
                    spawnRight = true;
                    safeLane = 1;
                    break;
            }

            // Set active states and Z positions for obstacles (placed in the middle of segment)
            float segmentLength = GetSegmentLength();
            float obstacleZ = segmentLength * 0.5f;

            for (int i = 0; i < obstacles.Length; i++)
            {
                if (obstacles[i] == null) continue;

                bool isActive = false;
                float laneX = 0f;

                if (i == 0) { isActive = spawnLeft; laneX = laneXPositions[0]; }
                else if (i == 1) { isActive = spawnCenter; laneX = laneXPositions[1]; }
                else if (i == 2) { isActive = spawnRight; laneX = laneXPositions[2]; }

                obstacles[i].gameObject.SetActive(isActive);
                if (isActive)
                {
                    // Apply random jitter to Z position for dynamic feel
                    float jitter = Random.Range(-obstacleZJitter, obstacleZJitter);
                    float targetZ = obstacleZ + jitter;
                    
                    // Divide positions and scales by parent scale to completely cancel parent distortion!
                    float localX = laneX / transform.localScale.x;
                    float localY = obstacleOriginalLocalPositions[i].y / transform.localScale.y;
                    float localZ = targetZ / transform.localScale.z;
                    obstacles[i].transform.localPosition = new Vector3(localX, localY, localZ);

                    float scaleX = obstacleOriginalLocalScales[i].x / transform.localScale.x;
                    float scaleY = obstacleOriginalLocalScales[i].y / transform.localScale.y;
                    float scaleZ = obstacleOriginalLocalScales[i].z / transform.localScale.z;
                    obstacles[i].transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                }
            }

            // Place coins in the designated safe lane
            PlaceCoinsInLane(laneXPositions[safeLane]);
        }

        private void PlaceCoinsInLane(float laneX)
        {
            if (collectibles == null || collectibles.Length == 0) return;

            float segmentLength = GetSegmentLength();
            float spacing = segmentLength / (collectibles.Length + 1);

            for (int i = 0; i < collectibles.Length; i++)
            {
                if (collectibles[i] == null) continue;

                // Reset default scales initially
                collectibles[i].transform.localScale = coinOriginalLocalScales[i];

                // Roll visibility chance per coin
                if (Random.value > coinVisibilityChance)
                {
                    collectibles[i].gameObject.SetActive(false);
                    continue;
                }

                collectibles[i].gameObject.SetActive(true);

                // Place the coin neatly spaced along the safe lane
                float coinZ = spacing * (i + 1);
                float coinY = coinOriginalLocalPositions[i].y;

                // Divide positions and scales by parent scale to completely cancel parent distortion!
                float localX = laneX / transform.localScale.x;
                float localY = coinY / transform.localScale.y;
                float localZ = coinZ / transform.localScale.z;
                collectibles[i].transform.localPosition = new Vector3(localX, localY, localZ);

                float scaleX = coinOriginalLocalScales[i].x / transform.localScale.x;
                float scaleY = coinOriginalLocalScales[i].y / transform.localScale.y;
                float scaleZ = coinOriginalLocalScales[i].z / transform.localScale.z;
                collectibles[i].transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

                // Roll buff replacements on active coins
                if (buffPrefabs != null && buffPrefabs.Length > 0 && Random.value < buffSpawnChance)
                {
                    collectibles[i].gameObject.SetActive(false);

                    GameObject selectedBuffPrefab = buffPrefabs[Random.Range(0, buffPrefabs.Length)];
                    if (selectedBuffPrefab != null)
                    {
                        GameObject buffInstance = null;
                        if (ObjectPool.Instance != null)
                        {
                            buffInstance = ObjectPool.Instance.Spawn(selectedBuffPrefab, collectibles[i].transform.position, Quaternion.identity);
                            buffInstance.transform.SetParent(transform);
                        }
                        else
                        {
                            buffInstance = Instantiate(selectedBuffPrefab, collectibles[i].transform.position, Quaternion.identity, transform);
                        }

                        if (buffInstance != null)
                        {
                            spawnedBuffs.Add(buffInstance);
                        }
                    }
                }
            }
        }

        private float GetSegmentLength()
        {
            if (endPoint != null)
            {
                float len = Mathf.Abs(endPoint.localPosition.z);
                return len > 5f ? len : 20f;
            }
            return 20f;
        }
    }
}