using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UpgradeCard : MonoBehaviour, IPointerDownHandler{
    bool _canPick = true;
    [SerializeField]
    TMPro.TextMeshProUGUI _title;
    [SerializeField]
    TMPro.TextMeshProUGUI _text;
    Action<UpgradeChoice> CB;
    UpgradeChoice _choice;

    public void OnPointerDown(PointerEventData eventData) {
        if (!_canPick)
            return;
        CB?.Invoke(_choice);
    }
    IEnumerator CanPick() {
        yield return new WaitForSecondsRealtime(0.5f);
        _canPick = true;
    }

    public void Showchoice(UpgradeChoice choice, Action<UpgradeChoice> OnPick) {
        _title.text = choice.Title;
        _text.text = choice.Text;
        _choice = choice;
        CB = OnPick;
        StartCoroutine(CanPick());
    }
}