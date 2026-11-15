using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Temporary local wallet. Replace this adapter when the game economy is introduced.</summary>
public static class CosmeticsSave
{
    public const string Prefix = "GlowRings.Cosmetics.";
    public const int DemoStartingCoins = 2500;
    public static event Action Changed;
    public static int Coins
    {
        get { RecoverCoinReward(); return PlayerPrefs.GetInt(Prefix + "DemoCoins", DemoStartingCoins); }
    }
    public const string RewardJournalKey = Prefix + "CoinRewardJournal";
    [Serializable] private sealed class CoinRewardJournal
    { public string Id; public int BalanceAfter; public bool Settled; public string[] CompletedIds; }
    private static CoinRewardJournal ReadRewardJournal()
    {
        string json = PlayerPrefs.GetString(RewardJournalKey, "");
        return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<CoinRewardJournal>(json);
    }
    private static void RecoverCoinReward()
    {
        var journal = ReadRewardJournal();
        if (journal == null || journal.Settled) return;
        // Save the balance before marking the journal settled. Replaying after an
        // interruption writes the same target balance instead of adding twice.
        PlayerPrefs.SetInt(Prefix + "DemoCoins", journal.BalanceAfter); PlayerPrefs.Save();
        journal.Settled = true; PlayerPrefs.SetString(RewardJournalKey, JsonUtility.ToJson(journal)); PlayerPrefs.Save();
    }
    public static bool HasCoinReward(string id)
    {
        RecoverCoinReward(); var journal = ReadRewardJournal();
        return journal != null && ((journal.Id == id && journal.Settled) ||
            (journal.CompletedIds != null && Array.IndexOf(journal.CompletedIds, id) >= 0));
    }
    public static bool GrantCoinRewardOnce(string id, int amount)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0) return false;
        RecoverCoinReward();
        if (HasCoinReward(id)) return false;
        long total = (long)Coins + amount; if (total > int.MaxValue) return false;
        var previous = ReadRewardJournal();
        var history = new List<string>(previous != null && previous.CompletedIds != null ? previous.CompletedIds : Array.Empty<string>());
        if (previous != null && previous.Settled && !history.Contains(previous.Id)) history.Add(previous.Id);
        var journal = new CoinRewardJournal { Id = id, BalanceAfter = (int)total, CompletedIds = history.ToArray() };
        PlayerPrefs.SetString(RewardJournalKey, JsonUtility.ToJson(journal)); PlayerPrefs.Save();
        RecoverCoinReward(); Changed?.Invoke(); return true;
    }
    public static bool TrySpendCoins(int amount)
    {
        int balance = Coins;
        if (amount <= 0 || balance < amount) return false;
        PlayerPrefs.SetInt(Prefix + "DemoCoins", balance - amount);
        Commit();
        return true;
    }

    public static ThemeData MenuTheme => CosmeticsCatalog.Load()?.FindTheme("original");
    private static void Commit() { PlayerPrefs.Save(); Changed?.Invoke(); }
}
