using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Bro : MonoBehaviour {
    [SerializeField]
    Config _config;
    [SerializeField]
    float _distFudge = 0.5f;
    [SerializeField]
    float _speed = 5f;

    [Header("Navigation")]
    [SerializeField]
    float _repathInterval = 1f;
    [SerializeField]
    float _repathThreshold = 0.5f;

    Transform _target;
    HashSet<Bro> _bros;

    NavMeshAgent _agent;
    Vector3 _lastGoal;
    bool _hasGoal;

    void Awake() {
        _target = FindFirstObjectByType<Target>().transform;
        _bros = FindObjectsByType<Bro>(FindObjectsSortMode.None)
            .Where(b => b != this)
            .ToHashSet();

        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _speed;
        _agent.angularSpeed = 360f;
        _agent.acceleration = 100f;
    }

    void OnEnable() {
        StartCoroutine(RepathRoutine());
    }

    /// <summary>
    /// Periodically recomputes a desired goal position and updates the NavMeshAgent.
    /// </summary>
    System.Collections.IEnumerator RepathRoutine() {
        var wait = new WaitForSeconds(_repathInterval);

        while (true) {
            if (_config != null && _target != null && _agent != null && _agent.isOnNavMesh) {
                var goal = CalculateTargetPoint();

                // Only update if the goal has moved enough
                if (!_hasGoal || (goal - _lastGoal).sqrMagnitude > _repathThreshold * _repathThreshold) {
                    _agent.SetDestination(goal);
                    _lastGoal = goal;
                    _hasGoal = true;
                }
            }

            yield return wait;
        }
    }

    /// <summary>
    /// Computes a goal position based on group center, repulsion, and target distance.
    /// </summary>
    Vector3 CalculateTargetPoint() {
        // Fallback if something's missing
        if (_config == null || _target == null)
            return transform.position;

        // If there are no other bros, just handle distance-to-target logic
        if (_bros == null || _bros.Count == 0) {
            return CalculateSoloTarget();
        }

        Vector3 center = Vector3.zero;
        Vector3 repulseDir = Vector3.zero;

        float repulseThreshSqr = _config.RepulseAtDistance * _config.RepulseAtDistance;

        foreach (var bro in _bros) {
            center += bro.transform.position;

            var toBro = transform.position - bro.transform.position; // bro -> me
            float sqr = toBro.sqrMagnitude;

            if (sqr < repulseThreshSqr && sqr > 0.0001f) {
                float dist = Mathf.Sqrt(sqr);
                float t = 1f - (dist / _config.RepulseAtDistance);
                repulseDir += toBro.normalized * t;
            }
        }

        center /= _bros.Count;

        // Distance control relative to target
        Vector3 toTarget = _target.position - transform.position;
        float distToTarget = toTarget.magnitude;
        Vector3 toTargetForce = Vector3.zero;

        if (distToTarget > _config.DesiredDistance + _distFudge) {
            toTargetForce = toTarget.normalized * (distToTarget - _config.DesiredDistance);
        }
        else if (distToTarget < _config.DesiredDistance - _distFudge) {
            toTargetForce = toTarget.normalized * (_config.DesiredDistance - distToTarget) * -1;
        }

        // Combine forces
        Vector3 groupDir = center - transform.position;
        Vector3 offset =
            groupDir * _config.GroupForce +
            repulseDir * _config.RepulseForce +
            toTargetForce * _config.TargetDrawForce;

        return transform.position + offset;
    }

    Vector3 CalculateSoloTarget() {
        Vector3 toTarget = _target.position - transform.position;
        float distToTarget = toTarget.magnitude;
        Vector3 toTargetForce = Vector3.zero;

        if (distToTarget > _config.DesiredDistance + _distFudge) {
            toTargetForce = toTarget.normalized * (distToTarget - _config.DesiredDistance);
        }
        else if (distToTarget < _config.DesiredDistance - _distFudge) {
            toTargetForce = toTarget.normalized * (_config.DesiredDistance - distToTarget);
        }

        return transform.position + toTargetForce * _config.TargetDrawForce;
    }
}
