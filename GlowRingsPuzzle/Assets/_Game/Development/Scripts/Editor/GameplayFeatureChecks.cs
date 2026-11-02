using System;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Play Mode integration checks against the actual Game scene. No PlayerPrefs are changed.</summary>
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

    private static IEnumerator Check(GameManager game)
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
        Button bombButton = GameObject.Find("Canvas/GamePanel/PlanningHUD/CellBombButton").GetComponent<Button>();
        game.StartGame();
        yield return null;
        Require(spawner.CurrentPiece != null && spawner.NextPieceData != null, "Current and preview pieces must exist.");
        Require(game.CurrentBombCount == 2, "New game must start with two bombs.");
        Require(!game.CanUseBomb, "An empty board must not offer a bomb.");
        Require(bombButton.onClick.GetPersistentEventCount() == 0, "Bomb must not inherit the discard handler.");

        RingPieceData expectedPiece = spawner.NextPieceData;
        RingColorType expectedColor = spawner.NextPieceColor;
        int discardCount = game.CurrentTrashCount;
        game.TrashCurrentPiece();
        yield return new WaitForSeconds(0.6f);
        Require(!game.IsResolving && game.CurrentTrashCount == discardCount - 1, "Discard must consume exactly one charge.");
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
        int bombs = game.CurrentBombCount;
        yield return null;
        bombButton.onClick.Invoke();
        Require(game.IsBombTargeting, "Bomb UI must enter target selection.");
        Require(!game.TryBombCell(board.Cells[8]) && game.CurrentBombCount == bombs,
            "An empty target must not consume a charge.");
        bombButton.onClick.Invoke();
        Require(!game.IsBombTargeting && game.CurrentBombCount == bombs, "Cancel must be free.");
        bombButton.onClick.Invoke();
        Require(game.TryBombCell(target), "Occupied target must accept the bomb.");
        Require(!game.TryBombCell(neighbor), "Repeated input must not spend a second charge.");
        yield return new WaitForSeconds(0.4f);
        Require(target.GetAllRings().Length == 0 && neighbor.GetAllRings().Length == 1,
            "Bomb must clear every layer of exactly one cell.");
        Require(game.CurrentBombCount == bombs - 1 && ScoreManager.Instance.Score == score && game.ComboCount == 0,
            "Bomb must cost one charge and award neither score nor combo.");
        Require(spawner.CurrentPiece == current && spawner.NextPieceData == expectedPiece && spawner.NextPieceColor == expectedColor,
            "Bomb must preserve both current and queued pieces.");

        // A blocked board must ask before using inventory; cancellation returns to the choice.
        foreach (Cell cell in board.Cells)
        for (int layer = 0; layer < 3; layer++) cell.ForcePlaceRing(ringPrefab, (RingLayer)layer, RingColorType.Blue);
        Set(game, "currentTrashCount", 0);
        Invoke(game, "CheckGameOver");
        Require(game.HasBlockedPiece && !game.IsGameOver && game.IsBoosterOfferOpen && !game.CanUseBomb,
            "A blocked board must offer an explicit, modal rescue choice.");
        Require(game.CurrentBombCount == 1, "Offering a booster must not spend it.");
        game.AcceptBombRescue();
        Require(game.IsBombTargeting && !game.IsBoosterOfferOpen, "Accepting a bomb must enter target selection.");
        game.CancelBombTargeting();
        Require(game.IsBoosterOfferOpen && game.CurrentBombCount == 1,
            "Cancelling a rescue target must reopen the offer without spending a charge.");
        game.AcceptBombRescue();
        Require(game.TryBombCell(target), "Bomb must rescue a blocked board.");
        yield return new WaitForSeconds(0.4f);
        Require(!game.IsGameOver && board.HasMove(current) && game.CurrentBombCount == 0,
            "Clearing a cell must restore a legal move.");
        game.ToggleBombTargeting();
        Require(!game.IsBombTargeting, "Exhausted inventory must block targeting.");

        for (int layer = 0; layer < 3; layer++) target.ForcePlaceRing(ringPrefab, (RingLayer)layer, RingColorType.Blue);
        Invoke(game, "CheckGameOver");
        Require(game.IsGameOver && !game.IsBoosterOfferOpen,
            "A blocked board without boosters must end immediately.");

        game.StartGame();
        yield return null;
        foreach (Cell cell in board.Cells)
        for (int layer = 0; layer < 3; layer++) cell.ForcePlaceRing(ringPrefab, (RingLayer)layer, RingColorType.Blue);
        Invoke(game, "CheckGameOver");
        int retainedBombs = game.CurrentBombCount;
        int retainedDiscards = game.CurrentTrashCount;
        BoosterOfferPanelUI offer = UnityEngine.Object.FindObjectOfType<BoosterOfferPanelUI>(true);
        yield return null;
        Require(offer.gameObject.activeInHierarchy, "The booster offer must be visible.");
        // Exercise the actual modal button callbacks, rather than spending through the old toolbar.
        offer.transform.Find("Card/EndRun").GetComponent<Button>().onClick.Invoke();
        Require(game.IsGameOver && !game.IsBoosterOfferOpen && game.CurrentBombCount == retainedBombs &&
            game.CurrentTrashCount == retainedDiscards, "Declining must end the run and preserve all inventory.");

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
        Require(game.CurrentTrashCount == retainedDiscards - 1 && spawner.LastSpawnData == expectedPiece &&
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
        Require(preview != null && preview.PreviewPiece != null &&
            preview.PreviewPiece.CurrentPieceData == spawner.NextPieceData,
            "Preview must instantiate the queued gameplay piece.");
        foreach (Collider collider in preview.PreviewPiece.GetComponentsInChildren<Collider>(true))
            Require(!collider.enabled, "Preview colliders must not intercept gameplay input.");
        foreach (Ring ring in preview.PreviewPiece.Rings)
        {
            Require(ring.ColorType == spawner.NextPieceColor, "Preview must use the queued color.");
            foreach (Renderer renderer in ring.GetComponentsInChildren<Renderer>())
                Require(renderer.sharedMaterial == ThemeManager.Instance.GetColorData(spawner.NextPieceColor).Material,
                    "Preview must use gameplay materials.");
        }
        Require(GameObject.Find("Canvas/GamePanel/PlanningHUD/ComboStatus") == null,
            "Persistent combo text must be removed.");
        Require(game.CurrentBombCount == 2 && !game.IsBombTargeting && game.ComboCount == 0,
            "Restart must reset inventory, targeting and combo.");
        Require(spawner.NextPieceData != null, "Restart must refill the preview queue.");
        Debug.Log("GameplayFeatureChecks PASS: bomb UI/cancel/empty target/three-layer clear/single charge/no score/no reroll; " +
            "discard preview promotion; explicit rescue accept/cancel/decline/discard and empty inventory; " +
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
