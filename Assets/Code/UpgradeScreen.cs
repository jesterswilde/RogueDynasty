using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeScreen : MonoBehaviour {
    [SerializeField]
    List<UpgradeCard> _cards;
    public void DisplayOptions(List<UpgradeChoice> choices, Action<UpgradeChoice> cb) {
        for(int i = 0; i < choices.Count; i++) {
            _cards[i].Showchoice(choices[i], cb);
        }
    }
}
