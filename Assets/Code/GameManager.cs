using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    static GameManager t;
    public static GameManager T => t;
    [SerializeField]
    Config _config;
    [SerializeField]
    Material _innards;
    public Material Innards => _innards;
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
    public void PlayAudio(AudioClip _clip) {
        if (_clip == null)
            return;
        var go = new GameObject();
        var a = go.AddComponent<AudioSource>();
        a.clip = _clip;
        a.Play();
        go.AddComponent<SelfDeleteAudio>();
    }

    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
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
