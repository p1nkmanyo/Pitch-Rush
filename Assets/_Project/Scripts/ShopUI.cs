using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PitchRush
{
    public class ShopUI : MonoBehaviour
    {
        [Header("References")]
        public ShopManager shopManager;
        public MainMenuManager menuManager;

        [Header("UI Templates")]
        public GameObject shopItemPrefab; // Prefab for a single shop item UI
        public Transform itemsContainer; // Parent container (e.g., a VerticalLayoutGroup)

        [System.Serializable]
        public struct ShopItemConfig
        {
            public BallState state;
            public string itemName;
            public Sprite icon;
        }

        public ShopItemConfig[] itemConfigs;

        private void OnEnable()
        {
            RefreshShopUI();
        }

        public void RefreshShopUI()
        {
            if (shopManager == null)
            {
                // Fallback to find it if not assigned
                shopManager = FindObjectOfType<ShopManager>();
                if (shopManager == null)
                {
                    Debug.LogError("ShopManager not found in scene!");
                    return;
                }
            }

            // Clear existing items
            foreach (Transform child in itemsContainer)
            {
                Destroy(child.gameObject);
            }

            // Create items based on ShopManager's catalog
            foreach (var item in shopManager.shopItems)
            {
                GameObject itemObj = Instantiate(shopItemPrefab, itemsContainer);
                ShopItemUI itemUI = itemObj.GetComponent<ShopItemUI>();
                if (itemUI != null)
                {
                    // Find config for icon/name if available
                    string displayName = item.state.ToString();
                    Sprite icon = null;
                    foreach (var config in itemConfigs)
                    {
                        if (config.state == item.state)
                        {
                            if (!string.IsNullOrEmpty(config.itemName)) displayName = config.itemName;
                            icon = config.icon;
                            break;
                        }
                    }

                    itemUI.Setup(item, displayName, icon, this);
                }
            }

            if (menuManager != null)
            {
                menuManager.UpdateCoinsDisplay();
            }
        }

        public void AttemptPurchaseOrSelect(BallState state)
        {
            if (shopManager == null) return;

            bool isUnlocked = PlayerPrefs.GetInt("Unlocked_" + state.ToString(), 0) == 1;

            // Normal is always unlocked
            if (state == BallState.Normal) isUnlocked = true;

            if (isUnlocked)
            {
                shopManager.SelectBall(state);
            }
            else
            {
                shopManager.BuyBall(state);
            }

            RefreshShopUI();
        }
    }
}
