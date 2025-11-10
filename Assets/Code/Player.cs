using UnityEngine;

[RequireComponent(typeof(Character))]
public class Player : MonoBehaviour {
    Character _char;
    public Character Char => _char;
    void Update() {
        var x = Input.GetAxisRaw("Horizontal");
        var y = Input.GetAxisRaw("Vertical");
        _char.Move((x * transform.right + y * transform.forward).normalized);
        if (Input.GetKeyDown(KeyCode.Space))
            _char.Jump();
        if (x == 0 && y == 0)
            return;
        var camForward = GameManager.T.Cam.transform.forward;
        camForward.y = 0;
        transform.forward = camForward.normalized;
    }
    void Awake()
    {
        _char = GetComponent<Character>();
    }
}
