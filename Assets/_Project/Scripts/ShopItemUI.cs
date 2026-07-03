using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PitchRush
{
    public class ShopItemUI : MonoBehaviour
    {
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI priceText;
        public Button actionButton;
        public TextMeshProUGUI actionButtonText;

        private BallState itemState;
        private ShopUI parentUI;

        public void Setup(ShopManager.ShopItem itemData, string displayName, Sprite icon, ShopUI parent)
        {
            itemState = itemData.state;
            parentUI = parent;

            if (nameText != null) nameText.text = displayName;
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }
            else if (iconImage != null)
            {
                iconImage.enabled = false;
            }

            bool isUnlocked = PlayerPrefs.GetInt("Unlocked_" + itemData.state.ToString(), 0) == 1;
            if (itemData.state == BallState.Normal) isUnlocked = true;

            BallState selectedState = (BallState)PlayerPrefs.GetInt("SelectedBallState", 0);
            bool isSelected = (selectedState == itemData.state);

            if (priceText != null)
            {
                priceText.text = isUnlocked ? "Owned" : itemData.cost.ToString() + " Coins";
            }

            if (actionButtonText != null)
            {
                if (isSelected)
                {
                    actionButtonText.text = "Selected";
                    actionButton.interactable = false;
                }
                else if (isUnlocked)
                {
                    actionButtonText.text = "Select";
                    actionButton.interactable = true;
                }
                else
                {
                    actionButtonText.text = "Buy";
                    // Only interactable if we have enough coins
                    int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
                    actionButton.interactable = (totalCoins >= itemData.cost);
                }
            }

            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionClicked);
        }

        private void OnActionClicked()
        {
            if (parentUI != null)
            {
                parentUI.AttemptPurchaseOrSelect(itemState);
            }
        }
    }
}
