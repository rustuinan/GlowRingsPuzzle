using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>Native Google Play billing. Only paid PendingOrders can fulfill coin packs.</summary>
public sealed class CoinPurchaseService : MonoBehaviour
{
    private static CoinPurchaseService instance;
    public static CoinPurchaseService Instance
    {
        get
        {
            if (instance == null)
            {
                var root = new GameObject("Coin Purchase Service"); instance = root.AddComponent<CoinPurchaseService>();
                DontDestroyOnLoad(root);
            }
            return instance;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot() { Instance.Initialize(); }
    public event Action Changed;
    public event Action<int> Purchased;
    public string Status { get; private set; } = GameLanguage.Choose("Preparing Google Play...", "Google Play bağlantısı hazırlanıyor...");
    public bool Busy { get; private set; }
    private CoinShopConfig config;
    private StoreController store;
    private bool connecting, ready;
    private float connectStarted;

    public async void Initialize()
    {
        if (connecting || Busy) return;
        config = Resources.Load<CoinShopConfig>("CoinShopConfig");
        if (config == null || !config.IsValid) { SetStatus(GameLanguage.Choose("Products are currently unavailable.", "Paketler şu anda kullanılamıyor.")); return; }
        if (Application.isEditor || Application.platform != RuntimePlatform.Android)
        { SetStatus(GameLanguage.Choose("Purchases are available through Google Play on Android.", "Satın alımlar Android’de Google Play üzerinden yapılır.")); return; }
        connecting = true; ready = false; connectStarted = Time.realtimeSinceStartup;
        SetStatus(GameLanguage.Choose("Connecting to Google Play...", "Google Play'e bağlanılıyor..."));
        try
        {
            if (store == null)
            {
                store = UnityIAPServices.StoreController(GooglePlay.Name);
                store.ProcessPendingOrdersOnPurchasesFetched(false);
                store.OnStoreDisconnected += Disconnected;
                store.OnProductsFetched += ProductsFetched;
                store.OnProductsFetchFailed += ProductsFailed;
                store.OnPurchasesFetched += PurchasesFetched;
                store.OnPurchasesFetchFailed += PurchasesFailed;
                store.OnPurchasePending += Pending;
                store.OnPurchaseFailed += Failed;
                store.OnPurchaseDeferred += Deferred;
                store.OnPurchaseConfirmed += Confirmed;
            }
            await store.Connect();
            if (this == null) return;
            var products = config.Packs.Select(x => new ProductDefinition(x.ProductId, ProductType.Consumable)).ToList();
            products.Add(new ProductDefinition(config.NoAdsProductId, ProductType.NonConsumable));
            store.FetchProductsWithNoRetries(products);
        }
        catch (Exception error)
        {
            connecting = false; ready = false;
            SetStatus(GameLanguage.Choose("Could not connect to Google Play. Please try again.", "Google Play'e bağlanılamadı. Tekrar deneyebilirsin."));
            Debug.LogWarning("Coin shop connection failed: " + error.GetType().Name);
        }
    }
    private void Update()
    {
        if (connecting && Time.realtimeSinceStartup - connectStarted > 30)
        { connecting = false; ready = false; SetStatus(GameLanguage.Choose("Connection timed out. Please try again.", "Bağlantı zaman aşımına uğradı. Tekrar deneyebilirsin.")); }
    }
    public bool CanBuy(string id)
    {
        var product = ready && store != null ? store.GetProductById(id) : null;
        bool known = config != null && (config.Find(id) != null || (id == config.NoAdsProductId && !NoAdsSave.Owned));
        return !connecting && !Busy && known && product != null && product.availableToPurchase &&
            product.metadata != null && product.metadata.localizedPrice > 0 && !string.IsNullOrEmpty(product.metadata.localizedPriceString);
    }
    public string PriceLabel(string id)
    {
        if (config != null && id == config.NoAdsProductId && NoAdsSave.Owned) return GameLanguage.Choose("PURCHASED", "SATIN ALINDI");
        if (Busy) return GameLanguage.Choose("PROCESSING...", "İŞLEM SÜRÜYOR...");
        if (connecting) return GameLanguage.Choose("LOADING...", "YÜKLENİYOR...");
        if (CanBuy(id)) return store.GetProductById(id).metadata.localizedPriceString;
        return GameLanguage.Choose("UNAVAILABLE", "KULLANILAMIYOR");
    }
    public void Buy(string id)
    {
        if (!CanBuy(id)) return;
        Busy = true; SetStatus(GameLanguage.Choose("Opening Google Play checkout...", "Google Play ödeme ekranı açılıyor..."));
        try { store.PurchaseProduct(id); }
        catch (Exception) { Busy = false; SetStatus(GameLanguage.Choose("Could not start purchase. Please try again.", "Satın alma başlatılamadı. Tekrar deneyebilirsin.")); }
    }
    private void ProductsFetched(System.Collections.Generic.List<Product> products)
    {
        connecting = false; ready = true;
        SetStatus(products.Any(x => x.availableToPurchase) ? GameLanguage.Choose("Choose a pack to pay with Google Play.", "Paketini seç. Ödeme Google Play ile tamamlanır.") : GameLanguage.Choose("Products are currently unavailable.", "Paketler şu anda kullanılamıyor."));
        store.FetchPurchases();
    }
    private void ProductsFailed(ProductFetchFailed failure)
    { connecting = false; ready = false; SetStatus(GameLanguage.Choose("Could not load prices. Reopen the shop to try again.", "Paket fiyatları alınamadı. Bağlantıyı yenileyebilirsin.")); }
    private void PurchasesFetched(Orders orders)
    {
        // An empty/partial Google response must not revoke cached ownership while offline.
        if (orders.ConfirmedOrders.Any(x => x.CartOrdered.Items().Any(item => item.Product.definition.id == config.NoAdsProductId)))
            NoAdsSave.Grant();
        foreach (var order in orders.PendingOrders) Pending(order);
        Changed?.Invoke();
    }
    private void PurchasesFailed(PurchasesFetchFailureDescription failure)
    { SetStatus(GameLanguage.Choose("Could not check previous purchases. Reopen the shop to try again.", "Önceki işlemler kontrol edilemedi. Bağlantıyı yenileyebilirsin.")); }
    private void Pending(PendingOrder order)
    {
        if (order == null || order.Info == null || string.IsNullOrEmpty(order.Info.Receipt))
        { Busy = false; SetStatus(GameLanguage.Choose("Could not verify the purchase. Reopen the shop to try again.", "İşlem doğrulanamadı. Lütfen bağlantıyı yenile.")); return; }
        var items = order.CartOrdered.Items().ToArray();
        if (items.Length == 1 && items[0].Product.definition.id == config.NoAdsProductId && items[0].Quantity == 1 &&
            !string.IsNullOrWhiteSpace(order.Info.TransactionID))
        {
            NoAdsSave.Grant(); Busy = false;
            SetStatus(GameLanguage.Choose("No Ads is active. Interstitial ads are disabled.", "Reklamsız paket aktif! Bölüm arası reklamlar kapatıldı."));
            try { store.ConfirmPurchase(order); }
            catch (Exception) { SetStatus(GameLanguage.Choose("No Ads is active. Confirmation will resume after reconnecting.", "Reklamsız paket aktif. İşlem onayı bağlantı yenilendiğinde tamamlanacak.")); }
            return;
        }
        bool delivered = CoinPurchaseFulfillment.TryGrant(config, order.Info.TransactionID,
            order.CartOrdered.Items().Select(x => (x.Product.definition.id, x.Quantity)), out int coins, out bool newlyGranted);
        Busy = false;
        if (!delivered) { SetStatus(GameLanguage.Choose("Delivery is pending. Reopen the shop to try again.", "Satın alımın teslimatı bekliyor. Lütfen bağlantıyı yenile.")); return; }
        SetStatus(GameLanguage.Format("+{0:N0} COINS ADDED!", "+{0:N0} COIN BAKİYENE EKLENDİ!", coins));
        if (newlyGranted) Purchased?.Invoke(coins);
        // Only consume/acknowledge after the wallet and receipt are durably saved.
        try { store.ConfirmPurchase(order); }
        catch (Exception) { SetStatus(GameLanguage.Choose("Coins added. Confirmation will resume after reconnecting.", "Coinlerin eklendi. İşlem onayı bağlantı yenilendiğinde tamamlanacak.")); }
    }
    private void Confirmed(Order order)
    {
        if (order is FailedOrder) SetStatus(GameLanguage.Choose("Purchase saved. Confirmation will resume after reconnecting.", "Satın alımın kaydedildi. İşlem onayı bağlantı yenilendiğinde tamamlanacak."));
    }
    private void Failed(FailedOrder order)
    {
        Busy = false;
        SetStatus(order.FailureReason == PurchaseFailureReason.UserCancelled ? GameLanguage.Choose("Purchase cancelled.", "Satın alma iptal edildi. Coin eklenmedi.") : GameLanguage.Choose("Purchase failed. Please try again.", "Satın alma tamamlanamadı. Tekrar deneyebilirsin."));
    }
    private void Deferred(DeferredOrder order)
    { Busy = false; SetStatus(GameLanguage.Choose("Payment pending. Coins will be added after confirmation.", "Ödeme onayı bekleniyor. Onaylandığında coinlerin eklenecek.")); }
    private void Disconnected(StoreConnectionFailureDescription failure)
    { connecting = false; ready = false; Busy = false; SetStatus(GameLanguage.Choose("Google Play disconnected. Please try again.", "Google Play bağlantısı kesildi. Tekrar deneyebilirsin.")); }
    private void SetStatus(string value) { Status = value; Changed?.Invoke(); }
    private void OnDestroy()
    {
        if (store != null)
        {
            store.OnStoreDisconnected -= Disconnected; store.OnProductsFetched -= ProductsFetched;
            store.OnProductsFetchFailed -= ProductsFailed; store.OnPurchasesFetched -= PurchasesFetched;
            store.OnPurchasesFetchFailed -= PurchasesFailed; store.OnPurchasePending -= Pending;
            store.OnPurchaseFailed -= Failed; store.OnPurchaseDeferred -= Deferred; store.OnPurchaseConfirmed -= Confirmed;
        }
        if (instance == this) instance = null;
    }
}
