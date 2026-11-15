using System;
using UnityEngine;

public static class DailySpinSave
{
    public const string StateKey = "GlowRings.DailySpin.State";
    [Serializable] public sealed class State
    {
        public string Id;
        public long ReadyUtcTicks;
        public int[] Rewards;
        public float StopAngle;
        public bool Pending;
        public int LastReward;
    }
    public static State Read()
    {
        string json = PlayerPrefs.GetString(StateKey, "");
        return string.IsNullOrEmpty(json) ? new State() : JsonUtility.FromJson<State>(json) ?? new State();
    }
    public static bool CanSpin(DateTime utcNow, DailySpinConfig config = null)
    {
        config = config != null ? config : Resources.Load<DailySpinConfig>("DailySpinConfig");
        var state = Read();
        return !state.Pending && ((config != null && config.UnlimitedTestSpins) || utcNow.Ticks >= state.ReadyUtcTicks);
    }
    public static TimeSpan Remaining(DateTime utcNow)
    {
        var config = Resources.Load<DailySpinConfig>("DailySpinConfig");
        return config != null && config.UnlimitedTestSpins ? TimeSpan.Zero : TimeSpan.FromTicks(Math.Max(0, Read().ReadyUtcTicks - utcNow.Ticks));
    }
    public static int SegmentAtPointer(float rotation, int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        float step = 360f / count;
        return Mathf.FloorToInt(Mathf.Repeat(rotation + step * 0.5f, 360) / step) % count;
    }
    public static bool TryBegin(DailySpinConfig config, DateTime utcNow, out State state)
    {
        state = Read();
        if (config == null || !config.IsValid() || !CanSpin(utcNow, config)) return false;
        int index = UnityEngine.Random.Range(0, config.Rewards.Length);
        float step = 360f / config.Rewards.Length;
        state = new State
        {
            Id = Guid.NewGuid().ToString("N"), Pending = true,
            ReadyUtcTicks = utcNow.AddHours(config.CooldownHours).Ticks,
            Rewards = (int[])config.Rewards.Clone(),
            StopAngle = index * step + UnityEngine.Random.Range(-step * 0.2f, step * 0.2f)
        };
        Write(state); return true;
    }
    public static int Complete()
    {
        var state = Read();
        if (!state.Pending || state.Rewards == null || state.Rewards.Length < 1) return 0;
        int amount = state.Rewards[SegmentAtPointer(state.StopAngle, state.Rewards.Length)];
        if (amount <= 0) return 0;
        CosmeticsSave.GrantCoinRewardOnce(state.Id, amount);
        if (!CosmeticsSave.HasCoinReward(state.Id)) return 0;
        state.Pending = false; state.LastReward = amount; Write(state); return amount;
    }
    private static void Write(State state) { PlayerPrefs.SetString(StateKey, JsonUtility.ToJson(state)); PlayerPrefs.Save(); }
}
