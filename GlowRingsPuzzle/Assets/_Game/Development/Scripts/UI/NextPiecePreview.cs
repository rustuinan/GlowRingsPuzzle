using UnityEngine;

public sealed class NextPiecePreview : MonoBehaviour
{
    [SerializeField] private PieceSpawner spawner;
    private Transform display;
    private RingPiece model;

    public RingPieceData PreviewData { get; private set; }
    public RingColorType PreviewColor { get; private set; }

    public void Configure(PieceSpawner source)
    {
        spawner = source;
    }

    public void Show(RingPieceData data, RingColorType color)
    {
        if (data == null)
        {
            PreviewData = null;
            if (display != null) display.gameObject.SetActive(false);
            return;
        }

        Camera camera = Camera.main;
        if (camera == null) return;
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, transform.position);
        Ray ray = camera.ScreenPointToRay(screenPoint);
        var plane = new Plane(Vector3.up, spawner.SpawnPosition);
        if (!plane.Raycast(ray, out float distance)) return;

        if (display == null)
        {
            display = new GameObject("Queued Piece Visual").transform;
            display.SetParent(transform, false);
            var pedestal = Instantiate(spawner.SpawnVisual, display, false);
            StylePedestal(pedestal.GetComponent<MeshRenderer>());
            model = Instantiate(spawner.PiecePrefab, display, false);
            model.transform.localPosition = Vector3.zero;
            foreach (Collider collider in display.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
                Destroy(collider);
            }
        }

        display.SetPositionAndRotation(ray.GetPoint(distance), Quaternion.identity);
        display.localScale = transform.InverseTransformVector(Vector3.one * 0.50f);
        display.gameObject.SetActive(true);
        if (data == PreviewData && color == PreviewColor) return;

        if (!model.Initialize(data, color))
        {
            display.gameObject.SetActive(false);
            return;
        }
        PreviewData = data;
        PreviewColor = color;
    }

    private static void StylePedestal(MeshRenderer renderer)
    {
        // These slots follow the Spawn mesh order: outer shell, neon trim, and inset.
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", new Color32(103, 87, 157, 255));
        properties.SetFloat("_Smoothness", 0.32f);
        renderer.SetPropertyBlock(properties, 0);

        properties.SetColor("_BaseColor", new Color32(206, 164, 255, 255));
        properties.SetColor("_EmissionColor", new Color(0.42f, 0.17f, 0.68f));
        renderer.SetPropertyBlock(properties, 1);

        properties.Clear();
        properties.SetColor("_BaseColor", new Color32(145, 125, 191, 255));
        properties.SetFloat("_Smoothness", 0.23f);
        renderer.SetPropertyBlock(properties, 2);
    }
}
