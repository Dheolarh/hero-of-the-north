using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContinousMotion : MonoBehaviour
{
    [SerializeField] private Toggle enableMovementOnLevelStartToggle;
    [SerializeField] private Toggle enableContinuousMotionToggle; 
    [SerializeField] private Toggle leftToggle;
    [SerializeField] private Toggle rightToggle;
    [SerializeField] private Toggle upToggle;
    [SerializeField] private Toggle downToggle;

    [SerializeField] private Toggle horizontalToggle;
    [SerializeField] private Toggle verticalToggle;
    
    [SerializeField] private Slider motionSpeedSlider;
    [SerializeField] private Slider rotationSpeedSlider;

    [Header("Ping Pong Limits UI")]
    [SerializeField] private Button pingPongMinusButton;
    [SerializeField] private Button pingPongPlusButton;
    [SerializeField] private TMP_Text pingPongValueText;

    public void Setup(CollisionsAndTriggers trigger)
    {
        if (trigger == null) return;

        // 1. Movement on start toggle
        if (enableMovementOnLevelStartToggle != null)
        {
            enableMovementOnLevelStartToggle.onValueChanged.RemoveAllListeners();
            enableMovementOnLevelStartToggle.isOn = trigger.activateOnStart;
            enableMovementOnLevelStartToggle.onValueChanged.AddListener((val) =>
            {
                trigger.activateOnStart = val;
                trigger.enableMove = val;
            });
        }

        // Helper to update interactable states based on whether Ping Pong is enabled
        System.Action<bool> updateInteractableStates = (pingPongActive) =>
        {
            if (leftToggle != null) leftToggle.interactable = !pingPongActive;
            if (rightToggle != null) rightToggle.interactable = !pingPongActive;
            if (upToggle != null) upToggle.interactable = !pingPongActive;
            if (downToggle != null) downToggle.interactable = !pingPongActive;
            
            if (horizontalToggle != null) horizontalToggle.interactable = pingPongActive;
            if (verticalToggle != null) verticalToggle.interactable = pingPongActive;
        };

        // 2. Enable Continuous Ping Pong Motion toggle
        if (enableContinuousMotionToggle != null)
        {
            enableContinuousMotionToggle.onValueChanged.RemoveAllListeners();
            enableContinuousMotionToggle.isOn = trigger.isPingPong;
            enableContinuousMotionToggle.onValueChanged.AddListener((val) =>
            {
                trigger.isPingPong = val;
                updateInteractableStates(val);

                // Auto-set standard move direction if we toggled modes
                if (val)
                {
                    // Ping-pong mode defaults to Horizontal
                    trigger.moveDirection = MoveDirection.Right;
                    if (horizontalToggle != null) horizontalToggle.SetIsOnWithoutNotify(true);
                    if (verticalToggle != null) verticalToggle.SetIsOnWithoutNotify(false);
                }
                else
                {
                    // Normal mode defaults to Right
                    trigger.moveDirection = MoveDirection.Right;
                    if (rightToggle != null) rightToggle.SetIsOnWithoutNotify(true);
                    if (leftToggle != null) leftToggle.SetIsOnWithoutNotify(false);
                    if (upToggle != null) upToggle.SetIsOnWithoutNotify(false);
                    if (downToggle != null) downToggle.SetIsOnWithoutNotify(false);
                }
            });
        }

        updateInteractableStates(trigger.isPingPong);

        // 3. Direction Toggles (Radio group behavior)
        System.Action<MoveDirection, Toggle> onDirectionToggle = (dir, toggledOn) =>
        {
            trigger.moveDirection = dir;
            
            if (leftToggle != null && leftToggle != toggledOn) leftToggle.SetIsOnWithoutNotify(false);
            if (rightToggle != null && rightToggle != toggledOn) rightToggle.SetIsOnWithoutNotify(false);
            if (upToggle != null && upToggle != toggledOn) upToggle.SetIsOnWithoutNotify(false);
            if (downToggle != null && downToggle != toggledOn) downToggle.SetIsOnWithoutNotify(false);
            if (horizontalToggle != null && horizontalToggle != toggledOn) horizontalToggle.SetIsOnWithoutNotify(false);
            if (verticalToggle != null && verticalToggle != toggledOn) verticalToggle.SetIsOnWithoutNotify(false);
        };

        if (leftToggle != null)
        {
            leftToggle.onValueChanged.RemoveAllListeners();
            leftToggle.isOn = (!trigger.isPingPong && trigger.moveDirection == MoveDirection.Left);
            leftToggle.onValueChanged.AddListener((val) => { if (val) onDirectionToggle(MoveDirection.Left, leftToggle); });
        }
        if (rightToggle != null)
        {
            rightToggle.onValueChanged.RemoveAllListeners();
            rightToggle.isOn = (!trigger.isPingPong && trigger.moveDirection == MoveDirection.Right);
            rightToggle.onValueChanged.AddListener((val) => { if (val) onDirectionToggle(MoveDirection.Right, rightToggle); });
        }
        if (upToggle != null)
        {
            upToggle.onValueChanged.RemoveAllListeners();
            upToggle.isOn = (!trigger.isPingPong && trigger.moveDirection == MoveDirection.Up);
            upToggle.onValueChanged.AddListener((val) => { if (val) onDirectionToggle(MoveDirection.Up, upToggle); });
        }
        if (downToggle != null)
        {
            downToggle.onValueChanged.RemoveAllListeners();
            downToggle.isOn = (!trigger.isPingPong && trigger.moveDirection == MoveDirection.Down);
            downToggle.onValueChanged.AddListener((val) => { if (val) onDirectionToggle(MoveDirection.Down, downToggle); });
        }

        if (horizontalToggle != null)
        {
            horizontalToggle.onValueChanged.RemoveAllListeners();
            horizontalToggle.isOn = (trigger.isPingPong && (trigger.moveDirection == MoveDirection.Right || trigger.moveDirection == MoveDirection.Left));
            horizontalToggle.onValueChanged.AddListener((val) => { if (val) onDirectionToggle(MoveDirection.Right, horizontalToggle); });
        }
        if (verticalToggle != null)
        {
            verticalToggle.onValueChanged.RemoveAllListeners();
            verticalToggle.isOn = (trigger.isPingPong && (trigger.moveDirection == MoveDirection.Up || trigger.moveDirection == MoveDirection.Down));
            verticalToggle.onValueChanged.AddListener((val) => { if (val) onDirectionToggle(MoveDirection.Up, verticalToggle); });
        }

        // 4. Speed Slider
        if (motionSpeedSlider != null)
        {
            motionSpeedSlider.minValue = 0f;
            motionSpeedSlider.maxValue = 20f;
            motionSpeedSlider.onValueChanged.RemoveAllListeners();
            motionSpeedSlider.value = trigger.moveSpeed;
            motionSpeedSlider.onValueChanged.AddListener((val) =>
            {
                trigger.moveSpeed = val;
            });
        }

        // 5. Rotation Speed Slider
        if (rotationSpeedSlider != null)
        {
            rotationSpeedSlider.minValue = 0f;
            rotationSpeedSlider.maxValue = 360f;
            rotationSpeedSlider.onValueChanged.RemoveAllListeners();
            rotationSpeedSlider.value = trigger.rotationSpeed;
            rotationSpeedSlider.onValueChanged.AddListener((val) =>
            {
                trigger.rotationSpeed = val;
            });
        }

        // 6. Ping Pong Distance controls (min 2, max 50)
        System.Action updatePingPongDistanceUI = () =>
        {
            if (trigger.pingPongDistance < 2f) trigger.pingPongDistance = 2f;
            if (trigger.pingPongDistance > 50f) trigger.pingPongDistance = 50f;
            if (pingPongValueText != null)
            {
                pingPongValueText.text = trigger.pingPongDistance.ToString("F1") + "m";
            }
        };

        updatePingPongDistanceUI();

        if (pingPongMinusButton != null)
        {
            pingPongMinusButton.onClick.RemoveAllListeners();
            pingPongMinusButton.onClick.AddListener(() =>
            {
                trigger.pingPongDistance -= 1f;
                updatePingPongDistanceUI();
            });
        }

        if (pingPongPlusButton != null)
        {
            pingPongPlusButton.onClick.RemoveAllListeners();
            pingPongPlusButton.onClick.AddListener(() =>
            {
                trigger.pingPongDistance += 1f;
                updatePingPongDistanceUI();
            });
        }
    }
}
