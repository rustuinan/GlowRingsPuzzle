using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

/// <summary>Uses the actual gameplay prefab, models and materials at the NEXT UI position.</summary>
[RequireComponent(typeof(RectTransform))]
public class NextPiecePreview : MonoBehaviour
{
    [SerializeField] private PieceSpawner spawner;
    [SerializeField] private TMP_Text title;
    private RingPiece model;
    private Transform referenceSurface;
    private RingPieceData shownData;
    private RingColorType shownColor;
    private ThemeData shownTheme;
    private readonly Vector3[] corners = new Vector3[4];
    public RingPiece PreviewPiece => model;
    public Transform ReferenceSurface => referenceSurface;

    public void Configure(PieceSpawner value, TMP_Text titleText) { spawner = value; title = titleText; }

    public void Show(RingPieceData data, RingColorType color)
    {
        ThemeData theme = ThemeManager.Instance != null ? ThemeManager.Instance.CurrentTheme : null;
        if (data == null || theme == null || spawner == null || spawner.PiecePrefab == null)
        {
            Clear();
            return;
        }
        if (model == null || data != shownData || color != shownColor || theme != shownTheme)
        {
            Clear();
            model = Instantiate(spawner.PiecePrefab);
            model.name = "Next Piece Preview (non-interactive)";
            if (!model.Initialize(data, theme, color)) { Clear(); return; }
            if (spawner.SpawnVisual != null)
            {
                referenceSurface = Instantiate(spawner.SpawnVisual);
                referenceSurface.name = "Next Piece Size Reference";
                foreach (Transform child in referenceSurface.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 2;
                foreach (Collider collider in referenceSurface.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                foreach (Renderer renderer in referenceSurface.GetComponentsInChildren<Renderer>(true))
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 2;
            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            shownData = data; shownColor = color; shownTheme = theme;
            if (title != null)
            {
                title.text = data.GetLayerCount() != 1 ? "NEXT" : data.HasOuter ? "NEXT • LARGE" :
                    data.HasMiddle ? "NEXT • MEDIUM" : "NEXT • SMALL";
            }
        }
        PositionModel();
    }

    private void PositionModel()
    {
        Camera camera = Camera.main;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (camera == null || canvas == null || model == null) return;
        Camera uiCamera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
        RectTransform rect = (RectTransform)transform;
        rect.GetWorldCorners(corners);
        Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(uiCamera, rect.TransformPoint(rect.rect.center));
        Vector2 screenLeft = RectTransformUtility.WorldToScreenPoint(uiCamera, (corners[0] + corners[1]) * 0.5f);
        Vector2 screenRight = RectTransformUtility.WorldToScreenPoint(uiCamera, (corners[2] + corners[3]) * 0.5f);
        Plane plane = new Plane(Vector3.up, spawner.SpawnPosition);
        if (!HitPlane(camera, plane, screenCenter, out Vector3 center) ||
            !HitPlane(camera, plane, screenLeft, out Vector3 left) ||
            !HitPlane(camera, plane, screenRight, out Vector3 right)) return;
        model.transform.SetPositionAndRotation(center, Quaternion.identity);
        // Fit the same gameplay pedestal for every shape, never the active ring's bounds.
        Renderer source = spawner.SpawnVisual != null ? spawner.SpawnVisual.GetComponentInChildren<Renderer>() : null;
        float referenceWidth = source != null ? source.bounds.size.x : 1.2f;
        float scale = Vector3.Distance(left, right) / Mathf.Max(referenceWidth, 0.01f);
        model.transform.localScale = spawner.PiecePrefab.transform.localScale * scale;
        if (referenceSurface != null)
        {
            referenceSurface.SetPositionAndRotation(center + (spawner.SpawnVisual.position - spawner.SpawnPosition) * scale,
                spawner.SpawnVisual.rotation);
            referenceSurface.localScale = spawner.SpawnVisual.lossyScale * scale;
        }
    }

    private static bool HitPlane(Camera camera, Plane plane, Vector2 screen, out Vector3 point)
    {
        Ray ray = camera.ScreenPointToRay(screen);
        if (plane.Raycast(ray, out float distance)) { point = ray.GetPoint(distance); return true; }
        point = Vector3.zero;
        return false;
    }

    private void OnDisable() { Clear(); }
    private void OnDestroy() { Clear(); }
    private void Clear()
    {
        if (referenceSurface != null)
        {
            referenceSurface.gameObject.SetActive(false);
            Destroy(referenceSurface.gameObject);
        }
        referenceSurface = null;
        if (model != null)
        {
            model.gameObject.SetActive(false);
            Destroy(model.gameObject);
        }
        model = null; shownData = null; shownTheme = null;
    }
}
