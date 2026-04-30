using DG.Tweening;
using UnityEngine;

public class RingPieceDragLift : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;

    [Header("Lift Animation")]
    [SerializeField] private float liftHeight = 0.22f;
    [SerializeField] private float liftDuration = 0.16f;
    [SerializeField] private float dropDuration = 0.14f;

    [Header("Visual Polish")]
    [SerializeField] private bool useScale = true;
    [SerializeField] private float liftedScale = 1.04f;

    private Vector3 startLocalPosition;
    private Vector3 startLocalScale;
    private Sequence activeSequence;
    private bool initialized;
    private bool isLifted;

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        KillTween();
        RestoreInstant();
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        if (visualRoot == null)
        {
            visualRoot = transform;
            Debug.LogWarning("RingPieceDragLift: VisualRoot atanmadı. Şimdilik root kullanılacak ama önerilen yapı Piece/VisualRoot child objesidir.");
        }

        startLocalPosition = visualRoot.localPosition;
        startLocalScale = visualRoot.localScale;

        initialized = true;
    }

    public void Lift()
    {
        Initialize();

        if (visualRoot == null)
        {
            return;
        }

        if (isLifted)
        {
            return;
        }

        isLifted = true;

        KillTween();

        Vector3 targetPosition = startLocalPosition + Vector3.up * liftHeight;
        Vector3 targetScale = useScale ? startLocalScale * liftedScale : startLocalScale;

        activeSequence = DOTween.Sequence();
        activeSequence.SetUpdate(false);

        activeSequence.Join(
            visualRoot.DOLocalMove(targetPosition, liftDuration)
                .SetEase(Ease.OutCubic)
        );

        activeSequence.Join(
            visualRoot.DOScale(targetScale, liftDuration)
                .SetEase(Ease.OutCubic)
        );
    }

    public void Drop()
    {
        Initialize();

        if (visualRoot == null)
        {
            return;
        }

        isLifted = false;

        KillTween();

        activeSequence = DOTween.Sequence();
        activeSequence.SetUpdate(false);

        activeSequence.Join(
            visualRoot.DOLocalMove(startLocalPosition, dropDuration)
                .SetEase(Ease.InOutSine)
        );

        activeSequence.Join(
            visualRoot.DOScale(startLocalScale, dropDuration)
                .SetEase(Ease.InOutSine)
        );
    }

    public void RestoreInstant()
    {
        Initialize();

        if (visualRoot == null)
        {
            return;
        }

        isLifted = false;

        KillTween();

        visualRoot.localPosition = startLocalPosition;
        visualRoot.localScale = startLocalScale;
    }

    private void KillTween()
    {
        if (activeSequence != null)
        {
            activeSequence.Kill();
            activeSequence = null;
        }

        if (visualRoot != null)
        {
            visualRoot.DOKill();
        }
    }
}