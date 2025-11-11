using System;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    static GameManager t;
    public static GameManager T => t;
    [SerializeField]
    Config _config;
    public Config Config => _config;
    Player _player;
    public Player Player => _player;
    Camera _camera;
    public Camera Cam => _camera;
    int _kill = 0;
    public int Kill { get => _kill; set {
            _kill = value;
            OnKill?.Invoke(_kill);
        } }
    public event Action<int> OnKill;

    void Awake()
    {
        if (t != null)
        {
            Destroy(this);
            Debug.LogWarning($"Multiple Game Managers, one on {name}");
            return;
        }
        t = this;

        _camera = FindFirstObjectByType<Camera>();
        _player = FindFirstObjectByType<Player>();
        _kill = 0;
    }
}

public class UIManager : MonoBehaviour {
    static UIManager t;
    public static UIManager T => t;
    [SerializeField]
    Image _healtHBar;
    [SerializeField]
    TMPro.TextMeshProUGUI _killCount;
    void UpdateKillCount(int count) {
        _killCount.text = count.ToString();
    }
    public void UpdateHealth(int health, int maxHealth) {
        _healtHBar.fillAmount = (float)health / (float)maxHealth;
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