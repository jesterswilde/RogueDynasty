using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MinimapCameraFollow : MonoBehaviour {
    [SerializeField]
    MinimapConfig _config;
    [SerializeField]
    Transform _target;

    Camera _camera;

    void Awake() {
        _camera = GetComponent<Camera>();
        ApplyCameraSettings();
    }

    void Start() {
        if (_target == null)
            TryAssignPlayer();
        ApplyCameraSettings();
    }

    void LateUpdate() {
        if (_config == null)
            return;

        if (_target == null)
            TryAssignPlayer();

        if (_target == null)
            return;

        float lerp = Mathf.Clamp01(_config.FollowLerp);
        Vector3 desiredPos = _target.position + Vector3.up * _config.CameraHeight + _config.CameraOffset;
        transform.position = lerp <= 0f
            ? desiredPos
            : Vector3.Lerp(transform.position, desiredPos, lerp);

        Quaternion desiredRot = _config.LockToWorldNorth
            ? Quaternion.Euler(90f, 0f, 0f)
            : Quaternion.Euler(90f, _target.eulerAngles.y, 0f);

        transform.rotation = lerp <= 0f
            ? desiredRot
            : Quaternion.Slerp(transform.rotation, desiredRot, lerp);
    }

    void ApplyCameraSettings() {
        if (_camera == null || _config == null)
            return;
        _camera.orthographic = true;
        _camera.orthographicSize = _config.OrthographicSize;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = _config.BackgroundColor;
        _camera.cullingMask = _config.GroundLayerMask;
        if (_config.RenderTexture != null)
            _camera.targetTexture = _config.RenderTexture;
    }

    void TryAssignPlayer() {
        if (GameManager.T != null && GameManager.T.Player != null)
            _target = GameManager.T.Player.transform;
    }

    void OnValidate() {
        _camera = GetComponent<Camera>();
        ApplyCameraSettings();
    }

    public void SetTarget(Transform target) => _target = target;
    public void SetConfig(MinimapConfig config) {
        _config = config;
        ApplyCameraSettings();
    }
}

