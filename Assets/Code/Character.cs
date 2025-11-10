using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Character : MonoBehaviour, IHittable
{
    [Header("Stats")]
    [SerializeField]
    float _speed = 5f;
    public float Speed { get => _speed; set => _speed = value; }
    [SerializeField]
    float _jumpSpeed = 7f;
    [SerializeField]
    float _maxHealth = 100;
    float _health = 100f;
    [SerializeField]
    TeamMask _team;

    [Header("Ground & Slopes")]
    [SerializeField]
    Detector _groundDetector;
    [SerializeField]
    LayerMask _groundMask = ~0;
    [SerializeField]
    float _groundCheckDistance = 0.5f;
    [SerializeField]
    float _slopeDegree = 45f;
    [SerializeField, Range(0f, 1f)]
    float _minSlopeSpeedFactor = 0.6f;

    Rigidbody _rigid;
    Vector3 _moveInput;
    bool _isGrounded;
    Vector3 _groundNormal = Vector3.up;
    float _currentSlopeAngle;
    float _isDead;

    public void OnHit(AttackData attack) {
        if ((_team & attack.TeamMask) <= 0)
            return;

        _health -= attack.Damage;
        if (_health <= 0)
            Die();
    }
    /// <summary>
    /// Used for things like potions. if hit by attack, use onHit
    /// </summar>
    public void ModifyHealth(float amount) {
        _health += amount;
        if (_health < 0)
            Die();
        else
            _health = Mathf.Min(_health, _maxHealth);
    }

    void Die() {
        throw new NotImplementedException();
    }

    /// <summary>
    /// World-space movement direction (not scaled by speed).
    /// Call this every frame from input / AI.
    /// </summary>
    public void Move(Vector3 dir)
    {
        _moveInput = Vector3.ClampMagnitude(dir, 1f);
    }

    /// <summary>
    /// Attempts to jump if grounded
    /// </summary>
    public void Jump()
    {
        if (!_isGrounded)
            return;

        Vector3 vel = _rigid.linearVelocity;
        vel.y = _jumpSpeed;
        _rigid.linearVelocity = vel;
        _isGrounded = false;
    }



    void UpdateGroundInfo()
    {
        _isGrounded = _groundDetector != null && _groundDetector.IsBlocked;
        _groundNormal = Vector3.up;
        _currentSlopeAngle = 0f;

        if (!_isGrounded)
            return;

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float checkDistance = _groundCheckDistance + 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, checkDistance, _groundMask, QueryTriggerInteraction.Ignore)) {
            _groundNormal = hit.normal;
            _currentSlopeAngle = Vector3.Angle(_groundNormal, Vector3.up);

            if (_currentSlopeAngle > _slopeDegree)
            {
                _isGrounded = false;
                _groundNormal = Vector3.up;
            }
        }
    }

    void HandleMovement()
    {
        Vector3 input = _moveInput;

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 moveDir = input;

        if (_isGrounded)
        {
            // Project movement onto the ground plane so we stick to slopes
            moveDir = Vector3.ProjectOnPlane(moveDir, _groundNormal).normalized;

            float t = Mathf.Clamp01(_currentSlopeAngle / _slopeDegree);
            float speedFactor = Mathf.Lerp(1f, _minSlopeSpeedFactor, t);

            Vector3 desiredVel = moveDir * _speed * speedFactor;

            Vector3 rbVel = _rigid.linearVelocity;
            rbVel.x = desiredVel.x;
            rbVel.z = desiredVel.z;
            _rigid.linearVelocity = rbVel;
        }
        else
        {
            Vector3 desiredVel = moveDir * _speed;

            Vector3 rbVel = _rigid.linearVelocity;
            rbVel.x = desiredVel.x;
            rbVel.z = desiredVel.z;
            _rigid.linearVelocity = rbVel;
        }
    }

    void FixedUpdate() {
        UpdateGroundInfo();
        HandleMovement();
    }
    void Awake() {
        _rigid = GetComponent<Rigidbody>();
        _rigid.constraints = RigidbodyConstraints.FreezeRotation;
        if (_groundDetector == null)
            _groundDetector = GetComponentInChildren<Detector>();
        _health = _maxHealth;
    }
}

class SlicingBlade : MonoBehaviour {
    [SerializeField]
    LayerMask _mask;
    Vector3 _lastPos = Vector3.zero;
    HashSet<Sliceable> currentlySliced = new();
    List<SlicingData> _slices = new();
    public List<SlicingData> Slices => _slices;
    void OnTriggerEnter(Collider other) {
        if (!_mask.Contains(other.gameObject))
            return;
        var sliceable = other.gameObject.GetComponentInParent<Sliceable>();
        if (sliceable == null)
            sliceable = other.gameObject.GetComponent<Sliceable>();
        if (sliceable == null)
            return;
        if (currentlySliced.Contains(sliceable))
            return;
    }
    void Update() {
        _lastPos = transform.position;
    }
}
struct SlicingData {
    Sliceable Sliced;
    Vector3 Position;
    Vector3 Up;
}