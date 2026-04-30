using System.Collections;
using UnityEngine;

public class MatchCellEffect : MonoBehaviour
{
    [Header("Beams")]
    [SerializeField] private MeshFilter beamAMeshFilter;
    [SerializeField] private MeshRenderer beamARenderer;

    [SerializeField] private MeshFilter beamBMeshFilter;
    [SerializeField] private MeshRenderer beamBRenderer;

    [SerializeField] private MeshFilter beamCMeshFilter;
    [SerializeField] private MeshRenderer beamCRenderer;

    [Header("Flares")]
    [SerializeField] private MeshFilter centerFlareMeshFilter;
    [SerializeField] private MeshRenderer centerFlareRenderer;

    [SerializeField] private MeshFilter outerFlareMeshFilter;
    [SerializeField] private MeshRenderer outerFlareRenderer;

    private MatchEffectSettings settings;
    private Vector3 centerPoint;

    private Mesh beamAMesh;
    private Mesh beamBMesh;
    private Mesh beamCMesh;
    private Mesh quadMesh;

    private MaterialPropertyBlock beamBlock;
    private MaterialPropertyBlock flareBlock;

    private Coroutine playRoutine;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");
    private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int RotationId = Shader.PropertyToID("_Rotation");

    private void Awake()
    {
        beamBlock = new MaterialPropertyBlock();
        flareBlock = new MaterialPropertyBlock();

        CreateMeshes();
        AssignMeshes();
    }

