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
    [SerializeField] private Button extraLifeButton;
    [SerializeField] private TMP_Text extraLifeButtonLabel;
    [SerializeField] private Sprite extraLifeIcon;

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
    private bool adsSubscribed;

    private void Awake()
    {
        Initialize();
        HideImmediate();
    }

    private void OnEnable()
    {
        Initialize();
        SubscribeToAds();
        RefreshExtraLifeButton();
    }

    private void OnDisable()
    {
        UnsubscribeFromAds();
    }

    private void OnDestroy()
    {
        KillActiveSequence();

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartButtonPressed);
        }


        if (extraLifeButton != null)
        {
            extraLifeButton.onClick.RemoveListener(OnExtraLifeButtonPressed);
        }

        UnsubscribeFromAds();
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

        CreateExtraLifeButtonIfNeeded();

        if (extraLifeButton != null)
        {
            extraLifeButton.onClick.RemoveListener(OnExtraLifeButtonPressed);
            extraLifeButton.onClick.AddListener(OnExtraLifeButtonPressed);
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

        RefreshExtraLifeButton();

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

            RefreshExtraLifeButton();

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

        if (extraLifeButton != null)
        {
            extraLifeButton.interactable = false;
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

        RefreshExtraLifeButton();

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

    public void OnExtraLifeButtonPressed()
    {
        if (isAnimatingHide || GameManager.Instance == null)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        SetButtonsInteractable(false);

        if (!GameManager.Instance.TryContinueWithRewardedAd())
        {
            SetButtonsInteractable(true);
            RefreshExtraLifeButton();
        }
    }

    public void SetButtonsInteractable(bool interactable)
    {
        if (restartButton != null)
        {
            restartButton.interactable = interactable;
        }

        if (extraLifeButton != null)
        {
            extraLifeButton.interactable = interactable && IsExtraLifeReady();
        }
    }

    public void RefreshExtraLifeButton()
    {
        if (!initialized || extraLifeButton == null)
        {
            return;
        }

        bool canOffer = GameManager.Instance != null && GameManager.Instance.CanOfferExtraLife;
        bool ready = canOffer && IsExtraLifeReady();

        extraLifeButton.gameObject.SetActive(canOffer);
        extraLifeButton.interactable = ready;

        if (extraLifeButtonLabel != null)
        {
            extraLifeButtonLabel.text = ready ? "EKSTRA CAN" : "YÜKLENİYOR";
        }
    }

    private bool IsExtraLifeReady()
    {
        return AdMobManager.Instance != null &&
               AdMobManager.Instance.IsRewardedReady &&
               GameManager.Instance != null &&
               !GameManager.Instance.IsExtraLifeRequestPending;
    }

    private void CreateExtraLifeButtonIfNeeded()
    {
        if (extraLifeButton != null || restartButton == null)
        {
            return;
        }

        GameObject clonedButton = Instantiate(restartButton.gameObject, restartButton.transform.parent);
        clonedButton.name = "ExtraLifeButton";
        extraLifeButton = clonedButton.GetComponent<Button>();

        if (extraLifeButton == null)
        {
            Destroy(clonedButton);
            return;
        }

        extraLifeButton.onClick = new Button.ButtonClickedEvent();

        RectTransform restartRect = restartButton.transform as RectTransform;
        RectTransform extraLifeRect = clonedButton.transform as RectTransform;

        if (restartRect != null && extraLifeRect != null)
        {
            Vector2 centerPosition = restartRect.anchoredPosition;
            restartRect.anchoredPosition = centerPosition + Vector2.right * 150f;
            extraLifeRect.anchoredPosition = centerPosition + Vector2.left * 150f;
        }

        Image[] childImages = clonedButton.GetComponentsInChildren<Image>(true);
        Image iconImage = null;

        for (int i = 0; i < childImages.Length; i++)
        {
            if (childImages[i] != null && childImages[i].gameObject != clonedButton)
            {
                childImages[i].enabled = false;

                if (iconImage == null)
                {
                    iconImage = childImages[i];
                }
            }
        }

        if (iconImage != null && extraLifeIcon != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = extraLifeIcon;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 120f);
            iconRect.sizeDelta = new Vector2(390f, 390f);
            iconRect.localRotation = Quaternion.identity;
            iconRect.localScale = Vector3.one;
        }

        GameObject labelObject = new GameObject(
            "ExtraLifeLabel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(clonedButton.transform, false);

        RectTransform labelRect = labelObject.transform as RectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, -205f);
        labelRect.sizeDelta = new Vector2(760f, 190f);

        extraLifeButtonLabel = labelObject.GetComponent<TextMeshProUGUI>();
        extraLifeButtonLabel.alignment = TextAlignmentOptions.Center;
        extraLifeButtonLabel.fontSize = 68f;
        extraLifeButtonLabel.fontStyle = FontStyles.Bold;
        extraLifeButtonLabel.color = Color.white;
        extraLifeButtonLabel.enableAutoSizing = true;
        extraLifeButtonLabel.fontSizeMin = 34f;
        extraLifeButtonLabel.fontSizeMax = 68f;
        extraLifeButtonLabel.raycastTarget = false;
    }

    private void SubscribeToAds()
    {
        if (adsSubscribed || AdMobManager.Instance == null)
        {
            return;
        }

        AdMobManager.Instance.RewardedAvailabilityChanged += OnRewardedAvailabilityChanged;
        adsSubscribed = true;
    }

    private void UnsubscribeFromAds()
    {
        if (!adsSubscribed)
        {
            return;
        }

        if (AdMobManager.Instance != null)
        {
            AdMobManager.Instance.RewardedAvailabilityChanged -= OnRewardedAvailabilityChanged;
        }

        adsSubscribed = false;
    }

    private void OnRewardedAvailabilityChanged(bool available)
    {
        RefreshExtraLifeButton();
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
