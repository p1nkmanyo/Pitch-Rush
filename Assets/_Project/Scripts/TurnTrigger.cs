using UnityEngine;

namespace PitchRush
{
    public class TurnTrigger : MonoBehaviour
    {
        [Tooltip("The angle to turn: -90 for left, 90 for right.")]
        public float turnAngle = 90f;
        private bool triggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;

            if (other.CompareTag("Player"))
            {
                triggered = true;
                TrackManager tm = FindAnyObjectByType<TrackManager>();
                if (tm != null)
                {
                    // Find the endPoint of this track segment to set as the new pivot
                    TrackSegment segment = GetComponentInParent<TrackSegment>();
                    Vector3 newPivot = transform.position;
                    if (segment != null && segment.endPoint != null)
                    {
                        newPivot = segment.endPoint.position;
                    }
                    tm.TriggerTurn(turnAngle, newPivot);
                }
            }
        }

        public void ResetTrigger()
        {
            triggered = false;
        }
    }
}
