using UnityEngine;

public static class SaveManager
{
    private const string HighScoreKey = "GlowRings.HighScore";

    public static void SaveHighScore(int value)
    {
        PlayerPrefs.SetInt(HighScoreKey, value);
        PlayerPrefs.Save();
    }

    public static int LoadHighScore()
    {
        return PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(HighScoreKey);
        PlayerPrefs.Save();
    }
}
