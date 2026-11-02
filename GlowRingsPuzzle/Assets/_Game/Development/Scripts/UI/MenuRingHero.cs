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
    private readonly Transform[] parts = new Transform[3];
    private readonly Transform[] satellites = new Transform[2];
    public void Configure(ThemeData value) { theme = value; }
    private void OnEnable()
    {
        if (!Application.isPlaying || theme == null) return;
        rig = new GameObject("Menu Ring Preview") { hideFlags = HideFlags.DontSave };
        rig.transform.position = new Vector3(1000, 1000, 1000);
        rings = new GameObject("Gameplay Ring Models").transform;
        rings.SetParent(rig.transform, false);
        RingColorType[] colors = {RingColorType.Purple, RingColorType.Blue, RingColorType.Pink};
        for (int layer = 0; layer < 3; layer++)
        {
            GameObject part = Instantiate(theme.RingModelData.GetModel((RingLayer)layer), rings);
            parts[layer] = part.transform;
            foreach (Transform child in part.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            foreach (Collider collider in part.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = theme.GetColorData(colors[layer]).Material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
        for (int i = 0; i < 2; i++)
        {
            GameObject accent = Instantiate(theme.RingModelData.OuterModelPrefab, rings);
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
        camera.orthographic = false; camera.fieldOfView = 36f;
        camera.nearClipPlane = 0.1f; camera.farClipPlane = 10;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
        camera.cullingMask = 1 << 30; camera.allowHDR = false; camera.allowMSAA = false;
        texture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        texture.Create(); camera.targetTexture = texture;
        GetComponent<RawImage>().texture = texture;
    }
    private void Update()
    {
        if (rings == null) return;
        float time = Time.unscaledTime;
        float spread = 0.55f + Mathf.Sin(time * 0.7f) * 0.3f;
        rings.localRotation = Quaternion.Euler(0, Mathf.Sin(time * 0.32f) * 18f, 0);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == null) continue;
            parts[i].localPosition = new Vector3(Mathf.Sin(time * 0.65f + i * 1.8f) * 0.11f,
                i * 0.22f * spread + Mathf.Sin(time * 1.1f + i) * 0.035f, (i - 1) * 0.06f);
            parts[i].localRotation = Quaternion.Euler(Mathf.Sin(time * 0.8f + i) * 16f,
                time * (i % 2 == 0 ? 14f : -19f), Mathf.Cos(time * 0.65f + i) * 19f);
        }
        for (int i = 0; i < satellites.Length; i++)
        {
            float angle = time * 0.45f + i * Mathf.PI;
            satellites[i].localPosition = new Vector3(Mathf.Cos(angle) * 0.65f,
                0.16f + Mathf.Sin(angle * 1.4f) * 0.17f, Mathf.Sin(angle) * 0.32f);
            satellites[i].localRotation = Quaternion.Euler(25f + Mathf.Sin(time) * 22f, time * 38f, i * 35f);
        }
    }
    private void OnDisable()
    {
        if (rig != null) Destroy(rig);
        if (texture != null) { texture.Release(); Destroy(texture); }
        rig = null; texture = null;
    }
}
