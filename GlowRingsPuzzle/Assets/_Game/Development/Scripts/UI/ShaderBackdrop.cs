using UnityEngine;

[ExecuteAlways]
public class ShaderBackdrop : MonoBehaviour
{
    [SerializeField] private Camera backdropCamera;
    [SerializeField] private Transform quad;
    public void Configure(Camera camera, Transform surface) { backdropCamera = camera; quad = surface; Apply(); }
    private void LateUpdate() { Apply(); }
    private void Apply()
    {
        if (backdropCamera == null || quad == null) return;
        quad.localScale = new Vector3(backdropCamera.orthographicSize * 2f * backdropCamera.aspect,
            backdropCamera.orthographicSize * 2f, 1f);
    }
}
