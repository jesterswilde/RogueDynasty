using System.Collections;
using System.Collections.Generic; // <-- needed for List<>
using EzySlice;
using UnityEngine;

public class DestructableObject : MonoBehaviour, IHittable {
    [SerializeField]
    float _maxHealth;
    float _health;
    bool _canBeHit = false;

    public void GotHitBy(AttackData attack) {
        if (!_canBeHit)
            return;

        _health -= attack.Damage;

        if (_health <= 0) {
            // 1. Cache parent mass before we destroy this object
            float parentMass = 1f;
            var parentRb = GetComponent<Rigidbody>();
            if (parentRb != null) {
                parentMass = parentRb.mass;
            }

            var sliced = gameObject.SliceInstantiate(
                attack.HitPosition,
                attack.HitPlane.normal,
                GameManager.T.Innards
            );

            if (sliced != null && sliced.Length > 0) {
                // 2. First pass: compute volumes
                List<GameObject> chunks = new List<GameObject>(sliced);
                List<float> volumes = new List<float>(chunks.Count);

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

                // 3. Second pass: set up physics + mass
                for (int i = 0; i < chunks.Count; i++) {
                    var s = chunks[i];

                    s.layer = gameObject.layer;

                    var meshCollider = s.AddComponent<MeshCollider>();
                    meshCollider.convex = true;

                    var rb = s.AddComponent<Rigidbody>();
                    rb.useGravity = true;
                    rb.linearDamping = 0.2f;
                    rb.angularDamping = 0.5f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                    float volume = volumes[i];
                    float fraction = volume / totalVolume;

                    rb.mass = parentMass * fraction;

                    // Make this slice destructible too
                    var dest = s.AddComponent<DestructableObject>();
                    dest._maxHealth = (_maxHealth / 2f) + 1f;
                }
            }

            Destroy(gameObject);
        }
    }

    IEnumerator EnableGetHit() {
        yield return new WaitForSeconds(1);
        _canBeHit = true;
    }

    void Start() {
        StartCoroutine(EnableGetHit());
    }

    void Awake() {
        _health = _maxHealth;
    }
}
