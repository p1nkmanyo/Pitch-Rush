using UnityEngine;

namespace PitchRush
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        private const string COINS_KEY = "TotalCoins";
        private const string SELECTED_BALL_KEY = "SelectedBallIndex";

        private int totalCoins;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Keep shop manager alive across scenes
                LoadData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadData()
        {
            totalCoins = PlayerPrefs.GetInt(COINS_KEY, 0);
        }

        public void AddCoins(int amount)
        {
            totalCoins += amount;
            PlayerPrefs.SetInt(COINS_KEY, totalCoins);
            PlayerPrefs.Save();
        }

        public int GetTotalCoins()
        {
            return totalCoins;
        }

        public bool TryPurchase(int cost, string itemId)
        {
            if (totalCoins >= cost)
            {
                totalCoins -= cost;
                PlayerPrefs.SetInt(COINS_KEY, totalCoins);
                PlayerPrefs.SetInt(itemId, 1); // 1 means unlocked
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        public bool IsUnlocked(string itemId)
        {
            // By default, item "Ball_0" (default ball) could be unlocked
            if (itemId == "Ball_0") return true;
            return PlayerPrefs.GetInt(itemId, 0) == 1;
        }

        public void SelectBall(int index)
        {
            PlayerPrefs.SetInt(SELECTED_BALL_KEY, index);
            PlayerPrefs.Save();
        }

        public int GetSelectedBall()
        {
            return PlayerPrefs.GetInt(SELECTED_BALL_KEY, 0);
        }
    }
}
