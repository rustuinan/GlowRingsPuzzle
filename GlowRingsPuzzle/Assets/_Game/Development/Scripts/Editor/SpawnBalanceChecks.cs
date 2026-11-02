using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Deterministic regression checks; uses a temporary preview scene, never the game scene.</summary>
public static class SpawnBalanceChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Glow Rings/Validate Spawn Balance")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run in Edit Mode.");
        Scene preview = EditorSceneManager.NewPreviewScene();
        GameObject root = new GameObject("Spawn balance checks");
        root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root, preview);
        try
        {
            BoardManager manager = root.AddComponent<BoardManager>();
            Cell[] cells = new Cell[9];
            Ring[,] rings = new Ring[9, 3];
            string[] fields = {"outerRing", "middleRing", "innerRing"};
            int[,] board = new int[9, 3];
            for (int c = 0; c < 9; c++)
            {
                GameObject cellObject = new GameObject("Cell " + c);
                cellObject.transform.SetParent(root.transform);
                cells[c] = cellObject.AddComponent<Cell>();
                for (int l = 0; l < 3; l++)
                {
                    GameObject ringObject = new GameObject("Ring " + l);
                    ringObject.transform.SetParent(cellObject.transform);
                    rings[c, l] = ringObject.AddComponent<Ring>();
                    Set(rings[c, l], "layer", (RingLayer)l);
                }
            }
            Set(manager, "cells", cells);
            System.Random random = new System.Random(81273);
            int checkedPlacements = 0;
            for (int sample = 0; sample < 80; sample++)
            {
                for (int c = 0; c < 9; c++)
                    for (int l = 0; l < 3; l++)
                        board[c, l] = random.NextDouble() < 0.45 ? random.Next(6) : -1;
                Sync(board, cells, rings, fields);
                // Normal generation runs only after the preceding matches have resolved.
                if (manager.FindMatches().Count != 0) continue;

                for (int target = 0; target < 9; target++)
                for (int mask = 1; mask <= 7; mask++)
                for (int color = 0; color < 6; color++)
                {
                    if (!SpawnBoardEvaluation.CanPlace(board, target, mask)) continue;
                    SpawnBoardEvaluation.Evaluate(board, target, mask, color,
                        out int predictedMatches, out int predictedCleared, out int setups);
                    int[] original = {board[target, 0], board[target, 1], board[target, 2]};
                    for (int l = 0; l < 3; l++)
                        if ((mask & (1 << l)) != 0) board[target, l] = color;
                    Sync(board, cells, rings, fields);
                    List<MatchData> matches = manager.FindMatches();
                    HashSet<Ring> cleared = new HashSet<Ring>();
                    foreach (MatchData match in matches)
                    {
                        foreach (Ring source in new[] {match.RingA, match.RingB, match.RingC})
                        for (int c = 0; c < 9; c++)
                        {
                            bool owns = false;
                            for (int l = 0; l < 3; l++) owns |= cells[c].GetRing((RingLayer)l) == source;
                            if (!owns) continue;
                            foreach (Ring sibling in cells[c].GetAllRings())
                                if (sibling.ColorType == match.ColorType) cleared.Add(sibling);
                        }
                    }
                    Require(predictedMatches == matches.Count && predictedCleared == cleared.Count,
                        "Preview differs from BoardManager at sample " + sample);
                    for (int l = 0; l < 3; l++) board[target, l] = original[l];
                    checkedPlacements++;
                }
            }
            Require(checkedPlacements > 1000, "Not enough legal placements tested.");

            // Explicit setup and blocked-layer cases.
            for (int c = 0; c < 9; c++) for (int l = 0; l < 3; l++) board[c, l] = -1;
            board[0, 0] = 0;
            SpawnBoardEvaluation.Evaluate(board, 1, 2, 0, out int count, out _, out int setup);
            Require(count == 0 && setup > 0, "A playable two-cell setup was missed.");
            Require(!SpawnBoardEvaluation.CanPlace(board, 0, 1), "Occupied layers must reject a piece.");

            PieceSpawner spawner = root.AddComponent<PieceSpawner>();
            MethodInfo chance = typeof(PieceSpawner).GetMethod("GetDynamicHelpfulChance", Private);
            float previous = 0f;
            for (int i = 0; i <= 100; i++)
            {
                float value = (float)chance.Invoke(spawner, new object[] {i / 100f});
                Require(value >= previous && (i == 0 || value - previous < 0.01f) && value <= 0.5f,
                    "Assistance must change smoothly and remain capped.");
                previous = value;
            }
            float normal = (float)chance.Invoke(spawner, new object[] {0.5f});
            for (int move = 0; move < 8; move++) spawner.ReportMoveResult(false);
            Require((float)chance.Invoke(spawner, new object[] {0.5f}) > normal, "Drought support did not rise.");
            spawner.ReportMoveResult(true);
            Require(Mathf.Approximately((float)chance.Invoke(spawner, new object[] {0.5f}), normal),
                "Match must reset drought support.");
            Set(spawner, "assistCooldown", 2);
            Set(spawner, "allClearAssistCooldown", 10);
            spawner.ResetBalance();
            Require((int)Get(spawner, "assistCooldown") == 0 && (int)Get(spawner, "allClearAssistCooldown") == 0,
                "New-game balance reset failed.");
            CheckGeneration(spawner, manager, board, cells, rings, fields);
            Debug.Log("SpawnBalanceChecks PASS: " + checkedPlacements +
                " placements agree with BoardManager; 1000 spawn cadence/palette checks; setup, legality, smooth assistance, drought and reset checks passed.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static void CheckGeneration(PieceSpawner spawner, BoardManager manager, int[,] board,
        Cell[] cells, Ring[,] rings, string[] fields)
    {
        FieldInfo instance = typeof(ThemeManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        object previousInstance = instance.GetValue(null);
        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        List<UnityEngine.Object> temporary = new List<UnityEngine.Object>();
        try
        {
            ThemeManager themeManager = spawner.gameObject.AddComponent<ThemeManager>();
            ThemeData theme = ScriptableObject.CreateInstance<ThemeData>();
            temporary.Add(theme);
            RingColorData[] palette = new RingColorData[2];
            for (int i = 0; i < palette.Length; i++)
            {
                palette[i] = ScriptableObject.CreateInstance<RingColorData>();
                Set(palette[i], "colorType", i == 0 ? RingColorType.Purple : RingColorType.Blue);
                temporary.Add(palette[i]);
            }
            Set(theme, "ringColors", palette);
            Set(themeManager, "currentTheme", theme);
            instance.SetValue(null, themeManager);
            List<RingPieceData> pieces = new List<RingPieceData>();
            for (int mask = 1; mask < 7; mask++)
            {
                RingPieceData piece = ScriptableObject.CreateInstance<RingPieceData>();
                Set(piece, "hasOuter", (mask & 1) != 0);
                Set(piece, "hasMiddle", (mask & 2) != 0);
                Set(piece, "hasInner", (mask & 4) != 0);
                pieces.Add(piece);
                temporary.Add(piece);
            }
            Set(spawner, "pieceDataList", pieces);
            MethodInfo select = typeof(PieceSpawner).GetMethod("SelectPieceForBoard", Private);
            UnityEngine.Random.InitState(33019);
            int assistCount = 0;
            int setupCount = 0;
            int allClearCount = 0;
            for (int scenario = 0; scenario < 2; scenario++)
            {
                for (int c = 0; c < 9; c++) for (int l = 0; l < 3; l++) board[c, l] = -1;
                board[0, 0] = board[1, 1] = 0;
                if (scenario == 0) board[4, 0] = 2;
                Sync(board, cells, rings, fields);
                Set(spawner, "allClearAssistChance", scenario == 0 ? 0.1f : 1f);
                spawner.ResetBalance();
                int lastAssist = -100;
                int lastAllClear = -100;
                for (int turn = 0; turn < 500; turn++)
                {
                    object[] args = {manager, RingColorType.Purple};
                    RingPieceData piece = (RingPieceData)select.Invoke(spawner, args);
                    RingColorType color = (RingColorType)args[1];
                    Require(piece != null && (color == RingColorType.Purple || color == RingColorType.Blue),
                        "Spawn must use an available shape and active theme color.");
                    SpawnType type = spawner.LastSpawnType;
                    if (type != SpawnType.Random)
                    {
                        Require(turn - lastAssist >= 3, "Assists must have two ordinary spawns between them.");
                        lastAssist = turn;
                        assistCount++;
                    }
                    if (type == SpawnType.SetupCombo) setupCount++;
                    if (type == SpawnType.AllClearAssist)
                    {
                        Require(turn - lastAllClear >= 11, "All-clear cooldown was bypassed.");
                        lastAllClear = turn;
                        allClearCount++;
                    }
                    spawner.ReportMoveResult(turn % 5 == 0);
                }
            }
            Require(assistCount > 20 && setupCount > 0 && allClearCount > 0,
                "Spawn tests must exercise each assistance path.");
        }
        finally
        {
            instance.SetValue(null, previousInstance);
            UnityEngine.Random.state = randomState;
            foreach (UnityEngine.Object asset in temporary) UnityEngine.Object.DestroyImmediate(asset);
        }
    }

    private static void Sync(int[,] board, Cell[] cells, Ring[,] rings, string[] fields)
    {
        for (int c = 0; c < 9; c++)
        for (int l = 0; l < 3; l++)
        {
            Set(rings[c, l], "colorType", (RingColorType)Mathf.Max(0, board[c, l]));
            Set(cells[c], fields[l], board[c, l] < 0 ? null : rings[c, l]);
        }
    }

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, Private).SetValue(target, value);
    private static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
