using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(Character))]
[RequireComponent(typeof(MinimapIcon))]
public class Player : MonoBehaviour {
    Character _char;
    Animator _anim;
    Attacker _attacker;
    MinimapIcon _minimapIcon;
    public Character Char => _char;
    void UpdateHealthUI(float health) {
        UIManager.T.UpdateHealth(health, _char.MaxHealth);
    }
    void OnDeath() {
        UIManager.T.ShowEndGameScreen();
    }
    void Update() {
        if (_char.IsStunned || _char.IsDead || GameManager.T.IsStarting)
            return;
        if (Input.GetMouseButtonDown(0))
            _attacker.QueueAttack(0);
        if (Input.GetMouseButtonDown(1))
            _attacker.QueueAttack(1);
        if (_attacker.IsPlaying) {
            _char.Move(Vector3.zero);
            return;
        }
        var x = Input.GetAxisRaw("Horizontal");
        var y = Input.GetAxisRaw("Vertical");
        var camForward = GameManager.T.Cam.transform.forward;
        camForward.y = 0;
        var camRight = GameManager.T.Cam.transform.right;
        camRight.y = 0;
        _char.Move((x * camRight + y * camForward).normalized);
        if (Input.GetKeyDown(KeyCode.Space))
            _char.Jump();
        if (_anim != null) {
            _anim.SetFloat("forwardVelocity", Mathf.Max(Mathf.Abs(y), Mathf.Abs(x)) * _char.Speed);
        }
        if (x == 0 && y == 0)
            return;
        transform.forward = (x * camRight + y * camForward).normalized;
    }
    void Start() {
        UpdateHealthUI(_char.MaxHealth);
        _char.OnHealthChange += UpdateHealthUI;
        _char.OnDeath += OnDeath;
    }
    void Awake()
    {
        _char = GetComponent<Character>();
        _anim = GetComponentInChildren<Animator>();
        _attacker = GetComponentInChildren<Attacker>();
        _minimapIcon = GetComponent<MinimapIcon>();
        if (_anim == null)
            Debug.LogWarning($"Player doesn't ahve animator as achild");
        if (_attacker == null)
            Debug.LogWarning($"Player doesn't ahve attacker as achild or on it");
        if (_minimapIcon == null)
            Debug.LogWarning("Player is missing a MinimapIcon component");
        else
            _minimapIcon.SetIconType(MinimapIconType.Player);
    }
}
