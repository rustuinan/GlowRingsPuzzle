using UnityEngine;

[CreateAssetMenu(menuName = "Glow Rings/Daily Reward Config")]
public sealed class DailyRewardConfig : ScriptableObject
{
    [Min(1)] public int FirstDayCoins = 100;
    [Min(1)] public int ExtraCoinsPerDay = 50;
    public Sprite CoinIcon;
    public Sprite Ribbon;
    public Sprite GiftIcon;
    public Sprite ClaimedIcon;
    public Sprite LockedIcon;
    public TMPro.TMP_FontAsset UIFont;
    public AudioClip RewardSound;
    public bool IsValid => FirstDayCoins > 0 && ExtraCoinsPerDay > 0;
    public int CoinsForDay(int day) => (int)System.Math.Min(int.MaxValue,
        FirstDayCoins + (long)System.Math.Max(0, day - 1) * ExtraCoinsPerDay);
}
