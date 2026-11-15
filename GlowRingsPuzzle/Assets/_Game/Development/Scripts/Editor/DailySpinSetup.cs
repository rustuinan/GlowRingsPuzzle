using UnityEditor;
using UnityEngine;

public static class DailySpinSetup
{
    [MenuItem("Tools/Glow Rings/Install Daily Spin")]
    public static void Run()
    {
        const string path = "Assets/_Game/Resources/DailySpinConfig.asset";
        var config = AssetDatabase.LoadAssetAtPath<DailySpinConfig>(path);
        if (config == null) { config = ScriptableObject.CreateInstance<DailySpinConfig>(); AssetDatabase.CreateAsset(config, path); }
        config.CoinIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Coins/Coin.png");
        config.SpinSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GUIPackCartoon/Demo/Sounds/Spin Wheel.wav");
        config.RewardSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GUIPackCartoon/Demo/Sounds/Coins.wav");
        EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
        Debug.Log("Daily Spin installed: 8 coin prizes, persistent daily cooldown and recoverable spin rewards.");
    }
}
