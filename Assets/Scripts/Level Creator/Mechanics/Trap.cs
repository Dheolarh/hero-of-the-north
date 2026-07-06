using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Trap : MonoBehaviour
{
    [SerializeField] private Toggle trapActiveToggle;
    [SerializeField] private Button editButton;
    [SerializeField] private TMP_Text trapNameText;
    [SerializeField] private Button deleteButton;

    public void Setup(string name, bool isActive, Action<bool> onToggleChanged, Action onEditClicked, Action onDeleteClicked)
    {
        if (trapNameText != null)
        {
            trapNameText.text = name;
        }

        if (trapActiveToggle != null)
        {
            trapActiveToggle.onValueChanged.RemoveAllListeners();
            trapActiveToggle.isOn = isActive;
            trapActiveToggle.onValueChanged.AddListener((val) =>
            {
                onToggleChanged?.Invoke(val);
            });
        }

        if (editButton != null)
        {
            editButton.onClick.RemoveAllListeners();
            editButton.onClick.AddListener(() =>
            {
                onEditClicked?.Invoke();
            });
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            if (onDeleteClicked != null)
            {
                deleteButton.onClick.AddListener(() =>
                {
                    onDeleteClicked?.Invoke();
                });
            }
        }
    }
}