    public void Initialize(MatchEffectSettings effectSettings, Vector3 worldCenter)
    {
        settings = effectSettings;
        centerPoint = worldCenter;
        transform.position = centerPoint;

        CreateMeshes();
        AssignMeshes();

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayRoutine());
    }

    private void CreateMeshes()
    {
        if (beamAMesh == null)
        {
            beamAMesh = new Mesh();
            beamAMesh.name = "Cell Beam A Mesh";
        }

        if (beamBMesh == null)
        {
            beamBMesh = new Mesh();
            beamBMesh.name = "Cell Beam B Mesh";
        }

        if (beamCMesh == null)
        {
            beamCMesh = new Mesh();
            beamCMesh.name = "Cell Beam C Mesh";
        }

        if (quadMesh == null)
        {
            quadMesh = new Mesh();
            quadMesh.name = "Cell Flare Quad Mesh";

            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(-0.5f, -0.5f, 0f);
            vertices[1] = new Vector3(-0.5f, 0.5f, 0f);
            vertices[2] = new Vector3(0.5f, 0.5f, 0f);
            vertices[3] = new Vector3(0.5f, -0.5f, 0f);

            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(0f, 0f);
            uvs[1] = new Vector2(0f, 1f);
            uvs[2] = new Vector2(1f, 1f);
            uvs[3] = new Vector2(1f, 0f);

            int[] triangles = new int[6];
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 0;
            triangles[4] = 2;
            triangles[5] = 3;

            quadMesh.vertices = vertices;
            quadMesh.uv = uvs;
            quadMesh.triangles = triangles;
            quadMesh.RecalculateBounds();
        }
    }

    private void AssignMeshes()
    {
        if (beamAMeshFilter != null)
        {
            beamAMeshFilter.sharedMesh = beamAMesh;
        }

        if (beamBMeshFilter != null)
        {
            beamBMeshFilter.sharedMesh = beamBMesh;
        }

        if (beamCMeshFilter != null)
        {
            beamCMeshFilter.sharedMesh = beamCMesh;
        }

        if (centerFlareMeshFilter != null)
        {
            centerFlareMeshFilter.sharedMesh = quadMesh;
        }

        if (outerFlareMeshFilter != null)
        {
            outerFlareMeshFilter.sharedMesh = quadMesh;
        }
    }

    private IEnumerator PlayRoutine()
    {
        if (settings == null)
        {
            Destroy(gameObject);
            yield break;
        }

        SetRendererEnabled(beamARenderer, true);
        SetRendererEnabled(beamBRenderer, true);
        SetRendererEnabled(beamCRenderer, true);
        SetRendererEnabled(centerFlareRenderer, true);
        SetRendererEnabled(outerFlareRenderer, true);

        float duration = settings.GetSafeDuration();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float pulse = Mathf.Sin(t * Mathf.PI);
            float fade = 1f - Mathf.SmoothStep(0.68f, 1f, t);
            float alpha = Mathf.Clamp01(pulse * fade);

            float length = Mathf.Lerp(settings.cellBeamStartLength, settings.cellBeamPeakLength, pulse);
            float width = Mathf.Lerp(settings.cellBeamStartWidth, settings.cellBeamPeakWidth, pulse);
            width = Mathf.Lerp(width, settings.cellBeamEndWidth, t);

            Vector3 directionA = Vector3.right;
            Vector3 directionB = Vector3.forward;
            Vector3 directionC = (Vector3.right + Vector3.forward).normalized;

            BuildBeamMesh(beamAMesh, directionA, length, width);
            BuildBeamMesh(beamBMesh, directionB, length * 0.92f, width * 0.82f);
            BuildBeamMesh(beamCMesh, directionC, length * 0.78f, width * 0.62f);

            ApplyBeamProperties(beamARenderer, alpha);
            ApplyBeamProperties(beamBRenderer, alpha * 0.72f);
            ApplyBeamProperties(beamCRenderer, alpha * 0.52f);

            UpdateFlare(
                centerFlareRenderer,
                centerPoint,
                settings.cellCenterFlareStartSize,
                settings.cellCenterFlarePeakSize,
                pulse,
                alpha,
                t * 8f
            );

            UpdateFlare(
                outerFlareRenderer,
                centerPoint,
                settings.cellOuterFlareStartSize,
                settings.cellOuterFlarePeakSize,
                pulse,
                alpha * 0.42f,
                -t * 5f
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void BuildBeamMesh(Mesh mesh, Vector3 direction, float length, float width)
    {
        if (mesh == null)
        {
            return;
        }

        Camera cam = Camera.main;

        if (cam == null)
        {
            return;
        }

        direction.Normalize();

        Vector3 start = centerPoint - direction * length * 0.5f;
        Vector3 end = centerPoint + direction * length * 0.5f;

        Vector3 side = Vector3.Cross(cam.transform.forward, direction);

        if (side.sqrMagnitude <= 0.0001f)
        {
            side = cam.transform.up;
        }

        side.Normalize();

        float halfWidth = width * 0.5f;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = start - side * halfWidth;
        vertices[1] = start + side * halfWidth;
        vertices[2] = end + side * halfWidth;
        vertices[3] = end - side * halfWidth;

        Vector2[] uvs = new Vector2[4];
        uvs[0] = new Vector2(0f, 0f);
        uvs[1] = new Vector2(0f, 1f);
        uvs[2] = new Vector2(1f, 1f);
        uvs[3] = new Vector2(1f, 0f);

        int[] triangles = new int[6];
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void ApplyBeamProperties(MeshRenderer targetRenderer, float alpha)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(beamBlock);

        beamBlock.SetColor(ColorId, settings.beamGlowColor);
        beamBlock.SetColor(CoreColorId, settings.beamCoreColor);
        beamBlock.SetFloat(AlphaId, Mathf.Clamp01(alpha));

        targetRenderer.SetPropertyBlock(beamBlock);
    }

    private void UpdateFlare(MeshRenderer targetRenderer, Vector3 worldPosition, float startSize, float peakSize, float pulse, float alpha, float rotation)
    {
        if (targetRenderer == null)
        {
            return;
        }

        Transform target = targetRenderer.transform;
        target.position = worldPosition;
        target.localScale = Vector3.one * Mathf.Lerp(startSize, peakSize, pulse);

        Camera cam = Camera.main;
        if (cam != null)
        {
            target.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
        }

        targetRenderer.GetPropertyBlock(flareBlock);

        flareBlock.SetColor(ColorId, settings.flareColor);
        flareBlock.SetColor(CoreColorId, settings.beamCoreColor);
        flareBlock.SetColor(AccentColorId, settings.beamGlowColor);
        flareBlock.SetFloat(AlphaId, Mathf.Clamp01(alpha));
        flareBlock.SetFloat(RotationId, rotation);

        targetRenderer.SetPropertyBlock(flareBlock);
    }

    private void SetRendererEnabled(Renderer targetRenderer, bool isEnabled)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.enabled = isEnabled;
    }
}