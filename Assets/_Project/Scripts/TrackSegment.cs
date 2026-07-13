using UnityEngine;

namespace PitchRush
{
    public class TrackSegment : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The transform located at the exact end of this track segment.")]
        public Transform endPoint;

        private Collectible[] collectibles;
        // Optionally cache obstacles if they need specific reset logic in the future
        // private Obstacle[] obstacles;

        private void Awake()
        {
            // Cache all collectibles in this segment so we can quickly reactivate them
            collectibles = GetComponentsInChildren<Collectible>(true); // true to include inactive
        }

        public void ResetSegment()
        {
            // Reactivate all collectibles that might have been disabled (collected) previously
            if (collectibles != null)
            {
                foreach (Collectible coin in collectibles)
                {
                    if (coin != null)
                    {
                        coin.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}