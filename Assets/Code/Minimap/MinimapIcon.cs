using UnityEngine;

public enum MinimapIconType {
    Player,
    Enemy,
    HealthPot,
    UpgradePickup,
    Custom
}

[DisallowMultipleComponent]
public class MinimapIcon : MonoBehaviour {
    [SerializeField]
    MinimapIconType _iconType = MinimapIconType.Custom;
    [SerializeField]
    bool _overrideColor;
    [SerializeField]
    Color _colorOverride = Color.white;
    [SerializeField]
    bool _overrideSize;
    [SerializeField, Min(1f)]
    float _sizeOverride = 8f;
    [SerializeField]
    Transform _trackedTransform;
    [SerializeField]
    float _heightOffset;

    public MinimapIconType IconType => _iconType;
    public bool HasColorOverride => _overrideColor;
    public Color ColorOverride => _colorOverride;
    public bool HasSizeOverride => _overrideSize;
    public float SizeOverride => _sizeOverride;
    public Transform TrackedTransform => _trackedTransform != null ? _trackedTransform : transform;
    public float HeightOffset => _heightOffset;
    public Vector3 WorldPosition {
        get {
            var pos = TrackedTransform.position;
            pos.y += _heightOffset;
            return pos;
        }
    }

    void OnEnable() {
        MinimapOverlayRenderer.Register(this);
    }

    void OnDisable() {
        MinimapOverlayRenderer.Unregister(this);
    }

    public void SetIconType(MinimapIconType type) => _iconType = type;
}

