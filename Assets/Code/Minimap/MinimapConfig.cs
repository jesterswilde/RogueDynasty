using System;
using UnityEngine;

[CreateAssetMenu(menuName = "RogueDynasty/Minimap Config", fileName = "MinimapConfig")]
public class MinimapConfig : ScriptableObject {
    [Header("Camera")]
    [SerializeField]
    RenderTexture _renderTexture;
    [SerializeField]
    LayerMask _groundLayerMask;
    [SerializeField]
    float _cameraHeight = 60f;
    [SerializeField]
    float _orthographicSize = 30f;
    [SerializeField]
    Vector3 _cameraOffset = Vector3.zero;
    [SerializeField, Range(0f, 1f)]
    float _followLerp = 0.2f;
    [SerializeField]
    bool _lockToWorldNorth = true;
    [SerializeField]
    Color _backgroundColor = new(0f, 0f, 0f, 0f);

    [Header("Icon Styling")]
    [SerializeField]
    float _iconEdgePadding = 4f;
    [SerializeField]
    MinimapIconStyle _playerStyle = new() { Color = Color.blue, Diameter = 14f };
    [SerializeField]
    MinimapIconStyle _enemyStyle = new() { Color = Color.red, Diameter = 10f };
    [SerializeField]
    MinimapIconStyle _healthPotStyle = new() { Color = Color.green, Diameter = 8f };
    [SerializeField]
    MinimapIconStyle _upgradeStyle = new() { Color = Color.green, Diameter = 8f };
    [Header("Player Direction Cone")]
    [SerializeField]
    bool _showPlayerDirectionCone = true;
    [SerializeField]
    Color _playerDirectionColor = new(0f, 0.6f, 1f, 0.3f);
    [SerializeField, Range(5f, 180f)]
    float _playerDirectionAngle = 70f;
    [SerializeField]
    float _playerDirectionLength = 80f;

    public RenderTexture RenderTexture => _renderTexture;
    public LayerMask GroundLayerMask => _groundLayerMask;
    public float CameraHeight => _cameraHeight;
    public float OrthographicSize => _orthographicSize;
    public Vector3 CameraOffset => _cameraOffset;
    public float FollowLerp => _followLerp;
    public bool LockToWorldNorth => _lockToWorldNorth;
    public Color BackgroundColor => _backgroundColor;
    public float IconEdgePadding => _iconEdgePadding;
    public MinimapIconStyle PlayerStyle => _playerStyle;
    public MinimapIconStyle EnemyStyle => _enemyStyle;
    public MinimapIconStyle HealthPotStyle => _healthPotStyle;
    public MinimapIconStyle UpgradeStyle => _upgradeStyle;
    public bool ShowPlayerDirectionCone => _showPlayerDirectionCone;
    public Color PlayerDirectionColor => _playerDirectionColor;
    public float PlayerDirectionAngle => _playerDirectionAngle;
    public float PlayerDirectionLength => _playerDirectionLength;

    public MinimapIconStyle GetStyle(MinimapIconType type) => type switch {
        MinimapIconType.Player => _playerStyle,
        MinimapIconType.Enemy => _enemyStyle,
        MinimapIconType.HealthPot => _healthPotStyle,
        MinimapIconType.UpgradePickup => _upgradeStyle,
        _ => _playerStyle
    };
}

[Serializable]
public struct MinimapIconStyle {
    public Color Color;
    [Min(1f)]
    public float Diameter;
}

