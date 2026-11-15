using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DailyRewardChecks
{
    private sealed class SavedPreferences
    {
        private readonly string balanceKey = CosmeticsSave.Prefix + "DemoCoins";
        private readonly bool hadBalance;
        private readonly int balance;
        private readonly Dictionary<string, (bool exists, string value)> strings;
        public SavedPreferences()
        {
            hadBalance = PlayerPrefs.HasKey(balanceKey); balance = PlayerPrefs.GetInt(balanceKey);
            strings = new[] { DailyRewardSave.StateKey, CosmeticsSave.RewardJournalKey }
                .ToDictionary(x => x, x => (PlayerPrefs.HasKey(x), PlayerPrefs.GetString(x)));
        }
        public void Clear()
        {
            foreach (var key in strings.Keys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.SetInt(balanceKey, 100); PlayerPrefs.Save();
        }
        public void Restore()
        {
            foreach (var pair in strings)
            { if (pair.Value.exists) PlayerPrefs.SetString(pair.Key, pair.Value.value); else PlayerPrefs.DeleteKey(pair.Key); }
            if (hadBalance) PlayerPrefs.SetInt(balanceKey, balance); else PlayerPrefs.DeleteKey(balanceKey);
            PlayerPrefs.Save();
        }
    }
    [MenuItem("Tools/Glow Rings/Validate Daily Reward (Edit Mode)")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run outside Play Mode.");
        var saved = new SavedPreferences();
        var config = ScriptableObject.CreateInstance<DailyRewardConfig>();
        try
        {
            saved.Clear(); var now = new DateTime(2026, 11, 3, 23, 59, 0);
            Require(DailyRewardSave.CanClaim(now) && DailyRewardSave.NextDay == 1, "First day must be available.");
            Require(DailyRewardSave.Claim(config, now) == 100 && CosmeticsSave.Coins == 200, "First reward must pay 100.");
            Require(!DailyRewardSave.CanClaim(now) && DailyRewardSave.Claim(config, now) == 0, "Same-day claims must be blocked.");
            Require(!DailyRewardSave.CanClaim(now.AddDays(-1)), "Clock rollback must not unlock a reward.");
            now = now.AddMinutes(2);
            Require(DailyRewardSave.CanClaim(now) && DailyRewardSave.NextDay == 2, "Local midnight must unlock the next day.");
            Require(DailyRewardSave.Claim(config, now) == 150 && CosmeticsSave.Coins == 350, "Second day must increase the reward.");
            Require(DailyRewardSave.Claim(config, now.AddDays(6)) == 200 && DailyRewardSave.Read().ClaimedDays == 3,
                "Missed days must preserve progress without paying unvisited days.");
            Require(config.CoinsForDay(8) == 450, "Rewards must keep increasing beyond day seven.");
            // Simulate termination after writing intent, before crediting the wallet.
            var pending = DailyRewardSave.Read(); pending.PendingId = "daily-reward-test-interrupted";
            pending.PendingAmount = 250; pending.PendingDate = now.AddDays(7).Date.Ticks;
            PlayerPrefs.SetString(DailyRewardSave.StateKey, JsonUtility.ToJson(pending)); PlayerPrefs.Save();
            Require(DailyRewardSave.Recover() == 250 && CosmeticsSave.Coins == 800, "Unpaid intent must recover.");
            Require(DailyRewardSave.Recover() == 0 && CosmeticsSave.Coins == 800, "Recovery must not replay.");
            // Simulate termination after credit, before finishing daily progression.
            pending = DailyRewardSave.Read(); pending.PendingId = "daily-reward-test-paid";
            pending.PendingAmount = 300; pending.PendingDate = now.AddDays(8).Date.Ticks;
            PlayerPrefs.SetString(DailyRewardSave.StateKey, JsonUtility.ToJson(pending)); PlayerPrefs.Save();
            CosmeticsSave.GrantCoinRewardOnce(pending.PendingId, 300);
            Require(DailyRewardSave.Recover() == 300 && CosmeticsSave.Coins == 1100 && DailyRewardSave.Read().ClaimedDays == 5,
                "Paid intent must advance progression without crediting twice.");
            Require(!CosmeticsSave.GrantCoinRewardOnce(pending.PendingId, 300), "A daily receipt must reject replay.");
            Debug.Log("DailyRewardChecks PASS: increasing rewards, same-day lock, local midnight, skipped days, clock rollback and interrupted payout recovery.");
        }
        finally { saved.Restore(); UnityEngine.Object.DestroyImmediate(config); }
    }
    [MenuItem("Tools/Glow Rings/Validate Daily Reward UI (Play Mode)")]
    public static void RunUI()
    {
        var menu = UnityEngine.Object.FindObjectOfType<MainMenuUI>();
        if (!EditorApplication.isPlaying || menu == null) throw new InvalidOperationException("Run in MainMenu in Play Mode.");
        menu.StartCoroutine(CheckUI(menu));
    }
    private static IEnumerator CheckUI(MainMenuUI menu)
    {
        var saved = new SavedPreferences(); DailyRewardUI ui = null;
        var config = Resources.Load<DailyRewardConfig>("DailyRewardConfig");
        try
        {
            saved.Clear(); menu.SendMessage("RefreshWallet", SendMessageOptions.DontRequireReceiver); menu.ShowPage(MenuPage.DailyReward);
            ui = UnityEngine.Object.FindObjectOfType<DailyRewardUI>();
            Require(ui != null && ui.ClaimAvailable && ui.DisplayedReward == config.FirstDayCoins, "Menu must open a claimable first-day reward.");
            Require(!ui.GetComponentsInChildren<TMPro.TMP_Text>().Any(x => x.text.Contains("COMING SOON")), "Daily Reward must be active.");
            yield return null;
            System.IO.Directory.CreateDirectory("Temp/CosmeticsValidation");
            ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Temp/CosmeticsValidation/Daily-Reward-Ready.png"));
            yield return null;
            ui.Claim(); int paid = CosmeticsSave.Coins;
            Require(paid == 100 + config.FirstDayCoins && !ui.ClaimAvailable && DailyRewardSave.Read().ClaimedDays == 1,
                "Claim must pay once and disable the button.");
            ui.Claim(); Require(CosmeticsSave.Coins == paid, "Double click must not duplicate coins.");
            var effect = ui.GetComponentInChildren<DailySpinRewardVFX>(true);
            Require(effect.IsPlaying && effect.ActiveCoinCount == 16, "Claim must play the reward celebration.");
            yield return new WaitForSecondsRealtime(0.9f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Temp/CosmeticsValidation/Daily-Reward-Collected.png"));
            yield return new WaitForSecondsRealtime(2.7f);
            Require(!effect.IsPlaying && effect.ActiveCoinCount == 0 && CosmeticsSave.Coins == paid, "Effect must clean up without replaying payout.");
            ui.Close(); menu.ShowPage(MenuPage.DailyReward);
            Require(!ui.ClaimAvailable && CosmeticsSave.Coins == paid, "Reopening must preserve the daily lock.");
            var state = DailyRewardSave.Read(); state.LastClaimDate = DateTime.Today.AddDays(-1).Ticks;
            PlayerPrefs.SetString(DailyRewardSave.StateKey, JsonUtility.ToJson(state)); PlayerPrefs.Save();
            ui.Reopen(); Require(ui.ClaimAvailable && ui.DisplayedReward == config.CoinsForDay(2), "Next day must show the higher reward.");
            ui.Claim(); Require(CosmeticsSave.Coins == paid + config.CoinsForDay(2), "Next day must credit the displayed reward.");
            ui.Close(); Require(!effect.IsPlaying && effect.ActiveCoinCount == 0, "Closing during celebration must clean effects.");
            state = DailyRewardSave.Read(); state.ClaimedDays = 7; state.LastClaimDate = DateTime.Today.AddDays(-1).Ticks;
            PlayerPrefs.SetString(DailyRewardSave.StateKey, JsonUtility.ToJson(state)); PlayerPrefs.Save();
            ui.Reopen();
            Require(ui.ClaimAvailable && ui.DisplayedReward == config.CoinsForDay(8) &&
                ui.GetComponentsInChildren<TMPro.TMP_Text>().Any(x => x.text == "14. GÜN"),
                "The next week must show days 8-14 with the increased day-eight reward.");
            Debug.Log("DailyRewardUIChecks PASS: menu, first claim, double-click guard, celebration, cleanup, reopening and higher next-day payout.");
        }
        finally
        {
            if (ui != null) ui.gameObject.SetActive(false);
            saved.Restore(); menu.SendMessage("RefreshWallet", SendMessageOptions.DontRequireReceiver);
            menu.SendMessage("RefreshDailyReward", SendMessageOptions.DontRequireReceiver);
        }
    }
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException("DailyRewardChecks: " + message); }
}
