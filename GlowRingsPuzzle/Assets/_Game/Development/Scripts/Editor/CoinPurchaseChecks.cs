using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Purchasing;
using System.Reflection;

public static class CoinPurchaseChecks
{
    [MenuItem("Tools/Glow Rings/Validate Coin Purchases (Edit Mode)")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run outside Play Mode.");
        string balanceKey = CosmeticsSave.Prefix + "DemoCoins";
        bool hadBalance = PlayerPrefs.HasKey(balanceKey); int balance = PlayerPrefs.GetInt(balanceKey);
        bool hadJournal = PlayerPrefs.HasKey(CosmeticsSave.RewardJournalKey);
        string journal = PlayerPrefs.GetString(CosmeticsSave.RewardJournalKey);
        bool hadNoAds = PlayerPrefs.HasKey(NoAdsSave.OwnershipKey); int noAds = PlayerPrefs.GetInt(NoAdsSave.OwnershipKey);
        var config = Resources.Load<CoinShopConfig>("CoinShopConfig");
        try
        {
            Require(config != null && config.IsValid && config.Packs.Length == 3, "Coin catalog must be valid.");
            Require(config.Packs.All(x => x.Icon != null), "Each pack needs artwork.");
            Require(config.NoAdsIcon != null && config.CoinIcon != null, "Wallet and No Ads need icons.");
            PlayerPrefs.DeleteKey(CosmeticsSave.RewardJournalKey); PlayerPrefs.SetInt(balanceKey, 100); PlayerPrefs.Save();
            var cart = new[] { (config.Packs[0].ProductId, 1) };
            Require(CoinPurchaseFulfillment.TryGrant(config, "test-transaction-1", cart, out int amount, out bool isNew) &&
                isNew && amount == config.Packs[0].Coins && CosmeticsSave.Coins == 100 + amount, "Confirmed order must pay the exact pack amount.");
            int paid = CosmeticsSave.Coins;
            Require(CoinPurchaseFulfillment.TryGrant(config, "test-transaction-1", cart, out _, out isNew) && !isNew && CosmeticsSave.Coins == paid,
                "Store redelivery must not duplicate coins.");
            Require(CoinPurchaseFulfillment.TryGrant(config, "test-transaction-2", cart, out _, out isNew) && isNew && CosmeticsSave.Coins == paid + config.Packs[0].Coins,
                "A second transaction of the same consumable must pay again.");
            paid = CosmeticsSave.Coins;
            Require(!CoinPurchaseFulfillment.TryGrant(config, "", cart, out _, out _) &&
                !CoinPurchaseFulfillment.TryGrant(config, "unknown", new[] { ("unknown-product", 1) }, out _, out _) &&
                !CoinPurchaseFulfillment.TryGrant(config, "zero", new[] { (config.Packs[0].ProductId, 0) }, out _, out _) &&
                !CoinPurchaseFulfillment.TryGrant(config, "huge", new[] { (config.Packs[0].ProductId, int.MaxValue) }, out _, out _) && CosmeticsSave.Coins == paid,
                "Unknown products, empty receipts, invalid quantities and overflow must never grant coins.");
            PlayerPrefs.SetInt(balanceKey, int.MaxValue); PlayerPrefs.Save();
            Require(!CoinPurchaseFulfillment.TryGrant(config, "full-wallet", cart, out _, out _) && CosmeticsSave.Coins == int.MaxValue,
                "A full wallet must not overflow or acknowledge an undelivered pack.");
            PlayerPrefs.SetInt(balanceKey, paid); PlayerPrefs.Save();
            string pendingReceipt = "google-play-coins-test-interrupted";
            PlayerPrefs.SetString(CosmeticsSave.RewardJournalKey, "{\"Id\":\"" + pendingReceipt + "\",\"BalanceAfter\":" + (paid + config.Packs[0].Coins) + ",\"Settled\":false}");
            PlayerPrefs.Save();
            Require(CoinPurchaseFulfillment.TryGrant(config, "test-interrupted", cart, out _, out isNew) && !isNew && CosmeticsSave.Coins == paid + config.Packs[0].Coins,
                "Interrupted wallet writes must recover exactly once.");
            CheckNoAds(config);
            Debug.Log("CoinPurchaseChecks PASS: catalog/artwork, exact grant, repeated consumables, duplicate receipt, invalid orders, quantity/coin overflow and interrupted fulfillment.");
        }
        finally
        {
            if (hadBalance) PlayerPrefs.SetInt(balanceKey, balance); else PlayerPrefs.DeleteKey(balanceKey);
            if (hadJournal) PlayerPrefs.SetString(CosmeticsSave.RewardJournalKey, journal); else PlayerPrefs.DeleteKey(CosmeticsSave.RewardJournalKey);
            if (hadNoAds) PlayerPrefs.SetInt(NoAdsSave.OwnershipKey, noAds); else PlayerPrefs.DeleteKey(NoAdsSave.OwnershipKey);
            PlayerPrefs.Save();
        }
    }
    private sealed class OrderDetails : IOrderInfo
    {
        public IAppleOrderInfo Apple => null;
        public IGoogleOrderInfo Google => null;
        public IPaymentProvidersOrderInfo PaymentProviders => null;
        public List<IPurchasedProductInfo> PurchasedProductInfo { get; set; }
        public string Receipt { get; set; }
        public string TransactionID { get; set; }
    }
    private static void CheckNoAds(CoinShopConfig config)
    {
        var root = new GameObject("No Ads Validation");
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        try
        {
            PlayerPrefs.DeleteKey(NoAdsSave.OwnershipKey); PlayerPrefs.Save();
            int balance = CosmeticsSave.Coins;
            var service = root.AddComponent<CoinPurchaseService>();
            typeof(CoinPurchaseService).GetField("config", flags).SetValue(service, config);
            var product = (Product)Activator.CreateInstance(typeof(Product), flags, null,
                new object[] { new ProductDefinition(config.NoAdsProductId, ProductType.NonConsumable), new ProductMetadata(), true }, null);
            var cart = new Cart(new List<CartItem> { new CartItem(product) });
            var info = new OrderDetails { TransactionID = "no-ads-test-receipt", Receipt = "test-paid-receipt" };
            var pending = new PendingOrder(cart, info);
            var deliver = typeof(CoinPurchaseService).GetMethod("Pending", flags);
            deliver.Invoke(service, new object[] { new PendingOrder(cart, new OrderDetails { TransactionID = "missing", Receipt = "" }) });
            Require(!NoAdsSave.Owned, "An order without a receipt must not remove ads.");
            deliver.Invoke(service, new object[] { pending });
            Require(NoAdsSave.Owned && CosmeticsSave.Coins == balance && !service.CanBuy(config.NoAdsProductId),
                "No Ads must grant ownership without changing coins or enabling another purchase.");
            deliver.Invoke(service, new object[] { pending });
            Require(NoAdsSave.Owned && CosmeticsSave.Coins == balance, "No Ads redelivery must be idempotent.");
            var ads = root.AddComponent<AdMobManager>();
            Require(!ads.ShowInterstitialAfterLevelStart(), "No Ads owners must not see interstitials.");
            PlayerPrefs.DeleteKey(NoAdsSave.OwnershipKey); PlayerPrefs.Save();
            var restored = new Orders(new List<ConfirmedOrder> { new ConfirmedOrder(cart, info) }, new List<PendingOrder>(), new List<DeferredOrder>());
            typeof(CoinPurchaseService).GetMethod("PurchasesFetched", flags).Invoke(service, new object[] { restored });
            Require(NoAdsSave.Owned && CosmeticsSave.Coins == balance, "Restored non-consumables must restore No Ads without granting coins.");
            typeof(CoinPurchaseService).GetMethod("PurchasesFetched", flags).Invoke(service,
                new object[] { new Orders(new List<ConfirmedOrder>(), new List<PendingOrder>(), new List<DeferredOrder>()) });
            Require(NoAdsSave.Owned, "An empty/partial store result must not revoke cached No Ads ownership.");
            Debug.Log("NoAdsChecks PASS: receipt guard, ownership, duplicate delivery, restore, offline persistence, interstitial suppression and unchanged coin balance.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException("CoinPurchaseChecks: " + message); }
}
