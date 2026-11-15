using System;
using UnityEngine;

public static class PlayerProgressSave
{
    public const string StateKey = "GlowRings.PlayerProgress.v1";

    [Serializable]
    public sealed class Stats
    {
        public long Runs, Pieces, MatchingMoves, Rings, Points, AllClears, MultiMatches, Bombs, Changes;
        public int BestCombo;
    }

    public static Stats Read()
    {
        string json = PlayerPrefs.GetString(StateKey, "");
        return string.IsNullOrEmpty(json) ? new Stats() : JsonUtility.FromJson<Stats>(json) ?? new Stats();
    }

    public static void RecordPlacement(bool firstInRun)
    {
        var stats = Read();
        if (firstInRun) stats.Runs++;
        stats.Pieces++;
        Write(stats);
    }

    public static void RecordMatch(int rings, int points, int combo, int matches, bool allClear)
    {
        if (rings <= 0) return;
        var stats = Read();
        stats.MatchingMoves++;
        stats.Rings += rings;
        stats.Points += points;
        stats.BestCombo = Mathf.Max(stats.BestCombo, combo);
        if (matches > 1) stats.MultiMatches++;
        if (allClear) stats.AllClears++;
        Write(stats);
    }

    public static void RecordBooster(bool bomb)
    {
        var stats = Read();
        if (bomb) stats.Bombs++;
        else stats.Changes++;
        Write(stats);
    }

    private static void Write(Stats stats)
    {
        PlayerPrefs.SetString(StateKey, JsonUtility.ToJson(stats));
        PlayerPrefs.Save();
    }
}
