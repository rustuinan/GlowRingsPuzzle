using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DailySpinChecks
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
            strings = new[] { DailySpinSave.StateKey, CosmeticsSave.RewardJournalKey }
                .ToDictionary(x => x, x => (PlayerPrefs.HasKey(x), PlayerPrefs.GetString(x)));
        }
        public void Clear(int coins = 100)
        { foreach (var key in strings.Keys) PlayerPrefs.DeleteKey(key); PlayerPrefs.SetInt(balanceKey, coins); PlayerPrefs.Save(); }
        public void Restore()
        {
            foreach (var pair in strings)
            { if (pair.Value.exists) PlayerPrefs.SetString(pair.Key, pair.Value.value); else PlayerPrefs.DeleteKey(pair.Key); }
            if (hadBalance) PlayerPrefs.SetInt(balanceKey, balance); else PlayerPrefs.DeleteKey(balanceKey);
            PlayerPrefs.Save();
        }
    }
    [MenuItem("Tools/Glow Rings/Validate Daily Spin (Edit Mode)")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run outside Play Mode.");
        var saved = new SavedPreferences();
        var config = ScriptableObject.CreateInstance<DailySpinConfig>();
        var random = UnityEngine.Random.state;
        try
        {
            saved.Clear();
            var now = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
            Require(DailySpinSave.CanSpin(now, config), "First spin must be free and ready.");
            float step = 360f / config.Rewards.Length;
            for (int index = 0; index < config.Rewards.Length; index++)
                foreach (float fraction in new[] { -0.2f, 0f, 0.2f })
                    foreach (int turns in new[] { -12, 0, 12 })
                    {
                        float angle = index * step + fraction * step + turns * 360;
                        Require(DailySpinSave.SegmentAtPointer(angle, config.Rewards.Length) == index, "Pointer-to-prize mapping failed.");
                        Require(Mathf.Abs(Mathf.DeltaAngle(90, 90 - index * step + angle)) < step * 0.5f,
                            "Printed wheel label does not land under the fixed pointer.");
                    }
            Require(DailySpinSave.TryBegin(config, now, out var pending), "Ready spin must start.");
            string id = pending.Id;
            Require(!DailySpinSave.TryBegin(config, now, out _), "Double-click must not consume another spin.");
            int landed = DailySpinSave.SegmentAtPointer(pending.StopAngle, pending.Rewards.Length);
            int prize = pending.Rewards[landed];
            config.Rewards[landed] += 123;
            Require(DailySpinSave.Read().Rewards[landed] == prize, "Pending spin must preserve its displayed prize snapshot.");
            Require(!DailySpinSave.CanSpin(now.AddDays(2), config), "Interrupted pending spin must finish before a new spin.");
            Require(DailySpinSave.Complete() == prize && CosmeticsSave.Coins == 100 + prize, "Landing must pay the exact shown amount.");
            Require(DailySpinSave.Complete() == 0 && CosmeticsSave.Coins == 100 + prize, "Reward must not be paid twice.");
            Require(!DailySpinSave.CanSpin(now.AddHours(24).AddTicks(-1), config) && DailySpinSave.CanSpin(now.AddHours(24), config), "Daily cooldown boundary is incorrect.");
            Require(!DailySpinSave.CanSpin(now.AddDays(-1), config), "Clock rollback must not reset the cooldown.");
            config.UnlimitedTestSpins = true;
            Require(DailySpinSave.TryBegin(config, now, out var repeat), "Test mode must bypass an existing cooldown.");
            int repeatPrize = repeat.Rewards[DailySpinSave.SegmentAtPointer(repeat.StopAngle, repeat.Rewards.Length)];
            Require(DailySpinSave.Complete() == repeatPrize && CosmeticsSave.Coins == 100 + prize + repeatPrize, "Repeat test spin must award its own prize.");
            Require(DailySpinSave.CanSpin(now, config), "Test spins must remain available after payout.");
            config.UnlimitedTestSpins = false;
            Require(!CosmeticsSave.GrantCoinRewardOnce(id, prize), "Already settled receipt must reject replay.");
            Require(CosmeticsSave.GrantCoinRewardOnce("later-reward", 10), "A later independent reward must be grantable.");
            int paidBalance = CosmeticsSave.Coins;
            Require(!CosmeticsSave.GrantCoinRewardOnce(id, prize) && CosmeticsSave.Coins == paidBalance, "Historical receipt must reject replay after newer rewards.");
            saved.Clear();
            PlayerPrefs.SetString(CosmeticsSave.RewardJournalKey, "{\"Id\":\"interrupted\",\"BalanceAfter\":375,\"Settled\":false}");
            PlayerPrefs.Save();
            Require(CosmeticsSave.Coins == 375 && CosmeticsSave.Coins == 375, "Prepared reward journal must recover once.");
            Require(CosmeticsSave.HasCoinReward("interrupted"), "Recovered reward must be marked settled.");
            PlayerPrefs.SetInt(CosmeticsSave.Prefix + "DemoCoins", 350); PlayerPrefs.Save();
            Require(CosmeticsSave.Coins == 350, "Settled journal must not undo later spending.");
            saved.Clear(); config.Rewards = new[] { 50, 100, 150, 250, 400, 600, 1000, 1500 };
            Require(DailySpinSave.TryBegin(config, now, out pending), "Recovery spin must start.");
            prize = pending.Rewards[DailySpinSave.SegmentAtPointer(pending.StopAngle, pending.Rewards.Length)];
            CosmeticsSave.GrantCoinRewardOnce(pending.Id, prize);
            // Simulate exit after wallet credit but before clearing the pending spin.
            Require(DailySpinSave.Read().Pending && DailySpinSave.Complete() == prize && CosmeticsSave.Coins == 100 + prize,
                "Recovering a credited pending spin must not add the reward again.");
            Require(!CosmeticsSave.GrantCoinRewardOnce("invalid", 0), "Non-positive rewards must be rejected.");
            Debug.Log("DailySpinChecks PASS: all pointer alignments, exact payouts, duplicate guards, cooldown boundary, pending snapshot, rollback, journal recovery and paid-pending recovery.");
        }
        finally { saved.Restore(); UnityEngine.Random.state = random; UnityEngine.Object.DestroyImmediate(config); }
    }
    [MenuItem("Tools/Glow Rings/Validate Daily Spin UI (Play Mode)")]
    public static void RunUI()
    {
        var menu = UnityEngine.Object.FindObjectOfType<MainMenuUI>();
        if (!EditorApplication.isPlaying || menu == null) throw new InvalidOperationException("Run in the MainMenu scene in Play Mode.");
        menu.StartCoroutine(CheckUI(menu));
    }
    private static IEnumerator CheckUI(MainMenuUI menu)
    {
        var saved = new SavedPreferences(); var config = Resources.Load<DailySpinConfig>("DailySpinConfig");
        float duration = config.SpinDuration; var random = UnityEngine.Random.state;
        DailySpinUI ui = null;
        try
        {
            saved.Clear(); config.SpinDuration = 0.25f;
            menu.ShowPage(MenuPage.DailySpin); ui = UnityEngine.Object.FindObjectOfType<DailySpinUI>();
            Require(ui != null, "Menu must open the active wheel popup.");
            var prizeWheel = ui.GetComponentsInChildren<RectTransform>().Single(x => x.name == "Prize Wheel");
            Require(!prizeWheel.GetComponentsInChildren<TMPro.TMP_Text>().Any(x => x.text.Contains("COINS")), "Prize sectors must only show amounts.");
            ui.Spin(); string pendingId = DailySpinSave.Read().Id;
            Require(ui.IsSpinning && CosmeticsSave.Coins == 100, "Reward must wait until the wheel stops.");
            ui.Spin(); Require(DailySpinSave.Read().Id == pendingId, "Double click started a second spin.");
            ui.Close(); Require(ui.gameObject.activeSelf, "Close must be blocked during the spin.");
            yield return new WaitForSecondsRealtime(0.6f);
            var state = DailySpinSave.Read();
            var rotor = ui.GetComponentsInChildren<RectTransform>().Single(x => x.name == "Prize Wheel");
            int shown = state.Rewards[DailySpinSave.SegmentAtPointer(rotor.localEulerAngles.z, state.Rewards.Length)];
            Require(!ui.IsSpinning && ui.DisplayedReward == shown && CosmeticsSave.Coins == 100 + shown, "Stopped pointer, result and balance must agree.");
            var vfx = ui.GetComponentInChildren<DailySpinRewardVFX>(true);
            Require(vfx != null && vfx.IsPlaying && vfx.ActiveCoinCount == 16, "A won spin must start the coin-flight celebration.");
            Require(ui.GetComponentsInChildren<TMPro.TMP_Text>().Any(x => x.text == "YOU WON!"), "Winning must have an explicit celebration heading.");
            Require(ui.GetComponentsInChildren<Button>().Single(x => x.name == "SPIN Button").interactable == config.UnlimitedTestSpins, "Availability must respect the test-mode setting after payout.");
            yield return new WaitForSecondsRealtime(0.55f);
            System.IO.Directory.CreateDirectory("Temp/CosmeticsValidation");
            ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Temp/CosmeticsValidation/Daily-Spin-Celebration.png"));
            yield return new WaitForSecondsRealtime(2.5f);
            Require(!vfx.IsPlaying && vfx.ActiveCoinCount == 0 && CosmeticsSave.Coins == 100 + shown, "VFX must clean up without altering the payout.");
            var wallet = ui.GetComponentsInChildren<RectTransform>().Single(x => x.name == "Coin Balance").GetComponentInChildren<TMPro.TMP_Text>();
            Require(wallet.text == CosmeticsSave.Coins.ToString("N0") + " COINS", "Wallet count-up must finish at the real balance.");
            ui.Close(); ui.Reopen(); Require(CosmeticsSave.Coins == 100 + shown, "Reopening paid spin duplicated the reward.");
            Require(!vfx.IsPlaying && vfx.ActiveCoinCount == 0, "Opening a paid spin must not replay the win effects.");
            if (config.UnlimitedTestSpins)
            {
                int previousBalance = CosmeticsSave.Coins;
                ui.Spin(); Require(ui.IsSpinning, "Test mode must allow an immediate second spin.");
                yield return new WaitForSecondsRealtime(0.6f);
                Require(CosmeticsSave.Coins == previousBalance + ui.DisplayedReward && DailySpinSave.CanSpin(DateTime.UtcNow), "Second spin must pay once and remain available.");
                ui.Spin(); Require(ui.IsSpinning && !vfx.IsPlaying, "Starting again during the win effect must clear it.");
                yield return new WaitForSecondsRealtime(0.6f);
                Debug.Log("DailySpin unlimited test mode PASS: consecutive rewards, no cooldown and restarting during celebration.");
            }
            saved.Clear(); ui.Reopen(); ui.Spin();
            state = DailySpinSave.Read(); shown = state.Rewards[DailySpinSave.SegmentAtPointer(state.StopAngle, state.Rewards.Length)];
            ui.gameObject.SetActive(false); Require(DailySpinSave.Read().Pending, "Interrupted spin was not saved.");
            ui.Reopen(); yield return new WaitForSecondsRealtime(0.6f);
            Require(!ui.IsSpinning && ui.DisplayedReward == shown && CosmeticsSave.Coins == 100 + shown, "Resumed animation must reveal and credit the same prize once.");
            Debug.Log("DailySpinUIChecks PASS: amount-only sectors, delayed payout, pointer/result/balance match, win heading, confetti/coin-flight cleanup, wallet count-up, no celebration replay, daily lock and interrupted animation recovery.");
        }
        finally
        {
            if (ui != null) ui.gameObject.SetActive(false);
            config.SpinDuration = duration; saved.Restore(); UnityEngine.Random.state = random;
            menu.SendMessage("RefreshWallet", SendMessageOptions.DontRequireReceiver);
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException("DailySpinChecks: " + message); }
}
