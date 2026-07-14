using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PitchRush
{
    public struct PlayerStatePoint
    {
        public Vector3 position;
        public float time;

        public PlayerStatePoint(Vector3 pos, float t)
        {
            position = pos;
            time = t;
        }
    }

    public class BuffManager : MonoBehaviour
    {
        [Header("Magnet Settings")]
        public float magnetRadius = 8f;
        public float magnetPullSpeed = 25f;

        [Header("Invincibility Settings")]
        public float shieldInvincibilityDuration = 1.5f;

        private Rigidbody rb;
        private Dictionary<BuffType, BuffSettings> activeBuffs = new Dictionary<BuffType, BuffSettings>();
        private Dictionary<BuffType, float> buffTimers = new Dictionary<BuffType, float>();
        private Dictionary<BuffType, GameObject> spawnedVfx = new Dictionary<BuffType, GameObject>();

        // Chrono Rewind states
        private List<PlayerStatePoint> stateHistory = new List<PlayerStatePoint>();
        private bool isRewinding = false;
        private bool isTemporaryInvincible = false;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            UpdateBuffTimers();
            HandleMagnet();
        }

        void FixedUpdate()
        {
            // Record position history for Chrono Rewind
            if (!isRewinding && GameManager.Instance != null && GameManager.Instance.IsGameActive)
            {
                stateHistory.Add(new PlayerStatePoint(transform.position, Time.time));

                // Keep history to exactly 2 seconds
                while (stateHistory.Count > 0 && Time.time - stateHistory[0].time > 2f)
                {
                    stateHistory.RemoveAt(0);
                }
            }
        }

        public void ApplyBuff(BuffSettings settings)
        {
            if (settings == null) return;

            BuffType type = settings.buffType;

            // If already active, refresh duration
            if (activeBuffs.ContainsKey(type))
            {
                buffTimers[type] = settings.duration;
                Debug.Log($"Refreshed buff: {settings.buffName}");
                return;
            }

            // Apply new buff
            activeBuffs.Add(type, settings);
            buffTimers.Add(type, settings.duration);

            // Spawn VFX
            if (settings.vfxPrefab != null)
            {
                GameObject vfx = Instantiate(settings.vfxPrefab, transform.position + settings.vfxOffset, Quaternion.identity);
                if (settings.parentVFXToPlayer)
                {
                    vfx.transform.SetParent(transform);
                }
                spawnedVfx.Add(type, vfx);
            }

            // Trigger special buff actions
            if (type == BuffType.MercuryFlow)
            {
                PlayerController controller = GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.StartMercurySplit();
                }
            }

            Debug.Log($"Applied buff: {settings.buffName} for {settings.duration} seconds.");
        }

        public void RemoveBuff(BuffType type)
        {
            if (!activeBuffs.ContainsKey(type)) return;

            activeBuffs.Remove(type);
            buffTimers.Remove(type);

            // Clean up VFX
            if (spawnedVfx.ContainsKey(type))
            {
                if (spawnedVfx[type] != null)
                {
                    Destroy(spawnedVfx[type]);
                }
                spawnedVfx.Remove(type);
            }

            // Trigger special buff end actions
            if (type == BuffType.MercuryFlow)
            {
                PlayerController controller = GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.StopMercuryMerge();
                }
            }

            Debug.Log($"Removed buff: {type}");
        }

        public bool IsBuffActive(BuffType type)
        {
            return activeBuffs.ContainsKey(type);
        }

        public bool IsInvincible()
        {
            return IsBuffActive(BuffType.SpeedBoost) || isTemporaryInvincible || isRewinding;
        }

        public void ConsumeShield()
        {
            if (IsBuffActive(BuffType.Shield))
            {
                RemoveBuff(BuffType.Shield);
                // Grant temporary invincibility so player can recover
                StartCoroutine(TemporaryInvincibilityCoroutine(shieldInvincibilityDuration));
                Debug.Log("Shield consumed! Temporary invincibility active.");
            }
        }

        public bool TriggerChronoRewind()
        {
            if (IsBuffActive(BuffType.ChronoRewind) && !isRewinding)
            {
                StartCoroutine(RewindCoroutine());
                return true;
            }
            return false;
        }

        public float GetRemainingTime(BuffType type)
        {
            if (buffTimers.ContainsKey(type))
            {
                return buffTimers[type];
            }
            return 0f;
        }

        private void UpdateBuffTimers()
        {
            List<BuffType> keysToUpdate = new List<BuffType>(buffTimers.Keys);

            foreach (var key in keysToUpdate)
            {
                buffTimers[key] -= Time.deltaTime;

                if (buffTimers[key] <= 0f)
                {
                    RemoveBuff(key);
                }
            }
        }

        private void HandleMagnet()
        {
            if (!IsBuffActive(BuffType.Magnet)) return;

            // Search for collectible items around the player
            Collider[] colliders = Physics.OverlapSphere(transform.position, magnetRadius);
            foreach (var col in colliders)
            {
                if (col.CompareTag("Player")) continue;

                Collectible coin = col.GetComponent<Collectible>();
                if (coin != null)
                {
                    // Pull the coin smoothly towards the player
                    col.transform.position = Vector3.MoveTowards(
                        col.transform.position,
                        transform.position,
                        magnetPullSpeed * Time.deltaTime
                    );
                }
            }
        }

        private IEnumerator RewindCoroutine()
        {
            isRewinding = true;
            rb.isKinematic = true; // Disable physics

            // Store the current game over state if GameManager is paused, but we want to unpause it for gameplay
            Time.timeScale = 1f; 

            // Deactivate the ChronoRewind buff immediately so it is not consumed again
            RemoveBuff(BuffType.ChronoRewind);

            // Replay position history backwards
            for (int i = stateHistory.Count - 1; i >= 0; i--)
            {
                transform.position = stateHistory[i].position;
                yield return new WaitForSecondsRealtime(0.02f); // Rapid rewind (ignores timescale)
            }

            stateHistory.Clear();
            rb.isKinematic = false;

            // Stop all momentum to prevent launching the ball post-rewind
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Reset the player to the center lane or closest lane to prevent immediately falling again
            PlayerController controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                // Align targetX to the closest boundary clamp to keep them safe
                // (No lanes are used in continuous steering, but we clamp targetX to current position)
                // We keep targetX at the safe rewind position.
            }

            // Give a temporary invincibility shield to recover
            StartCoroutine(TemporaryInvincibilityCoroutine(2f));
            isRewinding = false;

            Debug.Log("Chrono Rewind finished!");
        }

        public IEnumerator TemporaryInvincibilityCoroutine(float duration)
        {
            isTemporaryInvincible = true;

            // Blinking effect
            Renderer renderer = GetComponentInChildren<Renderer>();
            float elapsed = 0f;
            float blinkInterval = 0.1f;

            while (elapsed < duration)
            {
                if (renderer != null)
                {
                    renderer.enabled = !renderer.enabled;
                }
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            if (renderer != null)
            {
                renderer.enabled = true;
            }

            isTemporaryInvincible = false;
        }
    }
}
