using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class HealthPot : MonoBehaviour {
    [SerializeField]
    float _healAmount;
    [SerializeField]
    AudioClip _clip;
    Detector _detector;
    void PickedUp() {
        GameManager.T.Player.Char.ModifyHealth(_healAmount);
        GameManager.T.PlayAudio(_clip);
        Destroy(gameObject);
    }
    void Start() {
        _detector.OnBlocked += PickedUp;
    }
    void Awake() {
        _detector = GetComponentInChildren<Detector>();
    }
}
