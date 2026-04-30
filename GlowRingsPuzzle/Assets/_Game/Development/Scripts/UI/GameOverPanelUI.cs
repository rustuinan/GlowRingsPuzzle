using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private RectTransform animatedRoot;
    [SerializeField] private TMP_Text highScoreValueText;
    [SerializeField] private TMP_Text scoreValueText;
    [SerializeField] private Button restartButton;

    [Header("Animation")]
    [SerializeField] private float showDuration = 0.32f;
    [SerializeField] private float startScale = 0.88f;
    [SerializeField] private float endScale = 1f;
    [SerializeField] private float startYOffset = -40f;
    [SerializeField] private Ease fadeEase = Ease.OutCubic;
    [SerializeField] private Ease scaleEase = Ease.OutBack;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    private Vector2 animatedRootDefaultAnchoredPosition;
    private bool initialized;

    private void Awake()
    {
        Initialize();
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartButtonPressed);
        }
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        if (rootCanvasGroup == null)
        {
            rootCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (animatedRoot == null)
        {
            animatedRoot = transform as RectTransform;
        }

        if (animatedRoot != null)
        {
            animatedRootDefaultAnchoredPosition = animatedRoot.anchoredPosition;
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartButtonPressed);
            restartButton.onClick.AddListener(OnRestartButtonPressed);
        }

        initialized = true;
    }

    public void Show(int score, int highScore)
    {
        Initialize();

        gameObject.SetActive(true);

        if (highScoreValueText != null)
        {
            highScoreValueText.text = highScore.ToString();
        }

        if (scoreValueText != null)
        {
            scoreValueText.text = score.ToString();
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.DOKill();
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        if (animatedRoot != null)
        {
            animatedRoot.DOKill();
            animatedRoot.localScale = Vector3.one * startScale;
            animatedRoot.anchoredPosition = animatedRootDefaultAnchoredPosition + new Vector2(0f, startYOffset);
        }

        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        if (rootCanvasGroup != null)
        {
            sequence.Join(
                rootCanvasGroup.DOFade(1f, showDuration).SetEase(fadeEase)
            );
        }

        if (animatedRoot != null)
        {
            sequence.Join(
                animatedRoot.DOScale(endScale, showDuration).SetEase(scaleEase)
            );

            sequence.Join(
                animatedRoot.DOAnchorPos(animatedRootDefaultAnchoredPosition, showDuration).SetEase(moveEase)
            );
        }

        sequence.OnComplete(() =>
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.interactable = true;
                rootCanvasGroup.blocksRaycasts = true;
            }
        });
    }

    public void HideImmediate()
    {
        Initialize();

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.DOKill();
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        if (animatedRoot != null)
        {
            animatedRoot.DOKill();
            animatedRoot.localScale = Vector3.one;
            animatedRoot.anchoredPosition = animatedRootDefaultAnchoredPosition;
        }

        gameObject.SetActive(false);
    }

    public void OnRestartButtonPressed()
    {
        if (GameManager.Instance != null)
        {
            HideImmediate();
            GameManager.Instance.RestartGame();
        }
    }
}