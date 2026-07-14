using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Removed namespace PitchRush so local UI scripts can access ShopManager without errors

[System.Serializable]
public class SkinItem
{
    public string skinName; // e.g., "Normal", "HeavyIron", "LightPingPong"
    public int price;
}

public class ShopManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text totalCoinsText;

    [Header("Skins Data")]
    public SkinItem[] skins;

        private void OnEnable()
        {
            UpdateCoinsDisplay();
        }

        public void UpdateCoinsDisplay()
        {
            int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
            if (totalCoinsText != null)
            {
                totalCoinsText.text = $"Coins: {totalCoins}";
            }
        }

        public void BuySkin(string skinName)
        {
            SkinItem skinToBuy = null;
            foreach (var skin in skins)
            {
                if (skin.skinName == skinName)
                {
                    skinToBuy = skin;
                    break;
                }
            }

            if (skinToBuy == null)
            {
                Debug.LogError($"Skin {skinName} not found in ShopManager!");
                return;
            }

            // Check if already unlocked (Default "Normal" is always unlocked)
            if (skinName == "Normal" || PlayerPrefs.GetInt($"UnlockedSkin_{skinName}", 0) == 1)
            {
                SelectSkin(skinName);
                return;
            }

            int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
            if (totalCoins >= skinToBuy.price)
            {
                // Buy the skin
                totalCoins -= skinToBuy.price;
                PlayerPrefs.SetInt("TotalCoins", totalCoins);
                PlayerPrefs.SetInt($"UnlockedSkin_{skinName}", 1);
                PlayerPrefs.Save();

                UpdateCoinsDisplay();
                SelectSkin(skinName);
                Debug.Log($"Bought skin: {skinName}");
            }
            else
            {
                Debug.Log($"Not enough coins to buy {skinName}!");
            }
        }

    public void SelectSkin(string skinName)
    {
        // We assume calling this means either it's bought or unlocked
        PlayerPrefs.SetString("SelectedSkin", skinName);
        PlayerPrefs.Save();
        Debug.Log($"Selected skin: {skinName}");

        // Here you could update UI to show which skin is selected (e.g., enable a checkmark)
    }
}