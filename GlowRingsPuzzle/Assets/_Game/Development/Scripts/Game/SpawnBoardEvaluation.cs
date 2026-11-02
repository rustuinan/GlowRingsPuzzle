/// <summary>Read-only preview of the same cell/line clearing rules used by BoardManager.</summary>
public static class SpawnBoardEvaluation
{
    private static readonly int[,] Lines =
    {
        {0, 1, 2}, {3, 4, 5}, {6, 7, 8}, {0, 3, 6},
        {1, 4, 7}, {2, 5, 8}, {0, 4, 8}, {2, 4, 6}
    };

    // Empty slots are -1. Missing cells have all three slots set to -2.
    public static void Evaluate(int[,] board, int target, int layerMask, int color,
        out int matchCount, out int clearedCount, out int setupCount)
    {
        matchCount = clearedCount = setupCount = 0;
        int matchedCells = 0;
        int beforeTargetCount = CountColor(board, target, color, -1, 0);
        int afterTargetCount = CountColor(board, target, color, target, layerMask);
        if (afterTargetCount == 3)
        {
            matchCount++;
            matchedCells |= 1 << target;
        }
        else if (beforeTargetCount < 2 && afterTargetCount == 2)
        {
            setupCount++;
        }

        for (int line = 0; line < Lines.GetLength(0); line++)
        {
            bool containsTarget = false;
            int before = 0;
            int after = 0;
            int missingCell = -1;
            int lineMask = 0;
            for (int p = 0; p < 3; p++)
            {
                int cell = Lines[line, p];
                containsTarget |= cell == target;
                lineMask |= 1 << cell;
                if (CountColor(board, cell, color, -1, 0) > 0) before++;
                if (CountColor(board, cell, color, target, layerMask) > 0) after++;
                else missingCell = cell;
            }

            if (!containsTarget) continue;
            if (after == 3 && before < 3)
            {
                matchCount++;
                matchedCells |= lineMask;
            }
            else if (after == 2 && before < 2 && HasEmptySlot(board, missingCell))
            {
                setupCount++;
            }
        }

        for (int cell = 0; cell < 9; cell++)
        {
            if ((matchedCells & (1 << cell)) != 0)
                clearedCount += CountColor(board, cell, color, target, layerMask);
        }
    }

    public static bool CanPlace(int[,] board, int target, int layerMask)
    {
        if (layerMask == 0) return false;
        for (int layer = 0; layer < 3; layer++)
            if ((layerMask & (1 << layer)) != 0 && board[target, layer] != -1) return false;
        return true;
    }

    private static bool HasEmptySlot(int[,] board, int cell)
    {
        if (cell < 0) return false;
        for (int layer = 0; layer < 3; layer++)
            if (board[cell, layer] == -1) return true;
        return false;
    }

    private static int CountColor(int[,] board, int cell, int color, int target, int layerMask)
    {
        int count = 0;
        for (int layer = 0; layer < 3; layer++)
            if (board[cell, layer] == color || (cell == target && (layerMask & (1 << layer)) != 0)) count++;
        return count;
    }
}
