using UnityEngine;
using UnityEngine.UI;

public class Teleport : MonoBehaviour
{
    [SerializeField] private Slider teleportXSlider;
    [SerializeField] private Slider teleportYSlider;
    [Tooltip("Offset applied to the X teleport coordinate when saved, and added back when loaded into the editor.")]
    [SerializeField] private float coordinateOffsetX = 0f;
    [Tooltip("Offset applied to the Y teleport coordinate when saved, and added back when loaded into the editor.")]
    [SerializeField] private float coordinateOffsetY = 0f;

    public void Setup(CollisionsAndTriggers trigger)
    {
        if (trigger == null) return;

        GameObject targetObj = (trigger.objectsToTrigger != null && trigger.objectsToTrigger.Length > 0) 
            ? trigger.objectsToTrigger[0] 
            : null;

        Debug.Log($"[Teleport Setup] activeTriggerScript: {trigger.name}, targetObj: {(targetObj != null ? targetObj.name : "null")}");

        if (targetObj != null)
        {
            var borrower = GetComponent<CameraBorrowerSlider>() ?? gameObject.AddComponent<CameraBorrowerSlider>();
            CanvasGroup parentGroup = transform.parent != null ? transform.parent.GetComponentInParent<CanvasGroup>() : null;
            if (parentGroup == null && transform.parent != null)
            {
                Transform parent = transform.parent;
                while (parent != null)
                {
                    if (parent.name == "MechanicsPopupPanel")
                    {
                        parentGroup = parent.gameObject.GetComponent<CanvasGroup>() ?? parent.gameObject.AddComponent<CanvasGroup>();
                        break;
                    }
                    parent = parent.parent;
                }
            }
            
            // Adjust display position: add offset back (accounting for coordinate sign)
            Vector2 displayPos = trigger.teleportPosition;
            float signX = displayPos.x >= 0f ? 1f : -1f;
            float signY = displayPos.y >= 0f ? 1f : -1f;
            displayPos.x += coordinateOffsetX * signX;
            displayPos.y += coordinateOffsetY * signY;

            borrower.Initialize(teleportXSlider, teleportYSlider, targetObj, displayPos, parentGroup);
            borrower.OnPositionSaved = (pos) =>
            {
                // Subtract offset when saving (accounting for coordinate sign)
                Vector2 savedPos = pos;
                float saveSignX = savedPos.x >= 0f ? 1f : -1f;
                float saveSignY = savedPos.y >= 0f ? 1f : -1f;
                savedPos.x -= coordinateOffsetX * saveSignX;
                savedPos.y -= coordinateOffsetY * saveSignY;
                trigger.teleportPosition = savedPos;
            };
        }
    }
}
