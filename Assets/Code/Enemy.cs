using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Character), typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    [SerializeField]
    Transform _eyePos;
    Squad _squad;
    Character _char;
    Coroutine _scanCo;
    Coroutine _swarmingCo;

    
    [SerializeField]
    float _distFudge = 0.5f;
    [SerializeField]
    float _repathInterval = 1f;
    [SerializeField]
    float _repathThreshold = 0.5f;
    HashSet<Enemy> _bros;

    NavMeshAgent _agent;
    Vector3 _lastGoal;
    Transform _target;
    bool _hasGoal;



    //----------------------------
    //SWARMING--------------------

    System.Collections.IEnumerator RepathRoutine() {
        var wait = new WaitForSeconds(_repathInterval);
        var config = GameManager.T.Config;

        while (true) {
            if ( config != null && _target != null && _agent != null && _agent.isOnNavMesh) {
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

    Vector3 CalculateTargetPoint() {
        var _config = GameManager.T.Config;
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
        var config = GameManager.T.Config;
        Vector3 toTarget = _target.position - transform.position;
        float distToTarget = toTarget.magnitude;
        Vector3 toTargetForce = Vector3.zero;

        if (distToTarget > config.DesiredDistance + _distFudge) {
            toTargetForce = toTarget.normalized * (distToTarget - config.DesiredDistance);
        }
        else if (distToTarget < config.DesiredDistance - _distFudge) {
            toTargetForce = toTarget.normalized * (config.DesiredDistance - distToTarget);
        }

        return transform.position + toTargetForce * config.TargetDrawForce;
    }

    //----------------------------
    //SCANNING--------------------
    public void StartScanForEnemy()=>
        _scanCo = StartCoroutine(ScanForEnemies());
    IEnumerator ScanForEnemies(){
        while (true)
        {
            var scanInterval = GameManager.T.Config.EnemyScanInterval;
            var scanMask = GameManager.T.Config.EnemyScanMask;
            float waitTime = UnityEngine.Random.Range(scanInterval.x, scanInterval.y);
            yield return new WaitForSeconds(waitTime);
            bool didSee = HasUnobstructedView(_eyePos.position, GameManager.T.Player.gameObject, scanMask);
            if (didSee)
                _squad.SawEnemy();
        }
    }
    public void StopScanForEnemy() {
        if(_scanCo != null)
            StopCoroutine(_scanCo);
    }
    public void RegisterWithSquad()
    {
        _squad = GetComponentInParent<Squad>();
        if (_squad == null)
            throw new Exception($"{name} was not made as part of a squad!!!!!!");

        _squad.RegisterUnit(this);
    }
    bool HasUnobstructedView(Vector3 eye, GameObject target, LayerMask mask) {
        Collider col = target.GetComponent<Collider>();
        if (col == null) {
            Debug.LogWarning("Target has no collider!");
            return false;
        }

        Vector3 randomPoint = new(
            UnityEngine.Random.Range(col.bounds.min.x, col.bounds.max.x),
            UnityEngine.Random.Range(col.bounds.min.y, col.bounds.max.y),
            UnityEngine.Random.Range(col.bounds.min.z, col.bounds.max.z)
        );
        Vector3 dir = randomPoint - eye;
        float distance = dir.magnitude;

        if (Physics.Raycast(eye, dir.normalized, out RaycastHit hit, distance, mask)) {
            return hit.collider.gameObject == target;
        }
        return true;
    }

    void Awake() {
        _char = GetComponent<Character>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _char.Speed;
        _agent.angularSpeed = 360f;
        _agent.acceleration = 100f;
    }
}
