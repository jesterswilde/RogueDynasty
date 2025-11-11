using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class DepenetrateOnSpawn : MonoBehaviour
{
    [Header("Search settings")]
    [Tooltip("Layers to depenetrate against (usually everything except your own debris layer).")]
    [SerializeField]
    private LayerMask _depenetrationMask = ~0; // default: everything

    [Tooltip("Maximum depenetration step per physics frame (world units).")]
    [SerializeField]
    private float _maxStepPerFrame = 0.5f;

    [Tooltip("Maximum number of depenetration iterations.")]
    [SerializeField]
    private int _maxIterations = 20;

    [Tooltip("How many consecutive frames with no overlap before we consider it 'live'.")]
    [SerializeField]
    private int _stableFramesRequired = 2;

    [Header("Debug")]
    [SerializeField]
    private bool _logDebug = false;

    private Rigidbody _rb;
    private Collider[] _myColliders;
    private bool _hasFinished = false;

    public bool HasFinished => _hasFinished;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _myColliders = GetComponentsInChildren<Collider>();
        if (_myColliders == null || _myColliders.Length == 0)
        {
            if (_logDebug)
                Debug.LogWarning($"{name}: DepenetrateOnSpawn has no colliders to work with.");
        }
    }

    void OnEnable()
    {
        // Start depenetration as soon as we're enabled.
        StartCoroutine(DepenetrateRoutine());
    }

    private IEnumerator DepenetrateRoutine()
    {
        if (_myColliders == null || _myColliders.Length == 0)
        {
            _hasFinished = true;
            yield break;
        }

        bool hadRigidbody = _rb != null;
        bool originalKinematic = false;

        if (hadRigidbody)
        {
            originalKinematic = _rb.isKinematic;
            _rb.isKinematic = true; // we move it manually for the depenetration phase
        }

        // Wait one physics step so all transforms/colliders are registered in the physics engine
        yield return new WaitForFixedUpdate();

        int stableFrames = 0;
        int iterations = 0;

        while (iterations < _maxIterations && stableFrames < _stableFramesRequired)
        {
            iterations++;

            Vector3 totalOffset = Vector3.zero;
            int hitCount = 0;

            foreach (var myCol in _myColliders)
            {
                if (myCol == null || !myCol.enabled)
                    continue;

                // Use an overlap check approx around this collider
                Bounds b = myCol.bounds;

                Collider[] overlaps = Physics.OverlapBox(
                    b.center,
                    b.extents,
                    Quaternion.identity,      // approximate; good enough for search
                    _depenetrationMask,
                    QueryTriggerInteraction.Ignore
                );

                foreach (var other in overlaps)
                {
                    if (other == null)
                        continue;
                    if (other.transform == transform)
                        continue; // skip self
                    if (System.Array.IndexOf(_myColliders, other) >= 0)
                        continue; // skip our own child colliders

                    // Compute exact penetration
                    if (Physics.ComputePenetration(
                        myCol, myCol.transform.position, myCol.transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 dir,
                        out float dist
                    ))
                    {
                        if (dist > 0.0001f)
                        {
                            // dir is the direction we need to move myCol to get out of 'other'
                            Vector3 offset = dir * (dist + 0.1f); // small extra margin
                            totalOffset += offset;
                            hitCount++;
                        }
                    }
                }
            }

            if (hitCount == 0 || totalOffset == Vector3.zero)
            {
                // No overlaps this frame
                stableFrames++;
                if (_logDebug)
                    Debug.Log($"{name}: DepenetrateOnSpawn stable frame {stableFrames}/{_stableFramesRequired}");
            }
            else
            {
                // Average the correction over all overlaps
                totalOffset /= hitCount;

                // Clamp step size to avoid huge jumps
                if (totalOffset.magnitude > _maxStepPerFrame)
                    totalOffset = totalOffset.normalized * _maxStepPerFrame;

                if (hadRigidbody)
                    _rb.position += totalOffset;
                else
                    transform.position += totalOffset;

                stableFrames = 0; // still overlapping, reset stable counter

                if (_logDebug)
                    Debug.Log($"{name}: DepenetrateOnSpawn applying offset {totalOffset} (hits: {hitCount})");
            }

            yield return new WaitForFixedUpdate();
        }

        if (hadRigidbody)
        {
            _rb.isKinematic = originalKinematic; // restore original state
        }

        _hasFinished = true;

        if (_logDebug)
            Debug.Log($"{name}: DepenetrateOnSpawn finished after {iterations} iterations, stableFrames={stableFrames}");
    }
}
