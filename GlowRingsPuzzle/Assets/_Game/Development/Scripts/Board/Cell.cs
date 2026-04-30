using UnityEngine;

[ExecuteAlways]
public class Cell : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cellVisual;
    [SerializeField] private Transform ringParent;
    [SerializeField] private Renderer cellRenderer;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private float gizmoSize = 0.45f;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.6f, 0.35f);
    [SerializeField] private Color ringParentGizmoColor = new Color(1f, 0.6f, 0f, 1f);

    private Ring outerRing;
    private Ring middleRing;
    private Ring innerRing;

    public Transform RingParent => ringParent;

    public bool CanPlace(RingPiece piece)
    {
        if (piece == null)
            return false;

        for (int i = 0; i < piece.Rings.Count; i++)
        {
            Ring ring = piece.Rings[i];

            if (ring != null && HasRing(ring.Layer))
                return false;
        }

        return true;
    }

    public void PlacePiece(RingPiece piece)
    {
        if (piece == null)
            return;

        Transform targetParent = ringParent != null ? ringParent : transform;

        for (int i = 0; i < piece.Rings.Count; i++)
        {
            Ring ring = piece.Rings[i];

            if (ring == null)
                continue;

            ring.transform.SetParent(targetParent);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localRotation = Quaternion.identity;
            ring.transform.localScale = Vector3.one;

            SetRing(ring);
        }

        piece.MarkPlaced();
        Destroy(piece.gameObject);
    }

    public bool HasRing(RingLayer layer)
    {
        return GetRing(layer) != null;
    }

    public Ring GetRing(RingLayer layer)
    {
        switch (layer)
        {
            case RingLayer.Outer:
                return outerRing;

            case RingLayer.Middle:
                return middleRing;

            case RingLayer.Inner:
                return innerRing;

            default:
                return null;
        }
    }

    public Ring[] GetAllRings()
    {
        return new Ring[]
        {
            outerRing,
            middleRing,
            innerRing
        };
    }

    public void ClearRing(RingLayer layer)
    {
        RemoveRing(layer);
    }

    public void RemoveRing(RingLayer layer)
    {
        Ring ring = GetRing(layer);

        if (ring != null)
            Destroy(ring.gameObject);

        switch (layer)
        {
            case RingLayer.Outer:
                outerRing = null;
                break;

            case RingLayer.Middle:
                middleRing = null;
                break;

            case RingLayer.Inner:
                innerRing = null;
                break;
        }
    }

    public void ApplyCellMaterial(Material material)
    {
        if (cellRenderer != null && material != null)
            cellRenderer.material = material;
    }

    public void ApplyEditorDesign(Vector3 visualScale, Vector3 ringParentLocalPosition)
    {
        if (cellVisual != null)
            cellVisual.localScale = visualScale;

        if (ringParent != null)
            ringParent.localPosition = ringParentLocalPosition;
    }

    private void SetRing(Ring ring)
    {
        switch (ring.Layer)
        {
            case RingLayer.Outer:
                outerRing = ring;
                break;

            case RingLayer.Middle:
                middleRing = ring;
                break;

            case RingLayer.Inner:
                innerRing = ring;
                break;
        }
    }

    public void ForcePlaceRing(Ring ringPrefab, RingLayer layer, RingColorType colorType)
    {
        if (ringPrefab == null)
        {
            Debug.LogWarning("Cell: ForcePlaceRing için ringPrefab null.");
            return;
        }

        if (ringParent == null)
        {
            Debug.LogWarning("Cell: RingParent atanmadı.");
            return;
        }

        ClearRing(layer);

        Ring ring = Instantiate(ringPrefab, ringParent);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale = Vector3.one;

        ring.Initialize(layer, colorType, true);

        RegisterRing(layer, ring);
    }

    public void ClearAllRings()
    {
        ClearRing(RingLayer.Outer);
        ClearRing(RingLayer.Middle);
        ClearRing(RingLayer.Inner);
    }

    private void RegisterRing(RingLayer layer, Ring ring)
    {
        if (layer == RingLayer.Outer)
        {
            outerRing = ring;
        }
        else if (layer == RingLayer.Middle)
        {
            middleRing = ring;
        }
        else if (layer == RingLayer.Inner)
        {
            innerRing = ring;
        }
    }

    private void OnValidate()
    {
        if (cellRenderer == null && cellVisual != null)
            cellRenderer = cellVisual.GetComponent<Renderer>();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.02f, new Vector3(gizmoSize * 2f, 0.04f, gizmoSize * 2f));

        Transform targetRingParent = ringParent != null ? ringParent : transform;

        Gizmos.color = ringParentGizmoColor;
        Gizmos.DrawSphere(targetRingParent.position, 0.055f);
        Gizmos.DrawLine(transform.position, targetRingParent.position);
    }
}