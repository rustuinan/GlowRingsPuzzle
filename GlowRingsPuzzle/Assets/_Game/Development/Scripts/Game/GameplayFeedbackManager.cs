using DG.Tweening;
using UnityEngine;

public class GameplayFeedbackManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform feedbackRoot;
    [SerializeField] private FeedbackPopup feedbackPopupPrefab;

    [Header("Main Text Positions")]
    [SerializeField] private Vector2 mainTextPosition = new Vector2(0f, 210f);
    [SerializeField] private Vector2 comboTextPosition = new Vector2(0f, 120f);
    [SerializeField] private Vector2 scoreTextPosition = new Vector2(0f, 40f);

    [Header("Text Sizes")]
    [SerializeField] private float normalFontSize = 78f;
    [SerializeField] private float comboFontSize = 72f;
    [SerializeField] private float scoreFontSize = 48f;
    [SerializeField] private float allClearFontSize = 96f;

    [Header("Colors")]
    [SerializeField] private Color goodColor = new Color32(255, 235, 130, 255);
    [SerializeField] private Color superColor = new Color32(255, 110, 220, 255);
    [SerializeField] private Color amazingColor = new Color32(120, 220, 255, 255);
    [SerializeField] private Color comboColor = new Color32(255, 245, 150, 255);
    [SerializeField] private Color scoreColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color allClearColor = new Color32(255, 230, 80, 255);

    [Header("Animation")]
    [SerializeField] private float normalDuration = 0.85f;
    [SerializeField] private float comboDuration = 0.78f;
    [SerializeField] private float allClearDuration = 1.15f;
    [SerializeField] private float moveUpAmount = 70f;

    [Header("Camera Punch")]
    [SerializeField] private bool useCameraPunch = true;
    [SerializeField] private float normalCameraPunch = 0.035f;
    [SerializeField] private float allClearCameraPunch = 0.075f;
    [SerializeField] private float cameraPunchDuration = 0.18f;

    private Camera cachedCamera;
    private Vector3 cameraStartPosition;

    private void Awake()
    {
        if (feedbackRoot == null)
        {
            feedbackRoot = transform as RectTransform;
        }
    }

    public void PlayMatchFeedback(int comboCount, int matchCount, int clearedRingCount, bool isAllClear, int gainedScore)
    {
        if (feedbackPopupPrefab == null || feedbackRoot == null)
        {
            Debug.LogWarning("GameplayFeedbackManager: Feedback prefab veya root atanmadı.");
            return;
        }

        if (isAllClear)
        {
            PlayAllClearFeedback(comboCount, gainedScore);
            return;
        }

        string praiseText = GetPraiseText(comboCount, matchCount, clearedRingCount);
        Color praiseColor = GetPraiseColor(comboCount, matchCount, clearedRingCount);

        SpawnPopup(
            praiseText,
            praiseColor,
            normalFontSize,
            mainTextPosition,
            0.65f,
            1.18f,
            0.92f,
            moveUpAmount,
            normalDuration,
            true
        );

        if (comboCount >= 2)
        {
            SpawnPopup(
                comboCount + "x COMBO",
                comboColor,
                comboFontSize,
                comboTextPosition,
                0.60f,
                1.14f,
                0.92f,
                moveUpAmount * 0.75f,
                comboDuration,
                true
            );
        }

        if (gainedScore > 0)
        {
            SpawnPopup(
                "+" + gainedScore,
                scoreColor,
                scoreFontSize,
                scoreTextPosition,
                0.75f,
                1.05f,
                0.90f,
                moveUpAmount * 0.55f,
                normalDuration,
                false
            );
        }

        PunchCamera(normalCameraPunch);
    }

    private void PlayAllClearFeedback(int comboCount, int gainedScore)
    {
        SpawnPopup(
            "ALL CLEAR!",
            allClearColor,
            allClearFontSize,
            mainTextPosition,
            0.45f,
            1.35f,
            1.00f,
            moveUpAmount * 1.2f,
            allClearDuration,
            true
        );

        if (comboCount >= 2)
        {
            SpawnPopup(
                comboCount + "x COMBO",
                comboColor,
                comboFontSize,
                comboTextPosition,
                0.55f,
                1.20f,
                0.95f,
                moveUpAmount,
                allClearDuration * 0.9f,
                true
            );
        }

        if (gainedScore > 0)
        {
            SpawnPopup(
                "+" + gainedScore,
                scoreColor,
                scoreFontSize + 8f,
                scoreTextPosition,
                0.65f,
                1.15f,
                0.95f,
                moveUpAmount * 0.8f,
                allClearDuration,
                true
            );
        }

        PunchCamera(allClearCameraPunch);
    }

    private string GetPraiseText(int comboCount, int matchCount, int clearedRingCount)
    {
        if (comboCount >= 5)
        {
            return "LEGENDARY!";
        }

        if (comboCount >= 4)
        {
            return "AMAZING!";
        }

        if (comboCount >= 3)
        {
            return "SUPER!";
        }

        if (comboCount >= 2)
        {
            return "GREAT!";
        }

        if (matchCount >= 3)
        {
            return "AMAZING!";
        }

        if (matchCount >= 2)
        {
            return "SUPER!";
        }

        if (clearedRingCount >= 5)
        {
            return "GREAT!";
        }

        return "GOOD!";
    }

    private Color GetPraiseColor(int comboCount, int matchCount, int clearedRingCount)
    {
        if (comboCount >= 4 || matchCount >= 3)
        {
            return amazingColor;
        }

        if (comboCount >= 3 || matchCount >= 2)
        {
            return superColor;
        }

        return goodColor;
    }

    private void SpawnPopup(
        string text,
        Color color,
        float fontSize,
        Vector2 anchoredPosition,
        float startScale,
        float peakScale,
        float endScale,
        float moveAmount,
        float duration,
        bool punch)
    {
        FeedbackPopup popup = Instantiate(feedbackPopupPrefab, feedbackRoot);
        popup.Play(
            text,
            color,
            fontSize,
            anchoredPosition,
            startScale,
            peakScale,
            endScale,
            moveAmount,
            duration,
            punch
        );
    }

    private void PunchCamera(float strength)
    {
        if (!useCameraPunch)
        {
            return;
        }

        Camera cam = GetCamera();

        if (cam == null)
        {
            return;
        }

        cam.transform.DOKill();

        cam.transform.localPosition = cameraStartPosition;

        cam.transform
            .DOShakePosition(cameraPunchDuration, strength, 12, 70f, false, true)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (cam != null)
                {
                    cam.transform.localPosition = cameraStartPosition;
                }
            });
    }

    private Camera GetCamera()
    {
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;

            if (cachedCamera != null)
            {
                cameraStartPosition = cachedCamera.transform.localPosition;
            }
        }

        return cachedCamera;
    }
}