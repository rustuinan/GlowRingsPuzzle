using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private bool pressed;
    private void Update() { transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * (pressed ? 0.96f : 1f), Time.unscaledDeltaTime * 22f); }
    public void OnPointerDown(PointerEventData data) { pressed = true; }
    public void OnPointerUp(PointerEventData data) { pressed = false; }
    public void OnPointerExit(PointerEventData data) { pressed = false; }
    private void OnDisable() { pressed = false; transform.localScale = Vector3.one; }
}
