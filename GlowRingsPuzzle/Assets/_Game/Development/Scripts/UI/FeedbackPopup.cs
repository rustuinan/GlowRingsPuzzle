using DG.Tweening;
using TMPro;
using UnityEngine;

public class FeedbackPopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text popupText;

    private Sequence activeSequence;

    private void Awake()
    {
        FindMissingReferences();
    }

    private void OnDisable()
    {
        KillTween();
    }

    private void FindMissingReferences()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (popupText == null)
        {
            popupText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    public void Play(
        string text,
        Color color,
        float fontSize,
        Vector2 anchoredPosition,
        float startScale,
        float peakScale,
        float endScale,
        float moveUpAmount,
        float duration,
        bool punch)
    {
        FindMissingReferences();
        KillTween();

        if (popupText == null || rectTransform == null || canvasGroup == null)
        {
            Destroy(gameObject);
            return;
        }

        popupText.text = text;
        popupText.color = color;
        popupText.fontSize = fontSize;

        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one * startScale;

        canvasGroup.alpha = 0f;

        float introDuration = duration * 0.22f;
        float holdDuration = duration * 0.34f;
        float outroDuration = duration * 0.44f;

        Vector2 targetPosition = anchoredPosition + Vector2.up * moveUpAmount;

        activeSequence = DOTween.Sequence();
        activeSequence.SetUpdate(false);

        activeSequence.Append(canvasGroup.DOFade(1f, introDuration).SetEase(Ease.OutQuad));

        activeSequence.Join(
            rectTransform.DOScale(Vector3.one * peakScale, introDuration)
                .SetEase(Ease.OutBack, punch ? 1.55f : 1.15f)
        );

        activeSequence.AppendInterval(holdDuration);

        activeSequence.Append(
            rectTransform.DOAnchorPos(targetPosition, outroDuration)
                .SetEase(Ease.OutSine)
        );

        activeSequence.Join(
            rectTransform.DOScale(Vector3.one * endScale, outroDuration)
                .SetEase(Ease.InOutSine)
        );

        activeSequence.Join(
            canvasGroup.DOFade(0f, outroDuration)
                .SetEase(Ease.InQuad)
        );

        activeSequence.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }

    private void KillTween()
    {
        if (activeSequence != null)
        {
            activeSequence.Kill();
            activeSequence = null;
        }

        if (rectTransform != null)
        {
            rectTransform.DOKill();
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }
    }
}