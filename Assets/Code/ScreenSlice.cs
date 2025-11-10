using Assets.Scripts;
using System.Collections;
using UnityEngine;

public class ScreenSlicer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    Camera _camera;
    [SerializeField]
    float _minDragDistance = 10f; // in screen pixels, to avoid clicks

    [Header("Slice Physics")]
    [SerializeField]
    float _sliceForce = 3f;

    public Plane SlicePlane { get; private set; }
    public bool HasPlane { get; private set; }

    Vector3 _startScreenPos;
    Vector3 _startDir;
    bool _isDragging;

    void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;
    }

    void Update()
    {
        HandleMouseInput();
    }

    void HandleMouseInput()
    {
        if (_camera == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
            _startScreenPos = Input.mousePosition;

            Ray startRay = _camera.ScreenPointToRay(_startScreenPos);
            _startDir = startRay.direction.normalized;
        }

        if (_isDragging && Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            Vector3 endScreenPos = Input.mousePosition;

            // Ignore tiny drags (just clicks)
            if ((endScreenPos - _startScreenPos).sqrMagnitude <
                _minDragDistance * _minDragDistance)
            {
                return;
            }

            Ray endRay = _camera.ScreenPointToRay(endScreenPos);
            Vector3 endDir = endRay.direction.normalized;

            CreateSlicePlane(_startDir, endDir);
        }
    }

    /// <summary>
    /// Creates a plane that:
    ///  - passes through the camera position
    ///  - contains the two view rays with directions dirA and dirB
    /// Plane is in WORLD space.
    /// </summary>
    void CreateSlicePlane(Vector3 dirA, Vector3 dirB)
    {
        Vector3 normal = Vector3.Cross(dirA, dirB);
        if (normal.sqrMagnitude < 1e-6f)
        {
            HasPlane = false;
            return;
        }

        normal.Normalize();

        Vector3 camPos = _camera.transform.position;
        SlicePlane = new Plane(normal, camPos);
        HasPlane = true;

        SliceObject(SlicePlane);
    }

    /// <summary>
    /// Use the WORLD space plane to:
    ///  - test which objects are intersected
    ///  - convert to LOCAL plane per object before calling Slicer
    ///  - push both halves apart along the slice plane normal
    /// </summary>
    void SliceObject(Plane worldPlane)
    {
        var sliceables = FindObjectsByType<Sliceable>(FindObjectsSortMode.None);

        foreach (var s in sliceables)
        {
            GameObject obj = s.gameObject;

            // Quick bounds-based test in WORLD space
            if (!ColliderIsOnPlane(obj, worldPlane))
                continue;

            // Convert world plane to this object's LOCAL space for Slicer
            Plane localPlane = WorldToLocalPlane(worldPlane, obj.transform);

            // Perform the slice (Slicer expects a plane in the mesh's local space)
            GameObject[] slices = Slicer.Slice(localPlane, obj);
            Destroy(obj);

            if (slices == null || slices.Length < 2)
                continue;

            // Apply forces to push halves apart along world-plane normal
            Vector3 n = worldPlane.normal.normalized;
            Vector3 positiveForce = n * _sliceForce;
            Vector3 negativeForce = -n * _sliceForce;

            if (slices[0].TryGetComponent<Rigidbody>(out var rbPos))
            {
                rbPos.AddForce(positiveForce, ForceMode.Impulse);
            }

            if (slices[1].TryGetComponent<Rigidbody>(out var rbNeg))
            {
                rbNeg.AddForce(negativeForce, ForceMode.Impulse);
            }
        }
    }

    /// <summary>
    /// Convert a WORLD-space plane into the LOCAL space of a given transform.
    /// </summary>
    static Plane WorldToLocalPlane(Plane worldPlane, Transform target)
    {
        // Transform the normal into local space
        Vector3 localNormal = target.InverseTransformDirection(worldPlane.normal).normalized;

        // Any point on the plane in world space:
        // For Unity Plane: normal·p + distance = 0 => p = -normal * distance
        Vector3 worldPointOnPlane = -worldPlane.normal * worldPlane.distance;

        // Transform that point into local space
        Vector3 localPoint = target.InverseTransformPoint(worldPointOnPlane);

        // Rebuild the plane in local space
        Plane localPlane = new Plane(localNormal, localPoint);
        return localPlane;
    }

    /// <summary>
    /// Check if a GameObject's collider intersects a WORLD-space plane
    /// using its world-space bounds.
    /// </summary>
    bool ColliderIsOnPlane(GameObject obj, Plane plane, float tolerance = 0.01f)
    {
        if (obj == null)
            return false;

        if (!obj.TryGetComponent<Collider>(out var col))
            return false;

        Bounds b = col.bounds;
        Vector3 min = b.min;
        Vector3 max = b.max;

        Vector3[] corners = new Vector3[8] {
            new(min.x, min.y, min.z),
            new(max.x, min.y, min.z),
            new(min.x, max.y, min.z),
            new(min.x, min.y, max.z),
            new(max.x, max.y, min.z),
            new(max.x, min.y, max.z),
            new(min.x, max.y, max.z),
            new(max.x, max.y, max.z)
        };

        bool hasPositive = false;
        bool hasNegative = false;

        foreach (var c in corners)
        {
            float dist = plane.GetDistanceToPoint(c);
            if (Mathf.Abs(dist) <= tolerance)
            {
                // Corner is close enough to be "on" the plane
                return true;
            }
            else if (dist > 0)
                hasPositive = true;
            else
                hasNegative = true;

            // If collider spans both sides, it crosses the plane
            if (hasPositive && hasNegative)
                return true;
        }
        return false;
    }
}
