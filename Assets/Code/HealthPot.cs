using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class HealthPot : MonoBehaviour {
    [SerializeField]
    float _healAmount;
    Detector _detector;
    void PickedUp() {
        GameManager.T.Player.Char.ModifyHealth(_healAmount);
        Debug.Log("picked up");
        Destroy(gameObject);
    }
    void Start() {
        _detector.OnBlocked += PickedUp;
    }
    void Awake() {
        _detector = GetComponentInChildren<Detector>();
    }
}
