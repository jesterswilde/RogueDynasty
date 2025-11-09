using UnityEngine;

public class CameraController : MonoBehaviour {
    [Header("Target")]
    [SerializeField]
    Transform _target;              // What we follow & look at

    [Header("Distance / Zoom")]
    [SerializeField]
    float _distance = 5f;
    [SerializeField]
    float _minDistance = 2f;
    [SerializeField]
    float _maxDistance = 10f;
    [SerializeField]
    public float _zoomSpeed = 5f;

    [Header("Orbit")]
    [SerializeField]
    float _orbitSpeed = 180f;       // degrees per second
    [SerializeField]
    float _minPitch = -20f;         // look-down limit (degrees)
    [SerializeField]
    float _maxPitch = 70f;          // look-up limit (degrees)
    [SerializeField]
    public bool _invertY = false;

    private float yaw;
    private float pitch;
    private Transform cam;

    void Awake()
    {
        Camera c = GetComponentInChildren<Camera>();
        if (c != null)
            cam = c.transform;
        else
            Debug.LogWarning("CameraController: No Camera found in children.");
    }

    void Start()
    {
        if (_target != null)
            transform.position = _target.position;

        if (cam != null)
        {
            // Get the initial offset between the camera and controller
            Vector3 localPos = cam.localPosition;
            _distance = localPos.magnitude;

            // Initialize yaw and pitch from the controller’s current rotation
            Vector3 angles = transform.rotation.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;
        }
    }

    void LateUpdate()
    {
        if (_target == null || cam == null) return;

        transform.position = _target.position;

        HandleOrbit();
        HandleZoom();

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        cam.localPosition = new Vector3(0f, 0f, -_distance);
        cam.LookAt(_target.position);
    }

    void HandleOrbit()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * _orbitSpeed * Time.deltaTime;

        float ySign = _invertY ? 1f : -1f;
        pitch += mouseY * _orbitSpeed * Time.deltaTime * ySign;

        pitch = Mathf.Clamp(pitch, _minPitch, _maxPitch);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        _distance -= scroll * _zoomSpeed;
        _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
    }
}
