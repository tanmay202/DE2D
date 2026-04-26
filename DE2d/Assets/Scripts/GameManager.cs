using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Starting Money")]
    public int startingMoney = 100;

    [Header("UI")]
    public TextMeshProUGUI moneyText;   // HUD money display

    private int _money;
    public int Money => _money;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _money = startingMoney;
        RefreshUI();
    }

    public bool TrySpend(int amount)
    {
        if (_money < amount) return false;
        _money -= amount;
        RefreshUI();
        return true;
    }

    public void Earn(int amount)
    {
        _money += amount;
        RefreshUI();
    }

    void RefreshUI()
    {
        if (moneyText) moneyText.text = $"$ {_money}";
    }
}