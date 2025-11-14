using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MinimapIcon))]
public class Enemy : MonoBehaviour {
    [FoldoutGroup("Path & Swarm"), SerializeField]
    Transform _eyePos;
    Squad _squad;
    Character _char;
    Coroutine _scanCo;
    Coroutine _swarmingCo;
    Coroutine _attackCo;
    Vector3 _oldPos;

    [FoldoutGroup("Path & Swarm"), SerializeField]
    float _distFudge = 0.5f;
    [FoldoutGroup("Path & Swarm"), SerializeField]
    float _repathIntervalMin = 0.1f;
    [FoldoutGroup("Path & Swarm"), SerializeField]
    float _repathIntervalMax = 0.2f;
    [FoldoutGroup("Path & Swarm"), SerializeField]
    float _repathThreshold = 0.2f;
    [FoldoutGroup("Path & Swarm"), SerializeField]
    float _faceEnemyDist = 5f;

    NavMeshAgent _agent;
    Attacker _attacker;
    Vector3 _lastGoal;
    Transform _target;
    bool _hasGoal;
    Animator _anim;
    MinimapIcon _minimapIcon;

    [SerializeField]
    AttackTable _attackTable;
    [SerializeField]
    Vector2 _attackInterval;
    [SerializeField]
    float _maxAttackRange;



    //----------------------------
    // SWARMING
    //----------------------------
    public void StartAttacking() {
        _attackCo = StartCoroutine(AttackCo());
    }
    public void StopAttacking() {
        StopCoroutine(_attackCo);
    }
    public IEnumerator AttackCo() {
        while (true) {
            float waitTime = UnityEngine.Random.Range(_attackInterval.x, _attackInterval.y);
            yield return new WaitForSeconds(waitTime);
            if (_target == null)
                continue;
            var toTarget = (_target.position - transform.position);
            var dist = toTarget.magnitude;
            if (dist > _maxAttackRange)
                continue;
            var entry = _attackTable.GetEntry();
            if (!entry.HasValue)
                continue;
            _attacker.QueueAttackSet(entry.Value.Combo);
        }
    }

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
        if (config == null || _target == null)
            return transform.position;

        HashSet<Enemy> squadMembers = _squad != null ? _squad.SquadMembers : null;
        if (squadMembers == null || squadMembers.Count <= 1) {
            // No bros → treat as solo
            return CalculateSoloTarget();
        }

        // --- compute squad center & repulsion as you already do ---
        Vector3 center = Vector3.zero;
        Vector3 repulseDir = Vector3.zero;

        float repulseThreshSqr = config.RepulseAtDistance * config.RepulseAtDistance;
        int broCount = 0;

        foreach (var bro in squadMembers) {
            if (bro == null || bro == this)
                continue;

            broCount++;
            center += bro.transform.position;

            var toBro = transform.position - bro.transform.position;
            float toBroSqr = toBro.sqrMagnitude;

            if (toBroSqr < repulseThreshSqr && toBroSqr > 0.0001f) {
                float dist = Mathf.Sqrt(toBroSqr);
                float t = 1f - (dist / config.RepulseAtDistance);
                repulseDir += toBro.normalized * t;
            }
        }

        if (broCount == 0)
            return CalculateSoloTarget();

        center /= broCount;

        // --- exact distance ring around the player ---
        Vector3 toTarget = _target.position - transform.position;
        float distToTarget = toTarget.magnitude;
        if (distToTarget < 0.001f)
            return transform.position;

        Vector3 dirToTarget = toTarget.normalized;
        float desiredDist = config.DesiredDistance;

        // Base position at exact distance from player
        Vector3 basePos = _target.position - dirToTarget * desiredDist;

        // --- group & repulse forces as offsets around that ring ---
        Vector3 groupDir = center - transform.position;

        Vector3 offset =
            groupDir * config.GroupForce +
            repulseDir * config.RepulseForce;

        // Optional: only apply offset when we’re not already close enough to desired ring
        if (Mathf.Abs(distToTarget - desiredDist) <= _distFudge)
            return basePos + offset;

        return basePos + offset;
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
            if(GameManager.T.Player == null) {
                StopCoroutine(_scanCo);
                yield return new WaitForSeconds(20);
            }
            else {
                bool didSee = HasUnobstructedView(_eyePos.position, GameManager.T.Player.gameObject, scanMask);
                if (didSee)
                    _squad.SawEnemy();
            }
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
        if (_attacker.IsPlaying)
            transform.LookAt(_target);
        if (_squad.SeesEnemy && _target != null) {
            var toEnemy = _target.position - transform.position;
            if ((_faceEnemyDist * _faceEnemyDist) < toEnemy.sqrMagnitude) {
                transform.LookAt(_target);
            }

        }
        if (_anim != null && _agent != null) {
            var diff = (transform.position - _oldPos) / Time.deltaTime;
            var diffMag = diff.magnitude;
            if (diffMag < 0.01f) {
                _anim.SetFloat("forwardVelocity", 0f);
                _anim.SetFloat("horizontalVelocity", 0f);
            }
            else {
                var norm = diff.normalized;
                var forward = Vector3.Dot(transform.forward, norm * _char.Speed);
                var right = Vector3.Dot(transform.right, norm * _char.Speed);
                _anim.SetFloat("forwardVelocity", forward);
                _anim.SetFloat("horizontalVelocity", right);
            }
        }
        _oldPos = transform.position;
    }

    void Die() {
        GameManager.T.Kill++;
        if (_scanCo != null)
            StopCoroutine(_scanCo);
        if (_swarmingCo != null)
            StopCoroutine(_swarmingCo);
    }
    void OnStun() {
        _agent.speed = 0;
        _agent.ResetPath();
        _agent.enabled = false;
    }
    void OnStunOver() {
        _agent.speed = _char.Speed;
        _agent.enabled = true;
    }

    void Start() {
        _squad = GetComponentInParent<Squad>();
        if (_squad == null)
            Debug.LogWarning($"Enemy: {name} is not part of a squad");
        else
            _squad.RegisterUnit(this);
        _char.OnDeath += Die;
        _char.OnStun += OnStun;
        _char.OnStunEnd += OnStunOver;
    }

    void Awake() {
        _char = GetComponent<Character>();
        _attacker = GetComponentInChildren<Attacker>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _char.Speed;
        _agent.angularSpeed = 360f;
        _agent.acceleration = 100f;
        _anim = GetComponentInChildren<Animator>();
        _minimapIcon = GetComponent<MinimapIcon>();
        if (_minimapIcon == null)
            Debug.LogWarning($"Enemy {name} is missing a MinimapIcon component");
        else
            _minimapIcon.SetIconType(MinimapIconType.Enemy);
    }
}
