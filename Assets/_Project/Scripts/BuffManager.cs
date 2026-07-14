using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PitchRush
{
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
            return IsBuffActive(BuffType.SpeedBoost) || isTemporaryInvincible;
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
