using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

[RequireComponent(typeof(MinimapIcon))]
public class HealthPot : MonoBehaviour {
    [SerializeField]
    float _healAmount;
    [SerializeField]
    AudioClip _clip;
    Detector _detector;
    MinimapIcon _icon;
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
        _icon = GetComponent<MinimapIcon>();
        if (_icon != null)
            _icon.SetIconType(MinimapIconType.HealthPot);
        else
            Debug.LogWarning("HealthPot is missing MinimapIcon so it won't appear on the minimap.");
    }
}
