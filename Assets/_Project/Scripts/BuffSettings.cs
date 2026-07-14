using UnityEngine;

namespace PitchRush
{
    [CreateAssetMenu(fileName = "NewBuffSettings", menuName = "Pitch Rush/Buff Settings")]
    public class BuffSettings : ScriptableObject
    {
        [Header("Identity")]
        public string buffName;
        public BuffType buffType;
        public Sprite uiIcon;

        [Header("Settings")]
        public float duration = 5f;
        public GameObject vfxPrefab;
        
        [Header("VFX Settings")]
        public Vector3 vfxOffset = Vector3.zero;
        public bool parentVFXToPlayer = true;
    }
}
