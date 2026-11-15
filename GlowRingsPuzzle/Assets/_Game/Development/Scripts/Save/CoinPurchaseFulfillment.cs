using System;
using System.Collections.Generic;

public static class CoinPurchaseFulfillment
{
    public static bool TryGrant(CoinShopConfig config, string transactionId, IEnumerable<(string id, int quantity)> items, out int amount, out bool newlyGranted)
    {
        amount = 0; newlyGranted = false;
        if (config == null || !config.IsValid || string.IsNullOrWhiteSpace(transactionId) || items == null) return false;
        long total = 0;
        foreach (var item in items)
        {
            var pack = config.Find(item.id);
            if (pack == null || item.quantity < 1) return false;
            total += (long)pack.Coins * item.quantity;
            if (total > int.MaxValue) return false;
        }
        if (total < 1) return false;
        string receipt = "google-play-coins-" + transactionId;
        amount = (int)total;
        if (CosmeticsSave.HasCoinReward(receipt)) return true;
        newlyGranted = CosmeticsSave.GrantCoinRewardOnce(receipt, amount);
        return CosmeticsSave.HasCoinReward(receipt);
    }
}
