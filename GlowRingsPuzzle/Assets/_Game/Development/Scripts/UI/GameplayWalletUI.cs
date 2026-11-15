using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameplayWalletUI : MonoBehaviour
{
    [SerializeField] private GameManager game;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text balance;
    private CosmeticsStoreUI store;
    private GameplaySettingsUI settings;

    private bool CanOpen => !game.IsGameOver && !game.IsResolving && !game.IsPieceInteracting &&
        !game.IsBoosterOfferOpen && !game.IsBombTargeting && !game.IsTutorialActive &&
        (settings == null || !settings.IsOpen) && (store == null || !store.gameObject.activeInHierarchy);

    private void OnEnable()
    {
        settings = GetComponentInParent<GameplaySettingsUI>();
        button.onClick.AddListener(Open);
        CosmeticsSave.Changed += RefreshBalance;
        RefreshBalance();
    }

    private void Start()
    {
        if (game.IsPuzzle) gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(Open);
        CosmeticsSave.Changed -= RefreshBalance;
    }

    private void LateUpdate() { button.interactable = CanOpen; }

    private void RefreshBalance() { balance.text = CosmeticsSave.Coins.ToString("N0", GameLanguage.Culture); }

    private void Open()
    {
        if (!CanOpen) return;
        AudioManager.Instance?.PlayButtonClickForced();
        if (store == null)
            store = CosmeticsStoreUI.Open(transform.parent, balance.font, "OYUNA DÖN");
        else
            store.Reopen();
    }

    public static void Install(RectTransform parent, GameManager manager, TMP_FontAsset font, Sprite coin)
    {
        var rect = Rect("Wallet", parent, new Vector2(48, -79), new Vector2(320, 96));
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = new Vector2(48, -79);
        var surface = rect.gameObject.AddComponent<RoundedPanelGraphic>();
        surface.SetStyle(new Color32(28, 21, 72, 245), new Color32(178, 146, 94, 255), 28);
        var walletButton = rect.gameObject.AddComponent<Button>();
        walletButton.targetGraphic = surface;

        var icon = Rect("Coin", rect, new Vector2(-116, 0), new Vector2(60, 60)).gameObject.AddComponent<Image>();
        icon.sprite = coin;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        var value = Text("CoinsValue", rect, font, new Vector2(-15, 0), new Vector2(142, 56), "0",
            37, new Color32(245, 213, 144, 255));
        value.enableAutoSizing = true;
        value.fontSizeMin = 23;
        value.fontSizeMax = 37;
        value.enableWordWrapping = false;

        var plus = Rect("Plus Background", rect, new Vector2(108, 0), new Vector2(70, 70));
        var plusSurface = plus.gameObject.AddComponent<RoundedPanelGraphic>();
        plusSurface.SetStyle(new Color32(64, 48, 116, 255), new Color32(157, 123, 255, 255), 20);
        plusSurface.raycastTarget = false;
        Text("Plus", plus, font, Vector2.zero, new Vector2(64, 64), "+", 48, new Color32(140, 234, 255, 255));

        var wallet = rect.gameObject.AddComponent<GameplayWalletUI>();
        wallet.game = manager;
        wallet.button = walletButton;
        wallet.balance = value;
    }

    private static TMP_Text Text(string name, RectTransform parent, TMP_FontAsset font, Vector2 position,
        Vector2 size, string caption, float fontSize, Color color)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = caption;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Rect(string name, RectTransform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }
}
