using UnityEngine;

[CreateAssetMenu(menuName = "Glow Rings/Daily Spin Config")]
public sealed class DailySpinConfig : ScriptableObject
{
    public int[] Rewards = { 50, 100, 150, 250, 400, 600, 1000, 1500 };
    [Range(1, 168)] public int CooldownHours = 24;
    [Tooltip("Disable the daily cooldown for repeatable test spins.")] public bool UnlimitedTestSpins;
    [Range(2, 10)] public float SpinDuration = 5.5f;
    public Sprite CoinIcon;
    public AudioClip SpinSound;
    public AudioClip RewardSound;
    public bool IsValid()
    {
        if (Rewards == null || Rewards.Length < 3 || Rewards.Length > 12 || CooldownHours < 1 || CooldownHours > 168) return false;
        foreach (int amount in Rewards) if (amount <= 0) return false;
        return true;
    }
}
