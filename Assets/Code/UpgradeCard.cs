using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradeCard : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler{
    [SerializeField]
    TMPro.TextMeshProUGUI _title;
    [SerializeField]
    TMPro.TextMeshProUGUI _text;
    Action<UpgradeChoice> CB;
    UpgradeChoice _choice;

    public void OnPointerDown(PointerEventData eventData) {
        Debug.Log("Clicked!");
        CB?.Invoke(_choice);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        Debug.Log("Entered");
    }

    public void Showchoice(UpgradeChoice choice, Action<UpgradeChoice> OnPick) {
        _title.text = choice.Title;
        _text.text = choice.Text;
        _choice = choice;
        CB = OnPick;
    }
}