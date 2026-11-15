using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;

    private int score;
    private int highScore;

    public int Score => score;
    public int HighScore => highScore;

    private void Awake()
    {
        Instance = this;
        highScore = SaveManager.LoadHighScore();
        UpdateUI();
    }

    public void ResetScore()
    {
        score = 0;
        UpdateUI();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
            return;

        score += amount;

        if (score > highScore)
        {
            highScore = score;
            SaveManager.SaveHighScore(highScore);
        }

        UpdateUI();
    }

    public int GetScore()
    {
        return score;
    }

    internal MovePuzzleUI CreatePuzzleUI()
    {
        var frame = scoreText.GetComponentInParent<ReferenceFrameFit>().transform;
        scoreText.transform.parent.gameObject.SetActive(false);
        highScoreText.transform.parent.gameObject.SetActive(false);
        return MovePuzzleUI.Create(frame, scoreText.font);
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();

        if (highScoreText != null)
            highScoreText.text = highScore.ToString();
    }
}
