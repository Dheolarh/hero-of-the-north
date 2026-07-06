using UnityEngine;
using UnityEngine.UI;

public class Teleport : MonoBehaviour
{
    [SerializeField] private Slider teleportXSlider;
    [SerializeField] private Slider teleportYSlider;

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
            
            borrower.Initialize(teleportXSlider, teleportYSlider, targetObj, trigger.teleportPosition, parentGroup);
            borrower.OnPositionSaved = (pos) =>
            {
                trigger.teleportPosition = pos;
            };
        }
    }
}
