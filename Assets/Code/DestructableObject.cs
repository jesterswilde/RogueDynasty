using System;
using System.Collections;
using System.Collections.Generic;
using EzySlice;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Video;

public class DestructableObject : MonoBehaviour, IHittable {
    [SerializeField]
    float _maxHealth;
    [SerializeField]
    float _breakVelocity = 0.5f;
    float _health;
    bool _canBeHit = false;
    Vector3 _lastPos;
    float _maxSpeed = 20;
    Rigidbody _rigid;
    [SerializeField]
    AudioClip _hitSound;
    [SerializeField]
    AudioClip _destroyedSound;
    [SerializeField]
    int _soundToDepth = 2;


    HittableType IHittable.HType => HittableType.Object;
    public void GotHitBy(AttackData attack) {
        if (!_canBeHit)
            return;

        _health -= attack.Damage;
        if (_health > 0 && _hitSound != null) {
            GameManager.T.PlayAudio(_hitSound);
        }
        else if (_health <= 0) {
            transform.SetParent(null, worldPositionStays: true);

            var sliced = gameObject.SliceInstantiate(
                attack.HitPosition,
                attack.HitPlane.normal,
                GameManager.T.Innards
            );
            if(sliced == null || sliced.Length == 0) {
                var rends = GetComponentsInChildren<Renderer>();
                var center = rends[0].bounds.center;
                sliced = gameObject.SliceInstantiate(
                    center, 
                    attack.HitPlane.normal,
                    GameManager.T.Innards
                );
            }
            //if (sliced == null)
            //    throw new System.Exception("Still errrrrrr");
            CommitSlice(sliced, attack);
        }
    }
    void CommitSlice(GameObject[] sliced, AttackData attack) {
        if (sliced != null && sliced.Length > 0) {
            List<GameObject> chunks = new List<GameObject>(sliced);
            List<float> volumes = new List<float>(chunks.Count);

            float parentMass = 1f;
            var parentRb = GetComponent<Rigidbody>();
            if (parentRb != null) {
                parentMass = parentRb.mass;
            }

            float totalVolume = 0f;
            foreach (var s in chunks) {
                // Try renderer for AABB; could also use collider later
                var renderer = s.GetComponentInChildren<Renderer>();
                float volume = 0f;

                if (renderer != null) {
                    Bounds b = renderer.bounds;
                    volume = b.size.x * b.size.y * b.size.z;
                }

                volumes.Add(volume);
                totalVolume += volume;
            }

            // Avoid division by zero: fallback to equal share if no volume found
            if (totalVolume <= Mathf.Epsilon) {
                totalVolume = volumes.Count; // makes each slice get 1/Count
                for (int i = 0; i < volumes.Count; i++) {
                    volumes[i] = 1f; // equal weights
                }
            }

            float dir = 1;
            // 3. Second pass: set up physics + mass
            for (int i = 0; i < chunks.Count; i++) {
                var s = chunks[i];

                s.layer = gameObject.layer;

                try {
                    float volume = volumes[i];
                    float fraction = volume / totalVolume;

                    var meshCollider = s.AddComponent<MeshCollider>();
                    meshCollider.convex = true;

                    var rb = s.AddComponent<Rigidbody>();
                    rb.useGravity = true;
                    rb.linearDamping = 0.2f;
                    rb.angularDamping = 0.5f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.mass = parentMass * fraction;
                    rb.AddForce(attack.HitDirection * dir * _breakVelocity * rb.mass, ForceMode.Impulse);
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                } catch (Exception e) {
                    Destroy(GetComponent <MeshCollider>());
                    s.AddComponent<CapsuleCollider>();
                }



                // Make this slice destructible too
                var dest = s.AddComponent<DestructableObject>();
                dest._maxHealth = (_maxHealth / 2f) + 1f;
                dest._breakVelocity = _breakVelocity;
                dest._lastPos = s.transform.position;
                dir *= -1;
                var nextDepth = _soundToDepth--;
                if (nextDepth >= 0) {
                    dest._soundToDepth = nextDepth;
                    dest._hitSound = _hitSound;
                    dest._destroyedSound = _destroyedSound;
                }
            }
        }
        Destroy(gameObject);
    }

    IEnumerator EnableGetHit() {
        yield return new WaitForSeconds(1);
        _canBeHit = true;
    }
    void FixedUpdate() {
        if (_rigid.IsSleeping())
            return;
        var spdSqr = Time.fixedDeltaTime * _maxSpeed;
        spdSqr *= spdSqr;
        var travelDist = (_lastPos - transform.position).sqrMagnitude;
        if (travelDist > spdSqr) {
            transform.position = _lastPos;
            _rigid.linearVelocity = Vector3.zero;
        }
        else
            _lastPos = transform.position;
    }

    void Start() {
        StartCoroutine(EnableGetHit());
    }

    void Awake() {
        _health = _maxHealth;
        _rigid = GetComponent<Rigidbody>();
        _lastPos = transform.position;
    }
}
