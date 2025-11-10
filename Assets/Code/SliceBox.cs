using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;

[RequireComponent(typeof(BoxCollider))]
public class SliceBox : MonoBehaviour
{
    [System.Serializable]
    public struct SliceResult
    {
        public GameObject positiveSlice;
        public GameObject negativeSlice;
    }

    [Header("Slice Box")]
    [SerializeField]
    BoxCollider _boxCollider;

    /// <summary>
    /// Plane normal will be this object's up, and it passes through this object's position (WORLD space).
    /// All Sliceable objects whose colliders overlap the box will be sliced.
    /// Returns an array of SliceResult, one for each sliced object.
    /// </summary>
    public SliceResult[] Slice()
    {
        if (_boxCollider == null)
            _boxCollider = GetComponent<BoxCollider>();

        var results = new List<SliceResult>();

        // Define the slice plane in WORLD space
        Vector3 planeNormal = transform.up;
        Vector3 planePoint = transform.position;
        Plane worldPlane = new Plane(planeNormal, planePoint);

        // Compute the OverlapBox parameters from the BoxCollider
        Vector3 center = transform.TransformPoint(_boxCollider.center);
        Vector3 halfExtents = Vector3.Scale(_boxCollider.size * 0.5f, transform.lossyScale);
        Quaternion orientation = transform.rotation;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, orientation);

        var seen = new HashSet<Sliceable>();

        foreach (var hit in hits)
        {
            if (hit == null)
                continue;

            if (!hit.TryGetComponent<Sliceable>(out var sliceable))
                continue;

            if (!seen.Add(sliceable))
                continue; // already processed this sliceable

            GameObject obj = sliceable.gameObject;

            Plane localPlane = WorldToLocalPlane(worldPlane, obj.transform);
            GameObject[] slices = Slicer.Slice(localPlane, obj);
            Destroy(obj);

            if (slices == null || slices.Length < 2)
                continue;

            var r = new SliceResult
            {
                positiveSlice = slices[0],
                negativeSlice = slices[1]
            };
            results.Add(r);
        }

        return results.ToArray();
    }

    /// <summary>
    /// Convert a WORLD-space plane into the LOCAL space of a given transform.
    /// </summary>
    static Plane WorldToLocalPlane(Plane worldPlane, Transform target) {
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
}
