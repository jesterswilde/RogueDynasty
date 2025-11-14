using UnityEngine;

[RequireComponent(typeof(MinimapIcon))]
public class UpgradePickup : MonoBehaviour {
    [SerializeField]
    AudioClip _clip;
    Detector _detector;
    MinimapIcon _icon;
    void PickedUp() {
        GameManager.T.PickUpgrade();
        Destroy(gameObject);
    }
    void Start() {
        _detector.OnBlocked += PickedUp;
    }
    void Awake() {
        _detector = GetComponentInChildren<Detector>();
        _icon = GetComponent<MinimapIcon>();
        if (_icon != null)
            _icon.SetIconType(MinimapIconType.UpgradePickup);
        else
            Debug.LogWarning("UpgradePickup is missing MinimapIcon so it won't appear on the minimap.");
    }
}