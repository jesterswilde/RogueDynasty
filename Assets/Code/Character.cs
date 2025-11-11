using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EzySlice;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
public class Character : MonoBehaviour, IHittable {
    [Header("Stats")]
    [SerializeField] float _speed = 5f;
    public float Speed { get => _speed; set => _speed = value; }
    [SerializeField] 
    float _jumpSpeed = 7f;
    [SerializeField]
    float _maxHealth = 100f;
    public float MaxHealth => _maxHealth;
    [SerializeField]
    float _health;
    public float Health => _health;
    bool _isJumping = false;
    Attacker _attacker;
    Animator _anim;
    AnimWatcher _animWatcher;
    public event Action OnDeath;
    public event Action<float> OnHealthChange;
    public event Action OnStun;
    public event Action OnStunEnd;
    Weapon _weapon;
    public Weapon Weapon => _weapon;

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
    Vector3 _pos3;
    Vector3 _pos2;
    Vector3 _pos1;
    Vector3 _pos0;

    Rigidbody _rigid;
    Vector3 _moveInput;
    bool _isGrounded;
    Vector3 _groundNormal = Vector3.up;
    float _currentSlopeAngle;
    bool _isDead;
    bool _isStunned;
    public bool IsStunned => _isStunned;
    List<(float, float)> _recentDamage = new();
    [SerializeField]
    float _damageNeededForStun = 0.15f;
    [SerializeField]
    float _damangeStackingDuration = 2f;


    public bool IsDead => _isDead;

    public void GotHitBy(AttackData attack) {
        _health -= attack.Damage;
        Debug.Log($"{name} got hit by {attack.Attack.name} {_health} {attack.Damage}");
        _recentDamage.Add((_health, Time.time));
        HandleStun(attack);
        OnHealthChange?.Invoke(_health);
        if (_health <= 0)
            Die(attack);
    }

    void HandleStun(AttackData attack) {
        if (_isStunned)
            return;
        _isStunned = attack.Attack.Stuns;
        if (!_isStunned) {
            var now = Time.time;
            for(int i = _recentDamage.Count - 1; i >= 0; i--) {
                var invalid = (_recentDamage[i].Item2 - (now - _damangeStackingDuration)) < 0;
                if (invalid)
                    _recentDamage.RemoveAt(i);
            }
            var totalDamage = _recentDamage.Aggregate(0f, (a, b) => a + b.Item1);
            _isStunned = totalDamage / _maxHealth > _damageNeededForStun;
        }
        if (_isStunned) {
            _attacker.Interrupt();
            _anim.Play("isHit");
            _animWatcher.OnAnimEnd("isHit", StunOver);
        }
    }
    void StunOver() {
        _isStunned = false;
        _attacker.ForceAcceptInput();
    }

    public void ModifyHealth(float amount) {
        _health += amount;
        if (_health < 0)
            Die();
        else
            _health = Mathf.Min(_health, _maxHealth);
        OnHealthChange?.Invoke(_health);
    }

