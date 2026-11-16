using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Coin packs and the permanent ad removal purchase.</summary>
public sealed class CosmeticsStoreUI : MonoBehaviour
{
    private static readonly Color White = new Color32(242, 243, 255, 255);
    private static readonly Color Muted = new Color32(173, 170, 204, 255);
    private static readonly Color Gold = new Color32(245, 211, 143, 255);
    private static readonly Color Dark = new Color32(13, 10, 34, 255);
    private CosmeticsCatalog catalog;
    private TMP_FontAsset font;
    private RectTransform frame;
    private RectTransform page;
    private Color secondary;
    private Color surface;
    private string returnCaption;
    private string message;
    private TMP_Text walletLabel;
    private TMP_Text purchaseStatus;
    private CoinShopConfig coinShop;
    private CoinPurchaseService purchaseService;
    private readonly System.Collections.Generic.Dictionary<string, Button> purchaseButtons = new System.Collections.Generic.Dictionary<string, Button>();
    private readonly System.Collections.Generic.Dictionary<string, TMP_Text> purchasePrices = new System.Collections.Generic.Dictionary<string, TMP_Text>();

    public static CosmeticsStoreUI Open(Transform parent, TMP_FontAsset font, string returnCaption = "MENÜYE DÖN")
    {
        var root = Rect("Coin Store", parent, Vector2.zero, Vector2.zero); Stretch(root);
        var store = root.gameObject.AddComponent<CosmeticsStoreUI>();
        store.returnCaption = returnCaption;
        store.coinShop = Resources.Load<CoinShopConfig>("CoinShopConfig");
        store.font = store.coinShop != null && store.coinShop.UIFont != null ? store.coinShop.UIFont : font; store.catalog = CosmeticsCatalog.Load();
        store.purchaseService = CoinPurchaseService.Instance;
        store.purchaseService.Changed += store.RefreshPurchases;
        store.purchaseService.Purchased += store.OnPurchased;
        CosmeticsSave.Changed += store.RefreshWallet;
        root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.48f);
        store.frame = Rect("Coin Store Reference Frame", root, Vector2.zero, new Vector2(1080, 1920));
        store.frame.gameObject.AddComponent<ReferenceFrameFit>();
        store.BuildShop(); return store;
    }
    public void Reopen() { gameObject.SetActive(true); transform.SetAsLastSibling(); message = null; BuildShop(); }
    private void OnDestroy()
    {
        CosmeticsSave.Changed -= RefreshWallet;
        if (purchaseService != null) { purchaseService.Changed -= RefreshPurchases; purchaseService.Purchased -= OnPurchased; }
    }
    public void Close() { Click(); gameObject.SetActive(false); }
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        Close();
    }
    private void ClearPage()
    {
        purchaseButtons.Clear(); purchasePrices.Clear(); purchaseStatus = null; walletLabel = null;
        if (page != null) { page.gameObject.SetActive(false); Dispose(page.gameObject); }
        ThemeData palette = catalog != null ? catalog.FindTheme("midnight") : null;
        secondary = palette != null ? palette.Secondary : new Color32(157, 123, 255, 255);
        surface = Color.Lerp(Dark, palette != null ? palette.Surface : secondary, 0.58f);
        page = Rect("Shop Page", frame, Vector2.zero, new Vector2(1080, 1920));
        page.localScale = Vector3.one * 0.86f;
        var window = page.gameObject.AddComponent<RoundedPanelGraphic>();
        window.SetStyle(new Color32(20, 17, 45, 255), Dim(secondary, 0.6f), 48);
        window.raycastTarget = true;
    }
    private void Header()
    {
        Label(page, "MAĞAZA", -275, 850, 410, 90, 64, White, TextAlignmentOptions.Left);
        ButtonAt(page, "X", 432, 850, 98, 98, surface, Dim(secondary, 0.4f), Close);
        var wallet = Box("Coin Balance", page, 175, 850, 320, 88,
            new Color32(53, 42, 49, 255), Dim(Gold, 0.45f), 25);
        var icon = Rect("Wallet Coin Icon", wallet, new Vector2(-108, 0), new Vector2(52, 52));
        var image = icon.gameObject.AddComponent<Image>(); image.sprite = coinShop != null ? coinShop.CoinIcon : null; image.preserveAspect = true; image.raycastTarget = false;
        walletLabel = Label(wallet, CosmeticsSave.Coins.ToString("N0", GameLanguage.Culture), 34, 0, 205, 66, 38, Gold);
    }
    private void BuildShop()
    {
        ClearPage();
        Header();
        purchaseStatus = Body(page, "", 0, 700, 930, 106, 35, White);
        const float top = 630, bottom = -642;
        var viewport = Rect("Coin Shop Scroll", page, new Vector2(0, (top + bottom) * 0.5f), new Vector2(960, top - bottom));
        viewport.gameObject.AddComponent<Image>().color = Color.clear; viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 55;
        var content = Rect("Coin Packages", viewport, Vector2.zero, Vector2.zero);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
        int count = coinShop != null && coinShop.Packs != null ? coinShop.Packs.Length : 0;
        content.sizeDelta = new Vector2(0, Mathf.Max(top - bottom, 472 + count * 366));
        scroll.viewport = viewport; scroll.content = content;
        if (coinShop != null) NoAdsProduct(content);
        for (int i = 0; i < count; i++)
        {
            var pack = coinShop.Packs[i];
            var row = Box("Coin Package " + pack.ProductId, content, 0, -630 - i * 366, 944, 334, surface, Dim(secondary, 0.4f), 30);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1);
            var art = Rect("Package Icon", row, new Vector2(-330, 0), new Vector2(180, 180));
            var image = art.gameObject.AddComponent<Image>(); image.sprite = pack.Icon; image.preserveAspect = true; image.raycastTarget = false;
            Label(row, pack.Title, 113, 101, 630, 62, 40, White);
            Label(row, "+" + pack.Coins.ToString("N0", GameLanguage.Culture) + " " + GameLanguage.Text("COINS"), 113, 14, 630, 76, 52, Gold);
            string productId = pack.ProductId;
            var button = ButtonAt(row, "", 113, -105, 630, 90, Color.Lerp(surface, secondary, 0.3f), Dim(Gold, 0.7f),
                () => { Click(); message = null; purchaseService.Buy(productId); });
            purchaseButtons[productId] = button; purchasePrices[productId] = button.GetComponentInChildren<TMP_Text>();
        }
        Body(page, "Diğer paketleri görmek için kaydır", 0, -687, 930, 42, 30, Muted);
        ButtonAt(page, returnCaption, 0, -806, 930, 100, surface, Dim(secondary, 0.6f), Close);
        Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1;
        RefreshPurchases(); purchaseService.Initialize();
    }
    private void NoAdsProduct(RectTransform content)
    {
        var row = Box("No Ads Package", content, 0, -219, 944, 438,
            new Color32(51, 31, 87, 255), Dim(Gold, 0.65f), 30);
        row.anchorMin = row.anchorMax = new Vector2(0.5f, 1);
        var icon = Rect("No Ads Icon", row, new Vector2(-330, 18), new Vector2(180, 200));
        var image = icon.gameObject.AddComponent<Image>(); image.sprite = coinShop.NoAdsIcon; image.preserveAspect = true; image.raycastTarget = false;
        Label(row, "REKLAMSIZ", 113, 149, 630, 64, 46, White);
        Body(row, "Bölüm arası reklamları kaldırır.\nÖdüllü reklamlar isteğe bağlı kalır.", 113, 28, 630, 142, 36, White);
        string id = coinShop.NoAdsProductId;
        var button = ButtonAt(row, "", 113, -137, 630, 92, Color.Lerp(surface, secondary, 0.3f), Dim(Gold, 0.7f),
            () => { Click(); message = null; purchaseService.Buy(id); });
        purchaseButtons[id] = button; purchasePrices[id] = button.GetComponentInChildren<TMP_Text>();
    }
    private void RefreshWallet() { if (walletLabel != null) walletLabel.text = CosmeticsSave.Coins.ToString("N0", GameLanguage.Culture); }
    private void OnPurchased(int coins)
    {
        message = GameLanguage.Format("+{0:N0} COINS ADDED!", "+{0:N0} COIN BAKİYENE EKLENDİ!", coins);
        RefreshWallet(); RefreshPurchases();
    }
    private void RefreshPurchases()
    {
        if (purchaseService == null) return;
        if (purchaseStatus != null) { purchaseStatus.text = message ?? purchaseService.Status; purchaseStatus.color = message != null ? Gold : Muted; }
        foreach (var pair in purchaseButtons)
        {
            if (pair.Value == null) continue;
            pair.Value.interactable = purchaseService.CanBuy(pair.Key);
            purchasePrices[pair.Key].text = purchaseService.PriceLabel(pair.Key).Replace('\u202F', '\u00A0');
        }
    }
    private static void Click() { if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClickForced(); }
    private static Color Dim(Color color, float alpha) { color.a = alpha; return color; }
    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
    }
    private static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static RectTransform Box(string name, Transform parent, float x, float y, float width, float height, Color fill, Color border, float radius)
    {
        var rect = Rect(name, parent, new Vector2(x, y), new Vector2(width, height));
        var graphic = rect.gameObject.AddComponent<RoundedPanelGraphic>(); graphic.SetStyle(fill, border, radius); graphic.raycastTarget = false;
        return rect;
    }
    private TMP_Text Label(Transform parent, string text, float x, float y, float width, float height, float size,
        Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var rect = Rect(text, parent, new Vector2(x, y), new Vector2(width, height));
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font;
        label.text = LocalizeShopText(text); label.fontSize = size; label.fontStyle = FontStyles.Normal;
        label.characterSpacing = 2.2f; label.lineSpacing = 8;
        label.alignment = alignment; label.color = color; label.raycastTarget = false;
        label.enableWordWrapping = true; label.enableAutoSizing = true; label.fontSizeMin = size * 0.82f; label.fontSizeMax = size;
        return label;
    }
    private static string LocalizeShopText(string text)
    {
        switch (text)
        {
            case "MAĞAZA": return GameLanguage.Choose("SHOP", text);
            case "MENÜYE DÖN": return GameLanguage.Choose("BACK TO MENU", text);
            case "OYUNA DÖN": return GameLanguage.Choose("BACK TO GAME", text);
            case "REKLAMSIZ": return GameLanguage.Choose("NO ADS", text);
            case "COIN KESESİ": return GameLanguage.Choose("COIN POUCH", text);
            case "COIN ÇANTASI": return GameLanguage.Choose("COIN BAG", text);
            case "COIN SANDIĞI": return GameLanguage.Choose("COIN CHEST", text);
            case "Diğer paketleri görmek için kaydır": return GameLanguage.Choose("Scroll to see more packs", text);
            case "Bölüm arası reklamları kaldırır.\nÖdüllü reklamlar isteğe bağlı kalır.":
                return GameLanguage.Choose("Removes interstitial ads.\nRewarded ads remain optional.", text);
            default: return text;
        }
    }

    private TMP_Text Body(Transform parent, string text, float x, float y, float width, float height, float size, Color color)
    {
        var label = Label(parent, text, x, y, width, height, size, color);
        StyleBody(label);
        return label;
    }

    private void StyleBody(TMP_Text label)
    {
        var regular = Resources.Load<TMP_FontAsset>(GameLanguage.IsTurkish ? "Shop Body Turkish SDF" : "Fonts & Materials/LiberationSans SDF - Fallback");
        if (regular != null) label.font = regular;
        else if (coinShop != null && coinShop.BodyFont != null) label.font = coinShop.BodyFont;
        label.characterSpacing = 0;
        label.lineSpacing = 10;
    }
    private Button ButtonAt(Transform parent, string caption, float x, float y, float width, float height, Color color, Color edge, Action action)
    {
        var rect = Box(caption + " Button", parent, x, y, width, height, color, edge, Mathf.Min(26, height * 0.5f));
        var graphic = rect.GetComponent<RoundedPanelGraphic>(); graphic.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = graphic;
        var colors = button.colors; colors.pressedColor = new Color(0.78f, 0.78f, 0.88f, 1);
        colors.disabledColor = new Color(0.65f, 0.65f, 0.75f, 0.8f); button.colors = colors;
        button.onClick.AddListener(() => action()); rect.gameObject.AddComponent<MenuButtonFeedback>();
        Label(rect, caption, 0, 0, width - 44, height - 14, height >= 80 ? 37 : 30, White);
        return button;
    }
    private static void Dispose(UnityEngine.Object value)
    { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
}
