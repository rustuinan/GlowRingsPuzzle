using UnityEngine;

internal sealed class MovePuzzle
{
    public const int Count = 3;
    private const string CompletionKey = "GlowRings.MovePuzzles.Completed";
    private static int pendingLevel = -1;

    public int Level { get; }
    public int Moves { get; private set; }
    public int Cleared { get; private set; }
    public int MoveLimit => Level == 2 ? 2 : 1;
    public int Target => Level == 2 ? 7 : 3;

    public MovePuzzle(int level) { Level = level; }

    public static void Queue(int level) { pendingLevel = level; }

    public static MovePuzzle Take()
    {
        int level = pendingLevel;
        pendingLevel = -1;
        return level >= 0 && level < Count ? new MovePuzzle(level) : null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSelection() { pendingLevel = -1; }

    public static bool IsCompleted(int level) => (PlayerPrefs.GetInt(CompletionKey) & (1 << level)) != 0;

    public void Reset(BoardManager board, PieceSpawner spawner)
    {
        Moves = Cleared = 0;
        if (Level == 0)
        {
            Seed(4, RingLayer.Outer, RingColorType.Purple);
            Seed(4, RingLayer.Middle, RingColorType.Purple);
        }
        else
        {
            Seed(0, RingLayer.Outer, RingColorType.Blue);
            Seed(4, RingLayer.Middle, RingColorType.Blue);
            if (Level == 2)
            {
                Seed(1, RingLayer.Outer, RingColorType.Red);
                Seed(1, RingLayer.Middle, RingColorType.Red);
                Seed(8, RingLayer.Outer, RingColorType.Red);
                Seed(8, RingLayer.Middle, RingColorType.Red);
            }
        }

        void Seed(int cell, RingLayer layer, RingColorType color) =>
            board.GetCell(cell).ForcePlaceRing(spawner.PiecePrefab.GetRingTemplate(layer), layer, color);
    }

    public bool Spawn(PieceSpawner spawner)
    {
        if (Moves >= MoveLimit) return false;
        if (Level == 0) return spawner.SpawnPuzzlePiece(4, RingColorType.Purple);
        if (Level == 1) return spawner.SpawnPuzzlePiece(1, RingColorType.Blue);
        return Moves == 0
            ? spawner.SpawnPuzzlePiece(4, RingColorType.Red, 5, RingColorType.Blue)
            : spawner.SpawnPuzzlePiece(5, RingColorType.Blue);
    }

    public void Record(int cleared)
    {
        Moves++;
        Cleared += cleared;
        if (Cleared < Target || IsCompleted(Level)) return;
        PlayerPrefs.SetInt(CompletionKey, PlayerPrefs.GetInt(CompletionKey) | (1 << Level));
        PlayerPrefs.Save();
    }
}
