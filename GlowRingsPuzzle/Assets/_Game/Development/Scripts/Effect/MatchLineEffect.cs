using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchLineEffect : MonoBehaviour
{
    [Header("Lines")]
    [SerializeField] private MeshFilter glowLineMeshFilter;
    [SerializeField] private MeshRenderer glowLineRenderer;

    [SerializeField] private MeshFilter coreLineMeshFilter;
    [SerializeField] private MeshRenderer coreLineRenderer;

    [Header("Flares")]
    [SerializeField] private MeshFilter startFlareMeshFilter;
    [SerializeField] private MeshRenderer startFlareRenderer;

    [SerializeField] private MeshFilter middleFlareMeshFilter;
    [SerializeField] private MeshRenderer middleFlareRenderer;

    [SerializeField] private MeshFilter endFlareMeshFilter;
    [SerializeField] private MeshRenderer endFlareRenderer;

    [SerializeField] private MeshFilter sweepFlareMeshFilter;
    [SerializeField] private MeshRenderer sweepFlareRenderer;

    private MatchEffectSettings settings;

    private Vector3[] originalPoints = new Vector3[3];
    private Vector3[] beamPoints = new Vector3[3];

    private Mesh glowLineMesh;
    private Mesh coreLineMesh;
    private Mesh quadMesh;

    private MaterialPropertyBlock glowBlock;
    private MaterialPropertyBlock coreBlock;
    private MaterialPropertyBlock flareBlock;

    private Coroutine playRoutine;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");
    private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int RotationId = Shader.PropertyToID("_Rotation");

    private void Awake()
    {
        glowBlock = new MaterialPropertyBlock();
        coreBlock = new MaterialPropertyBlock();
        flareBlock = new MaterialPropertyBlock();

        CreateMeshes();
        AssignMeshes();
    }

    public void Initialize(MatchEffectSettings effectSettings, Vector3 pointA, Vector3 pointB, Vector3 pointC)
    {
        settings = effectSettings;

        originalPoints[0] = pointA;
        originalPoints[1] = pointB;
        originalPoints[2] = pointC;

        CreateMeshes();
        AssignMeshes();

        SortOriginalPoints();
        CopyOriginalToBeamPoints();
        ExtendBeamPoints();

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayRoutine());
    }

    private void CreateMeshes()
    {
        if (glowLineMesh == null)
        {
            glowLineMesh = new Mesh();
            glowLineMesh.name = "Glow Line Mesh";
        }

        if (coreLineMesh == null)
        {
            coreLineMesh = new Mesh();
            coreLineMesh.name = "Core Line Mesh";
        }

        if (quadMesh == null)
        {
            quadMesh = new Mesh();
            quadMesh.name = "Flare Quad Mesh";

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
        if (glowLineMeshFilter != null)
        {
            glowLineMeshFilter.sharedMesh = glowLineMesh;
        }

        if (coreLineMeshFilter != null)
        {
            coreLineMeshFilter.sharedMesh = coreLineMesh;
        }

        if (startFlareMeshFilter != null)
        {
            startFlareMeshFilter.sharedMesh = quadMesh;
        }

        if (middleFlareMeshFilter != null)
        {
            middleFlareMeshFilter.sharedMesh = quadMesh;
        }

        if (endFlareMeshFilter != null)
        {
            endFlareMeshFilter.sharedMesh = quadMesh;
        }

        if (sweepFlareMeshFilter != null)
        {
            sweepFlareMeshFilter.sharedMesh = quadMesh;
        }
    }

    private void SortOriginalPoints()
    {
        List<Vector3> sortedPoints = new List<Vector3>();
        sortedPoints.Add(originalPoints[0]);
        sortedPoints.Add(originalPoints[1]);
        sortedPoints.Add(originalPoints[2]);

        float maxDistance = -1f;
        Vector3 start = sortedPoints[0];
        Vector3 end = sortedPoints[1];

        for (int i = 0; i < sortedPoints.Count; i++)
        {
            for (int j = i + 1; j < sortedPoints.Count; j++)
            {
                float distance = (sortedPoints[i] - sortedPoints[j]).sqrMagnitude;

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    start = sortedPoints[i];
                    end = sortedPoints[j];
                }
            }
        }

        Vector3 direction = end - start;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        direction.Normalize();

        sortedPoints.Sort((a, b) =>
        {
            float dotA = Vector3.Dot(a, direction);
            float dotB = Vector3.Dot(b, direction);
            return dotA.CompareTo(dotB);
        });

        originalPoints[0] = sortedPoints[0];
        originalPoints[1] = sortedPoints[1];
        originalPoints[2] = sortedPoints[2];
    }

    private void CopyOriginalToBeamPoints()
    {
        beamPoints[0] = originalPoints[0];
        beamPoints[1] = originalPoints[1];
        beamPoints[2] = originalPoints[2];
    }

    private void ExtendBeamPoints()
    {
        if (settings == null)
        {
            return;
        }

        Vector3 direction = beamPoints[2] - beamPoints[0];

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        direction.Normalize();

        beamPoints[0] -= direction * settings.beamExtraLength;
        beamPoints[2] += direction * settings.beamExtraLength;
    }

    private IEnumerator PlayRoutine()
    {
        if (settings == null)
        {
            Destroy(gameObject);
            yield break;
        }

        SetRendererEnabled(glowLineRenderer, true);
        SetRendererEnabled(coreLineRenderer, true);
        SetRendererEnabled(startFlareRenderer, true);
        SetRendererEnabled(middleFlareRenderer, true);
        SetRendererEnabled(endFlareRenderer, true);
        SetRendererEnabled(sweepFlareRenderer, true);

        float duration = settings.GetSafeDuration();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float pulse = Mathf.Sin(t * Mathf.PI);
            float fade = 1f - Mathf.SmoothStep(0.82f, 1f, t);

            float glowAlpha = Mathf.Clamp01((0.55f + pulse * 0.85f) * fade);
            float coreAlpha = Mathf.Clamp01((0.65f + pulse * 0.90f) * fade);

            float glowWidth = Mathf.Lerp(settings.beamGlowStartWidth, settings.beamGlowPeakWidth, pulse);
            glowWidth = Mathf.Lerp(glowWidth, settings.beamGlowEndWidth, Mathf.SmoothStep(0.60f, 1f, t));

            float coreWidth = Mathf.Lerp(settings.beamCoreStartWidth, settings.beamCorePeakWidth, pulse);
            coreWidth = Mathf.Lerp(coreWidth, settings.beamCoreEndWidth, Mathf.SmoothStep(0.60f, 1f, t));

            BuildLineMesh(glowLineMesh, glowWidth);
            BuildLineMesh(coreLineMesh, coreWidth);

            ApplyGlowLineProperties(glowAlpha);
            ApplyCoreLineProperties(coreAlpha);

            UpdateFlare(
                startFlareRenderer,
                originalPoints[0],
                settings.endpointFlareStartSize,
                settings.endpointFlarePeakSize,
                pulse,
                glowAlpha,
                -t * 6f
            );

            UpdateFlare(
                middleFlareRenderer,
                originalPoints[1],
                settings.middleFlareStartSize,
                settings.middleFlarePeakSize,
                pulse,
                coreAlpha,
                t * 8f
            );

            UpdateFlare(
                endFlareRenderer,
                originalPoints[2],
                settings.endpointFlareStartSize,
                settings.endpointFlarePeakSize,
                pulse,
                glowAlpha,
                t * 6f
            );

            Vector3 sweepPosition = Vector3.Lerp(originalPoints[0], originalPoints[2], Mathf.Clamp01(t * 1.06f));

            UpdateFlare(
                sweepFlareRenderer,
                sweepPosition,
                settings.sweepFlareSize * 0.75f,
                settings.sweepFlareSize,
                pulse,
                glowAlpha * 0.75f,
                t * 12f
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void BuildLineMesh(Mesh targetMesh, float width)
    {
        if (targetMesh == null)
        {
            return;
        }

        Camera cam = Camera.main;

        if (cam == null)
        {
            return;
        }

        Vector3 start = beamPoints[0];
        Vector3 end = beamPoints[2];

        Vector3 direction = end - start;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        direction.Normalize();

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

        targetMesh.Clear();
        targetMesh.vertices = vertices;
        targetMesh.uv = uvs;
        targetMesh.triangles = triangles;
        targetMesh.RecalculateBounds();
    }

    private void ApplyGlowLineProperties(float alpha)
    {
        if (glowLineRenderer == null)
        {
            return;
        }

        glowLineRenderer.GetPropertyBlock(glowBlock);
        glowBlock.SetColor(ColorId, settings.beamGlowColor);
        glowBlock.SetColor(CoreColorId, settings.beamCoreColor);
        glowBlock.SetFloat(AlphaId, alpha);
        glowLineRenderer.SetPropertyBlock(glowBlock);
    }

    private void ApplyCoreLineProperties(float alpha)
    {
        if (coreLineRenderer == null)
        {
            return;
        }

        coreLineRenderer.GetPropertyBlock(coreBlock);
        coreBlock.SetColor(ColorId, settings.beamCoreColor);
        coreBlock.SetColor(CoreColorId, Color.white * 1.5f);
        coreBlock.SetFloat(AlphaId, alpha);
        coreLineRenderer.SetPropertyBlock(coreBlock);
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
        flareBlock.SetFloat(AlphaId, alpha);
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