using UnityEngine;

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
    }
}