    void Die() {
        if (_isDead) return;
        _isDead = true;
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    void Die(AttackData data) {
        if (_isDead) return;
        _isDead = true;
        OnDeath?.Invoke();
        var agent = GetComponent<NavMeshAgent>();
        if (agent != null) {
            agent.ResetPath();
            agent.enabled = false;
        }
        if (_rigid != null) {
            _rigid.linearVelocity = Vector3.zero;
            _rigid.angularVelocity = Vector3.zero;
            _rigid.isKinematic = true;
        }
        StartCoroutine(DeathSliceRoutine(data, _pos3));
    }

    System.Collections.IEnumerator DeathSliceRoutine(AttackData data, Vector3 oldPos) {
        // Wait a frame and a physics step to ensure everything is settled
        yield return null;
        yield return null;

        // Snap back to the stored position before baking/slicing
        transform.position = oldPos;

        var anim = GetComponentInChildren<Animator>();
        if (anim) anim.speed = 0f;

        // Detach from any parent so the pieces are independent
        transform.SetParent(null, true);

        // Bake skinned meshes into regular meshes
        var bakedObjects = SkinnedMeshBaker.BakeHiearchy(gameObject);

        foreach (var baked in bakedObjects) {
            var avgPos = (data.HitPosition + (3 * (transform.position + transform.up))) / 4;
            var sliced = baked.SliceInstantiate(data.HitPosition, data.HitPlane.normal, GameManager.T.Innards);

            if (sliced == null || !sliced.Any()) {
                Destroy(baked);
                continue;
            }

            // Clean any existing colliders
            foreach (var s in sliced) {
                if (s == null) continue;
                foreach (var existingColl in s.GetComponentsInChildren<Collider>())
                    Destroy(existingColl);
            }

            var pieces = sliced.ToArray();
            for (int i = 0; i < pieces.Length; i++) {
                var piece = pieces[i];
                if (piece == null) continue;

                var meshCollider = piece.AddComponent<MeshCollider>();
                meshCollider.convex = true;

                var rb = piece.AddComponent<Rigidbody>();
                rb.useGravity = true;
                rb.mass = 2f;
                rb.linearDamping = 0.2f;
                rb.angularDamping = 0.5f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                float side = (i % 2 == 0) ? 1f : -1f;
                const float separation = 0.05f;
                piece.transform.position += data.HitPlane.normal * separation * side;
                piece.layer = 9;

                piece.AddComponent<DepenetrateOnSpawn>();

                const float impulseStrength = 1.5f;
                rb.AddForce(data.HitPlane.normal * side * impulseStrength, ForceMode.Impulse);
            }

            // Prevent the two halves immediately colliding with each other
            if (pieces.Length >= 2) {
                var colA = pieces[0].GetComponent<Collider>();
                var colB = pieces[1].GetComponent<Collider>();
                if (colA && colB)
                    Physics.IgnoreCollision(colA, colB, true);
            }

            Destroy(baked);
        }

        // Remove the original character object after spawning the pieces
        Destroy(gameObject);
    }

    public void Move(Vector3 dir) {
        _moveInput = Vector3.ClampMagnitude(dir, 1f);
    }

    public void Jump() {
        if (!_isGrounded) return;
        if (_rigid.isKinematic) return; // can't jump when kinematic

        Vector3 vel = _rigid.linearVelocity;
        vel.y = _jumpSpeed;
        _rigid.linearVelocity = vel;
        _isJumping = true;
        _isGrounded = false;
        _anim.Play("jump");
    }

    void UpdateGroundInfo() {
        _isGrounded = _groundDetector != null && _groundDetector.IsBlocked;
        _groundNormal = Vector3.up;
        _currentSlopeAngle = 0f;
        if (_isJumping && _isGrounded && _rigid.linearVelocity.y <= 0) {
            _isJumping = false;
            _anim.CrossFade("land", 0.25f);
        }

        if (!_isGrounded)
            return;

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float checkDistance = _groundCheckDistance + 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, checkDistance, _groundMask, QueryTriggerInteraction.Ignore)) {
            _groundNormal = hit.normal;
            _currentSlopeAngle = Vector3.Angle(_groundNormal, Vector3.up);

            if (_currentSlopeAngle > _slopeDegree) {
                _isGrounded = false;
                _groundNormal = Vector3.up;
            }
        }
    }

    void HandleMovement() {
        Vector3 input = _moveInput;
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 moveDir = input;

        if (_isGrounded)
            moveDir = Vector3.ProjectOnPlane(moveDir, _groundNormal).normalized;

        float t = Mathf.Clamp01(_currentSlopeAngle / _slopeDegree);
        float speedFactor = Mathf.Lerp(1f, _minSlopeSpeedFactor, t);
        Vector3 desiredVel = moveDir * _speed * speedFactor;

        if (!_rigid.isKinematic) {
            // Dynamic body – use velocity
            Vector3 rbVel = _rigid.linearVelocity;
            rbVel.x = desiredVel.x;
            rbVel.z = desiredVel.z;
            _rigid.linearVelocity = rbVel;
        }
        else {
            // Kinematic – use MovePosition
            Vector3 targetPos = transform.position + desiredVel * Time.fixedDeltaTime;
            _rigid.MovePosition(targetPos);
        }
    }

    void Update() {
        _pos3 = _pos2;
        _pos2 = _pos1;
        _pos1 = _pos0;
        _pos0 = transform.position;
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
        _attacker = GetComponent<Attacker>();
        _anim = GetComponentInChildren<Animator>();
        _animWatcher = GetComponentInChildren<AnimWatcher>();
        _weapon = GetComponentInChildren<Weapon>();
    }
}
