/// <summary>A match chain allows one preparation move, without a real-time deadline.</summary>
public sealed class ComboChain
{
    public int Multiplier { get; private set; }
    public int PreparationMovesRemaining { get; private set; }

    public void RegisterMove(bool matched)
    {
        if (matched)
        {
            Multiplier = System.Math.Min(Multiplier + 1, 5);
            PreparationMovesRemaining = 1;
        }
        else if (Multiplier > 0 && PreparationMovesRemaining > 0)
        {
            PreparationMovesRemaining--;
        }
        else
        {
            Reset();
        }
    }

    public void Reset()
    {
        Multiplier = 0;
        PreparationMovesRemaining = 0;
    }
}
