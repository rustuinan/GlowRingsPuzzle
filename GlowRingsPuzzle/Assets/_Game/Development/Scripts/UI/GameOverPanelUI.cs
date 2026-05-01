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

    [Header("Show Animation")]
    [SerializeField] private float showDuration = 0.32f;
    [SerializeField] private float startScale = 0.88f;
    [SerializeField] private float endScale = 1f;
    [SerializeField] private float startYOffset = -40f;
    [SerializeField] private Ease fadeEase = Ease.OutCubic;
    [SerializeField] private Ease scaleEase = Ease.OutBack;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Header("Hide Animation")]
    [SerializeField] private float hideDuration = 0.24f;
    [SerializeField] private float hideScale = 0.92f;
    [SerializeField] private float hideYOffset = -55f;
    [SerializeField] private Ease hideFadeEase = Ease.InCubic;
    [SerializeField] private Ease hideScaleEase = Ease.InBack;
    [SerializeField] private Ease hideMoveEase = Ease.InCubic;

    private Vector2 animatedRootDefaultAnchoredPosition;
    private Sequence activeSequence;
    private bool initialized;
    private bool isAnimatingHide;

    private void Awake()
    {
        Initialize();
        HideImmediate();
    }

    private void OnDestroy()
    {
        KillActiveSequence();

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
        KillActiveSequence();

        isAnimatingHide = false;
        gameObject.SetActive(true);

        if (highScoreValueText != null)
        {
            highScoreValueText.text = highScore.ToString();
        }

        if (scoreValueText != null)
        {
            scoreValueText.text = score.ToString();
        }

        if (restartButton != null)
        {
            restartButton.interactable = false;
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        if (animatedRoot != null)
        {
            animatedRoot.localScale = Vector3.one * startScale;
            animatedRoot.anchoredPosition = animatedRootDefaultAnchoredPosition + new Vector2(0f, startYOffset);
        }

        activeSequence = DOTween.Sequence();
        activeSequence.SetUpdate(true);

        if (rootCanvasGroup != null)
        {
            activeSequence.Join(
                rootCanvasGroup.DOFade(1f, showDuration).SetEase(fadeEase)
            );
        }

        if (animatedRoot != null)
        {
            activeSequence.Join(
                animatedRoot.DOScale(endScale, showDuration).SetEase(scaleEase)
            );

            activeSequence.Join(
                animatedRoot.DOAnchorPos(animatedRootDefaultAnchoredPosition, showDuration).SetEase(moveEase)
            );
        }

        activeSequence.OnComplete(() =>
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.interactable = true;
                rootCanvasGroup.blocksRaycasts = true;
            }

            if (restartButton != null)
            {
                restartButton.interactable = true;
            }

            activeSequence = null;
        });
    }

    public void HideAnimated(System.Action onComplete)
    {
        Initialize();

        if (isAnimatingHide)
        {
            return;
        }

        isAnimatingHide = true;
        KillActiveSequence();

        if (restartButton != null)
        {
            restartButton.interactable = false;
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        activeSequence = DOTween.Sequence();
        activeSequence.SetUpdate(true);

        if (rootCanvasGroup != null)
        {
            activeSequence.Join(
                rootCanvasGroup.DOFade(0f, hideDuration).SetEase(hideFadeEase)
            );
        }

        if (animatedRoot != null)
        {
            activeSequence.Join(
                animatedRoot.DOScale(hideScale, hideDuration).SetEase(hideScaleEase)
            );

            activeSequence.Join(
                animatedRoot.DOAnchorPos(
                    animatedRootDefaultAnchoredPosition + new Vector2(0f, hideYOffset),
                    hideDuration
                ).SetEase(hideMoveEase)
            );
        }

        activeSequence.OnComplete(() =>
        {
            isAnimatingHide = false;

            if (animatedRoot != null)
            {
                animatedRoot.localScale = Vector3.one;
                animatedRoot.anchoredPosition = animatedRootDefaultAnchoredPosition;
            }

            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 0f;
                rootCanvasGroup.interactable = false;
                rootCanvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
            activeSequence = null;

            if (onComplete != null)
            {
                onComplete.Invoke();
            }
        });
    }

    public void HideImmediate()
    {
        Initialize();
        KillActiveSequence();

        isAnimatingHide = false;

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        if (animatedRoot != null)
        {
            animatedRoot.localScale = Vector3.one;
            animatedRoot.anchoredPosition = animatedRootDefaultAnchoredPosition;
        }

        if (restartButton != null)
        {
            restartButton.interactable = true;
        }

        gameObject.SetActive(false);
    }

    public void OnRestartButtonPressed()
    {
        if (isAnimatingHide)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        HideAnimated(() =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        });
    }

    private void KillActiveSequence()
    {
        if (activeSequence != null)
        {
            activeSequence.Kill();
            activeSequence = null;
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.DOKill();
        }

        if (animatedRoot != null)
        {
            animatedRoot.DOKill();
        }
    }
}