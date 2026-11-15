using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Play Mode integration checks against the actual Game scene. Restores the wallet and statistics.</summary>
public static class GameplayFeatureChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Glow Rings/Validate Gameplay Features (Play Mode)")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying || GameManager.Instance == null)
            throw new InvalidOperationException("Enter Play Mode in the Game scene first.");
        GameManager.Instance.StartCoroutine(Check(GameManager.Instance));
    }

    private const string WalletKey = CosmeticsSave.Prefix + "DemoCoins";

    private static IEnumerator Check(GameManager game)
    {
        int savedBalance = CosmeticsSave.Coins;
        bool hadBalance = PlayerPrefs.HasKey(WalletKey);
        bool hadProgress = PlayerPrefs.HasKey(PlayerProgressSave.StateKey);
        string savedProgress = PlayerPrefs.GetString(PlayerProgressSave.StateKey);
        try
        {
            PlayerPrefs.SetInt(WalletKey, 1000);
            yield return CheckWithTestWallet(game);
        }
        finally
        {
            if (hadBalance) PlayerPrefs.SetInt(WalletKey, savedBalance);
            else PlayerPrefs.DeleteKey(WalletKey);
            if (hadProgress) PlayerPrefs.SetString(PlayerProgressSave.StateKey, savedProgress);
            else PlayerPrefs.DeleteKey(PlayerProgressSave.StateKey);
            PlayerPrefs.Save();
        }
    }

    private static IEnumerator CheckWithTestWallet(GameManager game)
    {
        // Isolate gameplay checks from the SDK's fullscreen editor test ad.
        if (AdMobManager.Instance != null) UnityEngine.Object.DestroyImmediate(AdMobManager.Instance.gameObject);
        bool previousBackgroundMode = Application.runInBackground;
        Application.runInBackground = true;
        Time.timeScale = 1f;
        FirstMoveTutorialManager tutorial = UnityEngine.Object.FindObjectOfType<FirstMoveTutorialManager>();
        if (tutorial != null)
        {
            tutorial.StopAllCoroutines();
            Set(tutorial, "isTutorialActive", false);
            tutorial.enabled = false;
            UnityEngine.Object.FindObjectOfType<TutorialHandHintUI>(true)?.HideInstant();
        }
        PieceSpawner spawner = UnityEngine.Object.FindObjectOfType<PieceSpawner>();
        BoardManager board = game.Board;
        Ring ringPrefab = AssetDatabase.LoadAssetAtPath<Ring>("Assets/_Game/Development/Prefab/TutorialRing.prefab");
        Button bombButton = GameObject.Find("PlanningHUD/CellBombButton").GetComponent<Button>();
        game.StartGame();
        yield return null;
        Require(spawner.CurrentPiece != null && spawner.NextPieceData != null, "Current and preview pieces must exist.");
        Require(CosmeticsSave.Coins == 1000, "Starting a run must preserve the wallet.");
        Require(!game.CanUseBomb, "An empty board must not offer a bomb.");
        Require(bombButton.onClick.GetPersistentEventCount() == 0, "Bomb must not inherit the discard handler.");

        RingPieceData expectedPiece = spawner.NextPieceData;
        RingColorType expectedColor = spawner.NextPieceColor;
        int beforeDiscard = CosmeticsSave.Coins;
        game.TrashCurrentPiece();
        yield return new WaitForSeconds(0.6f);
        Require(!game.IsResolving && CosmeticsSave.Coins == beforeDiscard - GameManager.ChangePiecePrice, "Discard must debit its coin price once.");
        Require(spawner.CurrentPiece.CurrentPieceData == expectedPiece && spawner.LastSpawnColor == expectedColor,
            "Discard must promote the exact previewed piece.");

        Cell target = board.Cells[0];
        Cell neighbor = board.Cells[1];
        target.ForcePlaceRing(ringPrefab, RingLayer.Outer, RingColorType.Blue);
        target.ForcePlaceRing(ringPrefab, RingLayer.Middle, RingColorType.Red);
        target.ForcePlaceRing(ringPrefab, RingLayer.Inner, RingColorType.Green);
        neighbor.ForcePlaceRing(ringPrefab, RingLayer.Outer, RingColorType.Pink);
        RingPiece current = spawner.CurrentPiece;
        expectedPiece = spawner.NextPieceData;
        expectedColor = spawner.NextPieceColor;
        int score = ScoreManager.Instance.Score;
        int beforeBomb = CosmeticsSave.Coins;
        yield return null;
        bombButton.onClick.Invoke();
        Require(game.IsBombTargeting, "Bomb UI must enter target selection.");
        Require(!game.TryBombCell(board.Cells[8]) && CosmeticsSave.Coins == beforeBomb,
            "An empty target must not consume coins.");
        bombButton.onClick.Invoke();
        Require(!game.IsBombTargeting && CosmeticsSave.Coins == beforeBomb, "Cancel must be free.");
        bombButton.onClick.Invoke();
        Require(game.TryBombCell(target), "Occupied target must accept the bomb.");
        Require(!game.TryBombCell(neighbor), "Repeated input must not spend a second charge.");
        yield return new WaitForSeconds(0.4f);
        Require(target.GetAllRings().Length == 0 && neighbor.GetAllRings().Length == 1,
            "Bomb must clear every layer of exactly one cell.");
        Require(CosmeticsSave.Coins == beforeBomb - GameManager.BombPrice && ScoreManager.Instance.Score == score && game.ComboCount == 0,
            "Bomb must cost its coin price and award neither score nor combo.");
        Require(spawner.CurrentPiece == current && spawner.NextPieceData == expectedPiece && spawner.NextPieceColor == expectedColor,
            "Bomb must preserve both current and queued pieces.");

        // A blocked board must ask before using balance; cancellation returns to the choice.
        foreach (Cell cell in board.Cells)
        for (int layer = 0; layer < 3; layer++) cell.ForcePlaceRing(ringPrefab, (RingLayer)layer, RingColorType.Blue);
        PlayerPrefs.SetInt(WalletKey, GameManager.BombPrice);
        Invoke(game, "CheckGameOver");
        Require(game.HasBlockedPiece && !game.IsGameOver && game.IsBoosterOfferOpen && !game.CanUseBomb,
            "A blocked board must offer an explicit, modal rescue choice.");
        Require(CosmeticsSave.Coins == GameManager.BombPrice, "Offering a booster must not spend it.");
        game.AcceptBombRescue();
        Require(game.IsBombTargeting && !game.IsBoosterOfferOpen, "Accepting a bomb must enter target selection.");
        game.CancelBombTargeting();
        Require(game.IsBoosterOfferOpen && CosmeticsSave.Coins == GameManager.BombPrice,
            "Cancelling a rescue target must reopen the offer without spending coins.");
        game.AcceptBombRescue();
        Require(game.TryBombCell(target), "Bomb must rescue a blocked board.");
        yield return new WaitForSeconds(0.4f);
        Require(!game.IsGameOver && board.HasMove(current) && CosmeticsSave.Coins == 0,
            "Clearing a cell must restore a legal move.");
        game.ToggleBombTargeting();
        Require(!game.IsBombTargeting, "Exhausted balance must block targeting.");
        game.TrashCurrentPiece();
        Require(!game.IsResolving && spawner.CurrentPiece == current && CosmeticsSave.Coins == 0,
            "Insufficient coins must leave the current piece and wallet intact.");

        for (int layer = 0; layer < 3; layer++) target.ForcePlaceRing(ringPrefab, (RingLayer)layer, RingColorType.Blue);
        Invoke(game, "CheckGameOver");
        Require(game.IsGameOver && !game.IsBoosterOfferOpen,
            "A blocked board without boosters must end immediately.");

        PlayerPrefs.SetInt(WalletKey, 1000);
        game.StartGame();
        yield return null;
        foreach (Cell cell in board.Cells)
        for (int layer = 0; layer < 3; layer++) cell.ForcePlaceRing(ringPrefab, (RingLayer)layer, RingColorType.Blue);
        Invoke(game, "CheckGameOver");
        int retainedBalance = CosmeticsSave.Coins;
        BoosterOfferPanelUI offer = UnityEngine.Object.FindObjectOfType<BoosterOfferPanelUI>(true);
        yield return null;
        Require(offer.gameObject.activeInHierarchy, "The booster offer must be visible.");
        // Exercise the actual modal button callbacks, rather than spending through the old toolbar.
        offer.transform.Find("Card/EndRun").GetComponent<Button>().onClick.Invoke();
        Require(game.IsGameOver && !game.IsBoosterOfferOpen && CosmeticsSave.Coins == retainedBalance, "Declining must end the run and preserve all balance.");

        game.StartGame();
        yield return null;
        foreach (Cell cell in board.Cells)
        for (int layer = 0; layer < 3; layer++) cell.ForcePlaceRing(ringPrefab, (RingLayer)layer, RingColorType.Blue);
        expectedPiece = spawner.NextPieceData;
        expectedColor = spawner.NextPieceColor;
        Invoke(game, "CheckGameOver");
        yield return null;
        offer.transform.Find("Card/UseDiscard").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSeconds(0.6f);
        Require(CosmeticsSave.Coins == retainedBalance - GameManager.ChangePiecePrice && spawner.LastSpawnData == expectedPiece &&
            spawner.LastSpawnColor == expectedColor && game.IsBoosterOfferOpen,
            "Choosing discard must spend once, promote the preview, and ask again if the new piece is blocked.");
        game.DeclineBoosterRescue();

        ComboChain chain = new ComboChain();
        chain.RegisterMove(true); chain.RegisterMove(false); chain.RegisterMove(true);
        Require(chain.Multiplier == 2 && chain.PreparationMovesRemaining == 1,
            "One preparation move must preserve the chain.");
        chain.RegisterMove(false); chain.RegisterMove(false);
        Require(chain.Multiplier == 0, "Two misses must end the chain.");
        for (int i = 0; i < 20; i++) chain.RegisterMove(true);
        Require(chain.Multiplier == 5, "Combo must cap at x5.");
        chain.Reset();
        Require(chain.Multiplier == 0 && chain.PreparationMovesRemaining == 0, "Reset must clear the chain.");

        game.StartGame();
        yield return null;
        yield return null;
        NextPiecePreview preview = UnityEngine.Object.FindObjectOfType<NextPiecePreview>();
        Require(preview != null && preview.PreviewData == spawner.NextPieceData &&
            preview.PreviewColor == spawner.NextPieceColor, "Preview must show the queued shape and color.");
        Require(!preview.GetComponent<CanvasGroup>().blocksRaycasts && preview.GetComponentInChildren<Collider>() == null,
            "Preview must not intercept gameplay input.");
        Require(GameObject.Find("Canvas/GamePanel/PlanningHUD/ComboStatus") == null,
            "Persistent combo text must be removed.");
        Require(CosmeticsSave.Coins == retainedBalance - GameManager.ChangePiecePrice && !game.IsBombTargeting && game.ComboCount == 0,
            "Restart must preserve the balance and reset targeting and combo.");
        Require(spawner.NextPieceData != null, "Restart must refill the preview queue.");
        Debug.Log("GameplayFeatureChecks PASS: bomb UI/cancel/empty target/three-layer clear/single debit/no score/no reroll; " +
            "discard preview promotion; explicit rescue accept/cancel/decline/discard and empty balance; " +
            "combo grace/cap/reset; actual prefab/material preview with disabled colliders; no persistent combo label; restart.");
        Application.runInBackground = previousBackgroundMode;
    }

    [MenuItem("Tools/Glow Rings/Preview Bomb Targets (Play Mode)")]
    public static void PreviewTargets()
    {
        if (!EditorApplication.isPlaying || GameManager.Instance == null) return;
        GameManager game = GameManager.Instance;
        Ring prefab = AssetDatabase.LoadAssetAtPath<Ring>("Assets/_Game/Development/Prefab/TutorialRing.prefab");
        game.Board.Cells[0].ForcePlaceRing(prefab, RingLayer.Outer, RingColorType.Pink);
        game.Board.Cells[0].ForcePlaceRing(prefab, RingLayer.Middle, RingColorType.Green);
        game.Board.Cells[0].ForcePlaceRing(prefab, RingLayer.Inner, RingColorType.Blue);
        game.Board.Cells[4].ForcePlaceRing(prefab, RingLayer.Outer, RingColorType.Blue);
        ComboChain chain = (ComboChain)typeof(GameManager).GetField("comboChain", Private).GetValue(game);
        chain.RegisterMove(true);
        chain.RegisterMove(true);
        game.ToggleBombTargeting();
    }

    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    private static void Invoke(object target, string name) => target.GetType().GetMethod(name, Private).Invoke(target, null);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
