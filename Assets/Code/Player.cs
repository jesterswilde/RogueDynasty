using UnityEngine;

[RequireComponent(typeof(Character))]
public class Player : MonoBehaviour {
    Character _char;
    public Character Char => _char;
    void Update() {
        var x = Input.GetAxisRaw("Horizontal");
        var y = Input.GetAxisRaw("Vertical");
        var camForward = GameManager.T.Cam.transform.forward;
        camForward.y = 0;
        var camRight = GameManager.T.Cam.transform.right;
        camRight.y = 0;
        _char.Move((x * camRight + y * camForward).normalized);
        if (Input.GetKeyDown(KeyCode.Space))
            _char.Jump();
        if (x == 0 && y == 0)
            return;
        transform.forward = new Vector3(x, 0, y).normalized;
    }
    void Awake()
    {
        _char = GetComponent<Character>();
    }
}
