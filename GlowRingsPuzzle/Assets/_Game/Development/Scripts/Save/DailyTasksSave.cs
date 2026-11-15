using System;
using UnityEngine;

public static class DailyTasksSave
{
    public const string StateKey = "GlowRings.DailyTasks.State";
    public enum Task { PlacePieces, ClearRings, EarnScore }

    [Serializable]
    public sealed class State
    {
        public long Day;
        public int[] Progress = new int[3];
        public int Claimed;
    }

    public static int Goal(Task task)
    {
        switch (task)
        {
            case Task.PlacePieces: return 15;
            case Task.ClearRings: return 18;
            default: return 600;
        }
    }

    public static int Reward(Task task)
    {
        switch (task)
        {
            case Task.PlacePieces: return 50;
            case Task.ClearRings: return 75;
            default: return 100;
        }
    }

    public static State Read(DateTime localNow)
    {
        var state = Load();
        return state.Day == localNow.Date.Ticks ? state : new State { Day = localNow.Date.Ticks };
    }

    public static bool WasClaimed(State state, Task task)
    {
        return (state.Claimed & (1 << (int)task)) != 0;
    }

    public static void Record(int pieces, int rings, int score, DateTime localNow)
    {
        var state = ForWrite(localNow);
        if (state == null) return;

        bool changed = Add(state, Task.PlacePieces, pieces);
        changed |= Add(state, Task.ClearRings, rings);
        changed |= Add(state, Task.EarnScore, score);
        if (changed) Write(state);
    }

    public static int Claim(Task task, DateTime localNow)
    {
        int index = (int)task;
        if (index < 0 || index >= 3) return 0;
        var state = ForWrite(localNow);
        if (state == null || WasClaimed(state, task) || state.Progress[index] < Goal(task)) return 0;

        string id = "daily-task-" + state.Day + "-" + index;
        int reward = Reward(task);
        // The date and task form a stable wallet receipt, including after interrupted claims.
        bool granted = CosmeticsSave.GrantCoinRewardOnce(id, reward);
        if (!granted && !CosmeticsSave.HasCoinReward(id)) return 0;
        state.Claimed |= 1 << index;
        Write(state);
        return granted ? reward : 0;
    }

    private static State ForWrite(DateTime localNow)
    {
        var state = Load();
        long day = localNow.Date.Ticks;
        if (state.Day > day) return null;
        return state.Day == day ? state : new State { Day = day };
    }

    private static State Load()
    {
        string json = PlayerPrefs.GetString(StateKey, "");
        var state = string.IsNullOrEmpty(json) ? new State() : JsonUtility.FromJson<State>(json) ?? new State();
        if (state.Progress == null || state.Progress.Length != 3) Array.Resize(ref state.Progress, 3);
        return state;
    }

    private static bool Add(State state, Task task, int amount)
    {
        int index = (int)task;
        if (amount <= 0 || state.Progress[index] >= Goal(task)) return false;
        state.Progress[index] = (int)Math.Min(Goal(task), (long)state.Progress[index] + amount);
        return true;
    }

    private static void Write(State state)
    {
        PlayerPrefs.SetString(StateKey, JsonUtility.ToJson(state));
        PlayerPrefs.Save();
    }
}
