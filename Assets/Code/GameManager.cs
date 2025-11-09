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


    
    void Awake()
    {
        if (t != null)
        {
            Destroy(this);
            Debug.LogWarning($"Multiple Game Managers, one on {name}");
            return;
        }
        t = this;
    }
}
