using UnityEngine;
using UnityEngine.EventSystems;

public class DisableOnClick : MonoBehaviour, IPointerClickHandler
{
    public GameObject disableObject;
    public void OnPointerClick(PointerEventData eventData)
    {
        DisableSelf();
    }

    // For 2D/3D objects with Colliders (requires PhysicsRaycaster on Camera or default input)
    private void OnMouseDown()
    {
        DisableSelf();
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }

    public void HideObject()
    {
        disableObject.SetActive(false);
    }
}
