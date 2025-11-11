using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

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
    float _repathIntervalMin = 0.1f;
    [SerializeField]
    float _repathIntervalMax = 0.2f;
    [SerializeField]
    float _repathThreshold = 0.5f;

    NavMeshAgent _agent;
    Vector3 _lastGoal;
    Transform _target;
    bool _hasGoal;
    Animator _anim;

    //----------------------------
    // SWARMING
    //----------------------------

    public void StartSwarmingPlayer() => _swarmingCo = StartCoroutine(SwarmPlayerCo());

    public void StopSwarmingPlayer() {
        if (_swarmingCo != null)
            StopCoroutine(_swarmingCo);
        _swarmingCo = null;
    }

    IEnumerator SwarmPlayerCo() {
        var wait = new WaitForSeconds(_repathIntervalMin);
        var config = GameManager.T.Config;
        _target = GameManager.T.Player.transform;

        while (true) {
            if (config != null && _target != null && _agent != null && _agent.isOnNavMesh) {
                var goal = CalculateTargetPoint();

                // Only update if the goal has moved enough
                if (!_hasGoal || (goal - _lastGoal).sqrMagnitude > _repathThreshold * _repathThreshold) {
                    _agent.SetDestination(goal);
                    _lastGoal = goal;
                    _hasGoal = true;
                }
            }

            float waitTime = UnityEngine.Random.Range(_repathIntervalMin, _repathIntervalMax);
            yield return new WaitForSeconds(waitTime);
        }
    }

    Vector3 CalculateTargetPoint() {
        var config = GameManager.T.Config;

        // Fallback if something's missing
        if (config == null || _target == null)
            return transform.position;

        // Use squad's member set as "bros"
        HashSet<Enemy> squadMembers = _squad != null ? _squad.SquadMembers : null;

        // If there are no other bros, just handle distance-to-target logic
        if (squadMembers == null || squadMembers.Count <= 1) {
            return CalculateSoloTarget();
        }

        Vector3 center = Vector3.zero;
        Vector3 repulseDir = Vector3.zero;

        float repulseThreshSqr = config.RepulseAtDistance * config.RepulseAtDistance;
        int broCount = 0;

        foreach (var bro in squadMembers) {
            if (bro == null || bro == this)
                continue;

            broCount++;
            center += bro.transform.position;

            var toBro = transform.position - bro.transform.position; // bro -> me
            float sqr = toBro.sqrMagnitude;

            if (sqr < repulseThreshSqr && sqr > 0.0001f) {
                float dist = Mathf.Sqrt(sqr); // correct: sqrt of sqrMagnitude
                float t = 1f - (dist / config.RepulseAtDistance);
                repulseDir += toBro.normalized * t;
            }
        }

        // If we didn't actually find any other valid bros, fall back to solo logic
        if (broCount == 0) {
            return CalculateSoloTarget();
        }

        center /= broCount;

        // Distance control relative to target (match Bro logic)
        Vector3 toTarget = _target.position - transform.position;
        float distToTarget = toTarget.magnitude;
        Vector3 toTargetForce = Vector3.zero;

        if (distToTarget > config.DesiredDistance + _distFudge) {
            toTargetForce = toTarget.normalized * (distToTarget - config.DesiredDistance);
        }
        else if (distToTarget < config.DesiredDistance - _distFudge) {
            toTargetForce = toTarget.normalized * (config.DesiredDistance - distToTarget) * -1f;
        }

        // Combine forces
        Vector3 groupDir = center - transform.position;
        Vector3 offset =
            groupDir * config.GroupForce +
            repulseDir * config.RepulseForce +
            toTargetForce * config.TargetDrawForce;

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
    // SCANNING
    //----------------------------

    public void StartScanForEnemy() =>
        _scanCo = StartCoroutine(ScanForEnemies());

    IEnumerator ScanForEnemies() {
        while (true) {
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
        if (_scanCo != null)
            StopCoroutine(_scanCo);
    }

    public void RegisterWithSquad() {
        _squad = GetComponentInParent<Squad>();
        if (_squad == null)
            throw new Exception($"{name} was not made as part of a squad!!!!!!");

        _squad.RegisterUnit(this);
    }

    Bounds GetBoundsOf(GameObject go) {
        Collider[] colliders = go.GetComponentsInChildren<Collider>();

        Bounds totalBounds = colliders[0].bounds;
        foreach (var col in colliders) {
            totalBounds.Encapsulate(col.bounds);
        }
        return totalBounds;
    }

    bool HasUnobstructedView(Vector3 eye, GameObject target, LayerMask mask) {
        if ((transform.position - target.transform.position).magnitude > GameManager.T.Config.SpotDistance)
            return false;

        Bounds bounds = GetBoundsOf(target);
        Vector3 randomPoint = new(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
            UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
        );
        Vector3 dir = randomPoint - eye;
        float distance = dir.magnitude;

        if (Physics.Raycast(eye, dir.normalized, out RaycastHit hit, distance, mask)) {
            return hit.collider.gameObject == target;
        }
        return true;
    }

    void Update() {
        if (_char.IsDead)
            return;
        if (_anim != null && _agent != null) {
            Vector3 localVel = transform.InverseTransformDirection(_agent.velocity);
            //_anim.SetFloat("forwardVelocity", localVel.z);
            //_anim.SetFloat("horizontalVelocity", localVel.x);
        }
    }

    void Die() {
        Debug.Log("Enemy called die");
        GameManager.T.Kill++;
        if (_scanCo != null)
            StopCoroutine(_scanCo);
        if (_swarmingCo != null)
            StopCoroutine(_swarmingCo);
        Destroy(_agent);
        Destroy(_anim);
    }

    void Start() {
        _squad = GetComponentInParent<Squad>();
        if (_squad == null)
            Debug.LogWarning($"Enemy: {name} is not part of a squad");
        else
            _squad.RegisterUnit(this);
        _char.OnDeath += Die;
    }

    void Awake() {
        _char = GetComponent<Character>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _char.Speed;
        _agent.angularSpeed = 360f;
        _agent.acceleration = 100f;
        _anim = GetComponentInChildren<Animator>();
    }
}
