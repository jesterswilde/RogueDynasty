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