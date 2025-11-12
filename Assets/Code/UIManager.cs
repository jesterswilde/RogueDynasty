using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    static UIManager t;
    public static UIManager T => t;
    [SerializeField]
    Image _healthBar;
    [SerializeField]
    TMPro.TextMeshProUGUI _healthText;
    [SerializeField]
    TMPro.TextMeshProUGUI _killCount;
    [SerializeField]
    TMPro.TextMeshProUGUI _nextUpgradeAt;
    [SerializeField]
    UpgradeScreen _upgradeScreenPrefab;
    [SerializeField]
    GameObject _gameStartPrefab;
    [SerializeField]
    GameObject _gameEndPrefab;
    GameObject _screen;

    public void ShowStartGameScreen() {
        _screen = Instantiate(_gameStartPrefab, transform);
    }
    public void ShowEndGameScreen() {
        _screen = Instantiate(_gameEndPrefab, transform);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void UpdateNextUpgrade(string next) {
        _nextUpgradeAt.text = next;
    }
    public void ShowUpgrades(List<UpgradeChoice> choices, Action<UpgradeChoice> cb) {
        var screen = Instantiate(_upgradeScreenPrefab, transform);
        screen.DisplayOptions(choices, cb);
        _screen = screen.gameObject;
    }
    public void CloseScreen() {
        Destroy(_screen);
    }
    void UpdateKillCount(int count) {
        _killCount.text = count.ToString();
    }
    public void UpdateHealth(float health, float maxHealth) {
        _healthBar.fillAmount = (float)health / (float)maxHealth;
        _healthText.text = health.ToString();
    }


    void Start() {
        GameManager.T.OnKill += UpdateKillCount;
    }
    void PlayNext() {

    }
    void Awake() {
        if (t != null)
        {
            Destroy(this);
            Debug.LogWarning($"Multiple Game Managers, one on {name}");
            return;
        }
        t = this;
    }
}