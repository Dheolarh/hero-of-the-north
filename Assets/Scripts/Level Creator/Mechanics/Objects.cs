using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Objects : MonoBehaviour
{
    [SerializeField] private Toggle objectToggle;
    [SerializeField] private TMP_Text objectNameText;

    public Toggle Toggle => objectToggle;

    public void Setup(string name, bool isActive, Action<bool> onToggleChanged)
    {
        if (objectNameText != null)
        {
            objectNameText.text = name;
        }

        if (objectToggle != null)
        {
            objectToggle.onValueChanged.RemoveAllListeners();
            objectToggle.isOn = isActive;
            objectToggle.onValueChanged.AddListener((val) =>
            {
                onToggleChanged?.Invoke(val);
            });
        }
    }
}
