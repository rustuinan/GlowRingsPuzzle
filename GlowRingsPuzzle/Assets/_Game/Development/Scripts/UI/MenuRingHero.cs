using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

[RequireComponent(typeof(RawImage))]
public class MenuRingHero : MonoBehaviour
{
    [SerializeField] private ThemeData theme;
    private GameObject rig;
    private Transform rings;
    private RenderTexture texture;
    private const float CycleSeconds = 7.2f;
    private static readonly int EmissionIntensity = Shader.PropertyToID("_EmissionIntensity");
    private static readonly int RimIntensity = Shader.PropertyToID("_RimIntensity");
    private MaterialPropertyBlock lighting;
    private float startedAt;
    private readonly Transform[] parts = new Transform[3];
    private readonly Transform[] satellites = new Transform[2];
    private readonly Renderer[][] surfaces = new Renderer[3][];
    public void Configure(ThemeData value) { theme = value; }
    private void OnEnable()
    {
        Rebuild();
    }
    private void Rebuild()
    {
        Clear();
        var catalog = CosmeticsCatalog.Load();
        if (catalog != null) theme = CosmeticsSave.MenuTheme;
        if (!Application.isPlaying || theme == null) return;
        lighting ??= new MaterialPropertyBlock();
        var models = theme.RingModelData;
        rig = new GameObject("Menu Ring Preview") { hideFlags = HideFlags.DontSave };
        rig.transform.position = new Vector3(1000, 1000, 1000);
        rings = new GameObject("Gameplay Ring Models").transform;
        rings.SetParent(rig.transform, false);
        RingColorType[] colors = {RingColorType.Purple, RingColorType.Blue, RingColorType.Pink};
        for (int layer = 0; layer < 3; layer++)
        {
            GameObject part = Instantiate(models.GetModel((RingLayer)layer), rings);
            parts[layer] = part.transform;
            foreach (Transform child in part.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            foreach (Collider collider in part.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            surfaces[layer] = part.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in surfaces[layer])
            {
                renderer.sharedMaterial = theme.GetColorData(colors[layer]).Material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
        for (int i = 0; i < 2; i++)
        {
            GameObject accent = Instantiate(models.OuterModelPrefab, rings);
            satellites[i] = accent.transform;
            accent.transform.localScale = Vector3.one * (i == 0 ? 0.19f : 0.14f);
            foreach (Transform child in accent.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            foreach (Collider collider in accent.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Renderer renderer in accent.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = theme.GetColorData(i == 0 ? RingColorType.Yellow : RingColorType.Green).Material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
        Camera camera = new GameObject("Preview Camera", typeof(Camera)).GetComponent<Camera>();
        camera.transform.SetParent(rig.transform, false);
        camera.transform.localPosition = new Vector3(0, 1.8f, -2.15f);
        camera.transform.LookAt(rig.transform.position + Vector3.up * 0.16f);
        camera.orthographic = false; camera.fieldOfView = 43f;
        camera.nearClipPlane = 0.1f; camera.farClipPlane = 10;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
        camera.cullingMask = 1 << 30; camera.allowHDR = false; camera.allowMSAA = true;
        texture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        texture.antiAliasing = 4;
        texture.Create(); camera.targetTexture = texture;
        GetComponent<RawImage>().texture = texture;
        startedAt = Time.unscaledTime;
        ApplyPose(0f);
    }

    private void Update()
    {
        if (rings == null) return;
        ApplyPose(Mathf.Repeat((Time.unscaledTime - startedAt) / CycleSeconds, 1f));
    }

    private void ApplyPose(float phase)
    {
        float angle = phase * 2f * Mathf.PI;
        float drift = Mathf.Sin(angle);
        float turn = 1f - Mathf.Cos(angle);
        rings.localPosition = new Vector3(.045f * drift, .025f * turn, 0f);
        rings.localRotation = Quaternion.Euler(14f - 13f * turn, 16f * drift, -8f + 8f * turn);

        // Travel through the center openings on-axis before moving sideways or tilting.
        float innerLift = Ramp(phase, .06f, .24f) * (1f - Ramp(phase, .71f, .91f));
        float middleLift = Ramp(phase, .13f, .30f) * (1f - Ramp(phase, .67f, .84f));
        float innerFan = Ramp(phase, .24f, .45f) * (1f - Ramp(phase, .53f, .71f));
        float middleFan = Ramp(phase, .30f, .48f) * (1f - Ramp(phase, .50f, .67f));
        parts[1].localPosition = new Vector3(-.31f * middleFan, -.31f * middleLift, -.10f * middleFan);
        parts[1].localRotation = Quaternion.Euler(23f * middleFan, 0f, 30f * middleFan);
        parts[2].localPosition = new Vector3(.32f * innerFan, .38f * innerLift, .09f * innerFan);
        parts[2].localRotation = Quaternion.Euler(-34f * innerFan, 0f, 37f * innerFan);

        for (int i = 0; i < satellites.Length; i++)
        {
            float side = i == 0 ? 1f : -1f;
            satellites[i].localPosition = new Vector3(side * (.76f + .04f * turn),
                .13f + side * .07f * turn, side * (.11f + .10f * drift));
            satellites[i].localRotation = Quaternion.Euler(36f + 15f * turn, side * 24f * drift, side * (20f - 12f * turn));
        }

        for (int i = 0; i < surfaces.Length; i++)
        {
            float delay = (2 - i) * .018f;
            float glow = Ramp(phase, .90f + delay, .928f + delay) * (1f - Ramp(phase, .928f + delay, .96f + delay));
            foreach (Renderer surface in surfaces[i])
            {
                Material material = surface.sharedMaterial;
                if (!material.HasProperty(EmissionIntensity) || !material.HasProperty(RimIntensity)) continue;
                lighting.SetFloat(EmissionIntensity, material.GetFloat(EmissionIntensity) + .20f * glow);
                lighting.SetFloat(RimIntensity, material.GetFloat(RimIntensity) + .55f * glow);
                surface.SetPropertyBlock(lighting);
            }
        }
    }

    private static float Ramp(float phase, float start, float end) =>
        Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, phase));

    private void OnDisable()
    {
        Clear();
    }
    private void Clear()
    {
        if (rig != null) { rig.SetActive(false); Destroy(rig); }
        if (texture != null) { texture.Release(); Destroy(texture); }
        rig = null; texture = null;
    }
}
