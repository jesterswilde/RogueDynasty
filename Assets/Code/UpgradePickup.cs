using UnityEngine;

public class UpgradePickup : MonoBehaviour {
    [SerializeField]
    AudioClip _clip;
    Detector _detector;
    void PickedUp() {
        GameManager.T.PickUpgrade();
        Destroy(gameObject);
    }
    void Start() {
        _detector.OnBlocked += PickedUp;
    }
    void Awake() {
        _detector = GetComponentInChildren<Detector>();
    }
}