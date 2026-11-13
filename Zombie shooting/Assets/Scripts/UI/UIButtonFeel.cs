using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Hover feedback for runtime buttons: slight scale-up and a tick sound.</summary>
public class UIButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
{
    private Vector3 target = Vector3.one;
    public void OnPointerEnter(PointerEventData e) { target = Vector3.one * 1.04f; UIKit.PlayHover(); }
    public void OnPointerExit(PointerEventData e) { target = Vector3.one; }
    public void OnSelect(BaseEventData e) { }
    private void OnDisable() { target = Vector3.one; transform.localScale = Vector3.one; }
    private void Update() { transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * 14f); }
}
