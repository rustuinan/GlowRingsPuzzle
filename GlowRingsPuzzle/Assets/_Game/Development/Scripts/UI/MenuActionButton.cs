using UnityEngine;
using UnityEngine.UI;

public enum MenuPage { Settings = 0, Statistics = 1, Wallet = 2, Shop = 4, DailyReward = 5, DailySpin = 6, PuzzleMode = 7 }

[RequireComponent(typeof(Button))]
public class MenuActionButton : MonoBehaviour
{
    [SerializeField] private MainMenuUI menu;
    [SerializeField] private MenuPage page;
    private Button button;
    public void Configure(MainMenuUI controller, MenuPage destination) { menu = controller; page = destination; }
    private void OnEnable() { button = GetComponent<Button>(); button.onClick.AddListener(Open); }
    private void OnDisable() { if (button != null) button.onClick.RemoveListener(Open); }
    private void Open() { if (menu != null) menu.ShowPage(page); }
}
