using System;
using UnityEngine;

/// <summary>One claim per local calendar day. Missed days preserve progression.</summary>
public static class DailyRewardSave
{
    public const string StateKey = "GlowRings.DailyReward.State";
    [Serializable] public sealed class State
    {
        public int ClaimedDays;
        public long LastClaimDate;
        public int LastReward;
        public string PendingId;
        public int PendingAmount;
        public long PendingDate;
    }
    public static State Read()
    {
        string json = PlayerPrefs.GetString(StateKey, "");
        return string.IsNullOrEmpty(json) ? new State() : JsonUtility.FromJson<State>(json) ?? new State();
    }
    public static bool CanClaim(DateTime localNow)
    {
        var state = Read();
        return string.IsNullOrEmpty(state.PendingId) && localNow.Date.Ticks > state.LastClaimDate;
    }
    public static int NextDay => (int)Math.Min(int.MaxValue, (long)Read().ClaimedDays + 1);
    public static int Claim(DailyRewardConfig config, DateTime localNow)
    {
        var state = Read();
        if (!string.IsNullOrEmpty(state.PendingId)) return Recover();
        if (config == null || !config.IsValid || !CanClaim(localNow)) return 0;
        state.PendingId = "daily-reward-" + Guid.NewGuid().ToString("N");
        state.PendingAmount = config.CoinsForDay(NextDay);
        state.PendingDate = localNow.Date.Ticks;
        Write(state); // Persist the exact reward before touching the wallet.
        return Recover();
    }
    public static int Recover()
    {
        var state = Read();
        if (string.IsNullOrEmpty(state.PendingId) || state.PendingAmount <= 0) return 0;
        CosmeticsSave.GrantCoinRewardOnce(state.PendingId, state.PendingAmount);
        if (!CosmeticsSave.HasCoinReward(state.PendingId)) return 0;
        int amount = state.PendingAmount;
        state.ClaimedDays = (int)Math.Min(int.MaxValue, (long)state.ClaimedDays + 1);
        state.LastClaimDate = state.PendingDate; state.LastReward = amount;
        state.PendingId = null; state.PendingAmount = 0; state.PendingDate = 0;
        Write(state); return amount;
    }
    private static void Write(State state)
    { PlayerPrefs.SetString(StateKey, JsonUtility.ToJson(state)); PlayerPrefs.Save(); }
}
