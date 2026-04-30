using UnityEngine;

public class Cell : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cellVisual;
    [SerializeField] private Transform ringParent;
    [SerializeField] private Renderer cellRenderer;

    [Header("Runtime Rings")]
    [SerializeField] private Ring outerRing;
    [SerializeField] private Ring middleRing;
    [SerializeField] private Ring innerRing;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private float gizmoSize = 0.45f;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 1f, 0.35f);
    [SerializeField] private Color ringParentGizmoColor = new Color(1f, 0.65f, 0f, 0.75f);

    public Transform RingParent
    {
        get { return ringParent; }
    }

    private void Awake()
    {
        FindMissingReferences();
    }

    private void OnValidate()
    {
        FindMissingReferences();
    }

    private void FindMissingReferences()
    {
        if (ringParent == null)
        {
            Transform foundRingParent = transform.Find("RingParent");

            if (foundRingParent != null)
            {
                ringParent = foundRingParent;
            }
        }

        if (cellVisual == null)
        {
            Transform foundCellVisual = transform.Find("CellVisual");

            if (foundCellVisual != null)
            {
                cellVisual = foundCellVisual;
            }
        }

        if (cellRenderer == null && cellVisual != null)
        {
            cellRenderer = cellVisual.GetComponentInChildren<Renderer>(true);
        }
    }

    public bool CanPlace(RingPiece piece)
    {
        if (piece == null)
        {
            return false;
        }

        IReadOnlyListProxy rings = new IReadOnlyListProxy(piece.Rings);

        for (int i = 0; i < rings.Count; i++)
        {
            Ring ring = rings.Get(i);

            if (ring == null)
            {
                continue;
            }

            if (HasRing(ring.Layer))
            {
                return false;
            }
        }

        return true;
    }

    public void PlacePiece(RingPiece piece)
    {
        if (piece == null)
        {
            return;
        }

        if (ringParent == null)
        {
            Debug.LogError("Cell: RingParent atanmadı. Cell: " + gameObject.name);
            return;
        }

        if (!CanPlace(piece))
        {
            Debug.LogWarning("Cell: Bu piece bu cell'e yerleştirilemez. Cell: " + gameObject.name);
            return;
        }

        IReadOnlyListProxy rings = new IReadOnlyListProxy(piece.Rings);

        for (int i = 0; i < rings.Count; i++)
        {
            Ring ring = rings.Get(i);

            if (ring == null)
            {
                continue;
            }

            ring.transform.SetParent(ringParent);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localRotation = Quaternion.identity;
            ring.transform.localScale = Vector3.one;

            RegisterRing(ring.Layer, ring);
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
        if (layer == RingLayer.Outer)
        {
            return outerRing;
        }

        if (layer == RingLayer.Middle)
        {
            return middleRing;
        }

        if (layer == RingLayer.Inner)
        {
            return innerRing;
        }

        return null;
    }

    public Ring[] GetAllRings()
    {
        int count = 0;

        if (outerRing != null)
        {
            count++;
        }

        if (middleRing != null)
        {
            count++;
        }

        if (innerRing != null)
        {
            count++;
        }

        Ring[] rings = new Ring[count];
        int index = 0;

        if (outerRing != null)
        {
            rings[index] = outerRing;
            index++;
        }

        if (middleRing != null)
        {
            rings[index] = middleRing;
            index++;
        }

        if (innerRing != null)
        {
            rings[index] = innerRing;
            index++;
        }

        return rings;
    }

    public void RemoveRing(RingLayer layer)
    {
        if (layer == RingLayer.Outer)
        {
            outerRing = null;
        }
        else if (layer == RingLayer.Middle)
        {
            middleRing = null;
        }
        else if (layer == RingLayer.Inner)
        {
            innerRing = null;
        }
    }

    public void ClearRing(RingLayer layer)
    {
        Ring ring = GetRing(layer);

        if (ring != null)
        {
            Destroy(ring.gameObject);
        }

        RemoveRing(layer);
    }

    public void ClearAllRings()
    {
        ClearRing(RingLayer.Outer);
        ClearRing(RingLayer.Middle);
        ClearRing(RingLayer.Inner);
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
            FindMissingReferences();
        }

        if (ringParent == null)
        {
            Debug.LogError("Cell: RingParent atanmadı. Cell: " + gameObject.name);
            return;
        }

        ClearRing(layer);

        Ring ring = Instantiate(ringPrefab, ringParent);
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale = Vector3.one;

        if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
        {
            ring.Initialize(layer, colorType, ThemeManager.Instance.CurrentTheme);
        }
        else
        {
            ring.Initialize(layer, colorType, true);
        }

        RegisterRing(layer, ring);
    }

    public void ApplyCellMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (cellRenderer == null)
        {
            FindMissingReferences();
        }

        if (cellRenderer != null)
        {
            cellRenderer.sharedMaterial = material;
        }
    }

    public void ApplyEditorDesign(Vector3 visualScale, Vector3 ringParentLocalPosition)
    {
        FindMissingReferences();

        if (cellVisual != null)
        {
            cellVisual.localScale = visualScale;
        }

        if (ringParent != null)
        {
            ringParent.localPosition = ringParentLocalPosition;
            ringParent.localRotation = Quaternion.identity;
            ringParent.localScale = Vector3.one;
        }
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

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position, new Vector3(gizmoSize, 0.02f, gizmoSize));

        if (ringParent != null)
        {
            Gizmos.color = ringParentGizmoColor;
            Gizmos.DrawSphere(ringParent.position, 0.05f);
        }
    }

    private struct IReadOnlyListProxy
    {
        private readonly System.Collections.Generic.IReadOnlyList<Ring> source;

        public int Count
        {
            get
            {
                if (source == null)
                {
                    return 0;
                }

                return source.Count;
            }
        }

        public IReadOnlyListProxy(System.Collections.Generic.IReadOnlyList<Ring> sourceList)
        {
            source = sourceList;
        }

        public Ring Get(int index)
        {
            if (source == null)
            {
                return null;
            }

            if (index < 0 || index >= source.Count)
            {
                return null;
            }

            return source[index];
        }
    }
}