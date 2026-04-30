using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TutorialHandHintUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform pointerRect;
    [SerializeField] private Image pointerImage;

    [Header("Animation")]
    [SerializeField] private float moveDuration = 1.35f;
    [SerializeField] private float waitAtStart = 0.28f;
    [SerializeField] private float waitAtEnd = 0.32f;
    [SerializeField] private float pointerScale = 1f;
    [SerializeField] private float pressScale = 0.86f;
    [SerializeField] private Ease moveEase = Ease.InOutSine;

    [Header("Target Screen Offset")]
    [SerializeField] private Vector2 startTargetScreenOffset = Vector2.zero;
    [SerializeField] private Vector2 endTargetScreenOffset = Vector2.zero;

    [Header("Pointer Visual Alignment")]
    [Tooltip("Sprite'ın görsel ucunu hedefe oturtmak için kullanılır. Eğer image hedefin sağ-altında kalıyorsa X ve Y değerlerini artır/azalt.")]
    [SerializeField] private Vector2 pointerTipOffset = new Vector2(-34f, 34f);

    [Tooltip("Açı verdiğin hand/ok sprite için tip offset dönüşle beraber hesaplansın.")]
    [SerializeField] private bool rotateTipOffsetWithPointer = true;

    [Header("Target Centering")]
    [SerializeField] private bool useRendererBoundsCenter = true;
    [SerializeField] private bool useActiveRingsCenterForPiece = true;
    [SerializeField] private bool useRingParentForCellTarget = true;
    [SerializeField] private float cellTargetYOffset = 0.05f;

    private Camera mainCamera;
    private Sequence activeSequence;

    private void Awake()
    {
        FindMissingReferences();
        HideInstant();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void FindMissingReferences()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        if (pointerRect == null)
        {
            pointerRect = transform as RectTransform;
        }

        if (pointerImage == null)
        {
            pointerImage = GetComponent<Image>();
        }

        mainCamera = Camera.main;
    }

    public void Play(Transform fromWorldTarget, Transform toWorldTarget)
    {
        if (fromWorldTarget == null || toWorldTarget == null)
        {
            HideInstant();
            return;
        }

        FindMissingReferences();
        Stop();

        gameObject.SetActive(true);

        Vector3 fromWorldPosition = GetTargetWorldCenter(fromWorldTarget);
        Vector3 toWorldPosition = GetTargetWorldCenter(toWorldTarget);

        Vector2 startTargetPoint;
        Vector2 endTargetPoint;

        if (!TryWorldToCanvasPoint(fromWorldPosition, out startTargetPoint))
        {
            HideInstant();
            return;
        }

        if (!TryWorldToCanvasPoint(toWorldPosition, out endTargetPoint))
        {
            HideInstant();
            return;
        }

        startTargetPoint += startTargetScreenOffset;
        endTargetPoint += endTargetScreenOffset;

        Vector2 startPointerPoint = GetPointerAnchoredPositionForTarget(startTargetPoint);
        Vector2 endPointerPoint = GetPointerAnchoredPositionForTarget(endTargetPoint);

        pointerRect.anchoredPosition = startPointerPoint;
        pointerRect.localScale = Vector3.one * pointerScale;

        if (pointerImage != null)
        {
            Color color = pointerImage.color;
            color.a = 1f;
            pointerImage.color = color;
        }

        activeSequence = DOTween.Sequence();
        activeSequence.SetUpdate(false);
        activeSequence.SetLoops(-1, LoopType.Restart);

        activeSequence.AppendInterval(waitAtStart);

        activeSequence.Append(
            pointerRect.DOScale(Vector3.one * pressScale, 0.14f)
                .SetEase(Ease.OutQuad)
        );

        activeSequence.Append(
            pointerRect.DOAnchorPos(endPointerPoint, moveDuration)
                .SetEase(moveEase)
        );

        activeSequence.Join(
            pointerRect.DOScale(Vector3.one * pointerScale, moveDuration)
                .SetEase(Ease.OutSine)
        );

        activeSequence.AppendInterval(waitAtEnd);

        activeSequence.Append(
            pointerRect.DOScale(Vector3.one * pressScale, 0.10f)
                .SetEase(Ease.OutQuad)
        );

        activeSequence.Append(
            pointerRect.DOScale(Vector3.one * pointerScale, 0.12f)
                .SetEase(Ease.OutQuad)
        );

        activeSequence.AppendCallback(() =>
        {
            if (pointerRect != null)
            {
                pointerRect.anchoredPosition = startPointerPoint;
                pointerRect.localScale = Vector3.one * pointerScale;
            }
        });
    }

    public void Stop()
    {
        if (activeSequence != null)
        {
            activeSequence.Kill();
            activeSequence = null;
        }

        if (pointerRect != null)
        {
            pointerRect.DOKill();
        }
    }

    public void HideInstant()
    {
        Stop();

        if (pointerImage != null)
        {
            Color color = pointerImage.color;
            color.a = 0f;
            pointerImage.color = color;
        }

        gameObject.SetActive(false);
    }

    private Vector2 GetPointerAnchoredPositionForTarget(Vector2 targetPoint)
    {
        Vector2 offset = pointerTipOffset;

        if (rotateTipOffsetWithPointer && pointerRect != null)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, pointerRect.localEulerAngles.z);
            offset = rotation * pointerTipOffset;
        }

        return targetPoint - offset;
    }

    private Vector3 GetTargetWorldCenter(Transform target)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        Cell cell = target.GetComponent<Cell>();

        if (cell != null && useRingParentForCellTarget)
        {
            Transform ringParent = target.Find("RingParent");

            if (ringParent != null)
            {
                return ringParent.position;
            }

            return target.position + Vector3.up * cellTargetYOffset;
        }

        RingPiece ringPiece = target.GetComponent<RingPiece>();

        if (ringPiece != null)
        {
            if (useActiveRingsCenterForPiece)
            {
                Vector3 activeCenter;
                if (TryGetActiveRingsCenter(ringPiece, out activeCenter))
                {
                    return activeCenter;
                }
            }

            if (useRendererBoundsCenter)
            {
                return GetRendererBoundsCenter(target);
            }
        }

        Ring ring = target.GetComponent<Ring>();

        if (ring != null && useRendererBoundsCenter)
        {
            return GetRendererBoundsCenter(target);
        }

        if (useRendererBoundsCenter)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);

            if (renderers != null && renderers.Length > 0)
            {
                return GetRendererBoundsCenter(target);
            }
        }

        return target.position;
    }

    private bool TryGetActiveRingsCenter(RingPiece ringPiece, out Vector3 center)
    {
        center = ringPiece.transform.position;

        if (ringPiece.Rings == null || ringPiece.Rings.Count == 0)
        {
            return false;
        }

        bool hasRenderer = false;
        Bounds bounds = new Bounds(ringPiece.transform.position, Vector3.zero);

        for (int i = 0; i < ringPiece.Rings.Count; i++)
        {
            Ring ring = ringPiece.Rings[i];

            if (ring == null)
            {
                continue;
            }

            Renderer[] renderers = ring.GetComponentsInChildren<Renderer>(true);

            if (renderers == null || renderers.Length == 0)
            {
                continue;
            }

            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer currentRenderer = renderers[r];

                if (currentRenderer == null)
                {
                    continue;
                }

                if (!currentRenderer.enabled)
                {
                    continue;
                }

                if (!currentRenderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasRenderer)
                {
                    bounds = currentRenderer.bounds;
                    hasRenderer = true;
                }
                else
                {
                    bounds.Encapsulate(currentRenderer.bounds);
                }
            }
        }

        if (!hasRenderer)
        {
            return false;
        }

        center = bounds.center;
        return true;
    }

    private Vector3 GetRendererBoundsCenter(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            return target.position;
        }

        bool hasRenderer = false;
        Bounds bounds = new Bounds(target.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer currentRenderer = renderers[i];

            if (currentRenderer == null)
            {
                continue;
            }

            if (!currentRenderer.enabled)
            {
                continue;
            }

            if (!currentRenderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!hasRenderer)
            {
                bounds = currentRenderer.bounds;
                hasRenderer = true;
            }
            else
            {
                bounds.Encapsulate(currentRenderer.bounds);
            }
        }

        if (!hasRenderer)
        {
            return target.position;
        }

        return bounds.center;
    }

    private bool TryWorldToCanvasPoint(Vector3 worldPosition, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        if (rootCanvas == null)
        {
            return false;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return false;
        }

        Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);

        RectTransform canvasRect = rootCanvas.transform as RectTransform;

        Camera canvasCamera = null;

        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera = rootCanvas.worldCamera;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            canvasCamera,
            out localPoint
        );

        return true;
    }
}