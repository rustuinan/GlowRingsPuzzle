public class MatchData
{
    public Ring RingA { get; private set; }
    public Ring RingB { get; private set; }
    public Ring RingC { get; private set; }

    public RingColorType ColorType { get; private set; }

    public MatchData(Ring ringA, Ring ringB, Ring ringC, RingColorType colorType)
    {
        RingA = ringA;
        RingB = ringB;
        RingC = ringC;
        ColorType = colorType;
    }
}