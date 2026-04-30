using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    private Camera cachedCamera;

    private void LateUpdate()
    {
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        if (cachedCamera == null)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(cachedCamera.transform.forward, cachedCamera.transform.up);
    }
}