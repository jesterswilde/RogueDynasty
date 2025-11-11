using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Unity.VisualScripting;
using UnityEngine;

public class Weapon : MonoBehaviour {
    AttackDesc _curAttack;
    Character _origin;
    [SerializeField]
    Transform _attackPoint;
    [SerializeField]
    LayerMask _mask;
    Vector3 _lastPos = Vector3.zero;
    HashSet<Sliceable> currentlySliced = new();
    List<IHittable> _alreadyHit = new();
    public List<IHittable> Slices => _alreadyHit;
    public void SetAttack(AttackDesc attack) {
        _curAttack = attack;
        _alreadyHit = new();
    }
    void OnTriggerEnter(Collider other) {
        if (_curAttack == null)
            return;
        if (!_mask.Contains(other.gameObject))
            return;
        var hittables = other.gameObject.GetComponentsInParent<Component>().Where(c => c is IHittable).Select(c => c as IHittable).Where(h => !_alreadyHit.Contains(h));
        if (hittables.Count() == 0)
            return;
        var attackData = new AttackData {
            Damage = _curAttack.Damage,
            HitPlane = Slicer.CreatePlaneFromPoints(_attackPoint.position, _attackPoint.position + _attackPoint.up, _lastPos),
            HitPosition = _attackPoint.position,
            HitDirection = (_attackPoint.position - _lastPos).normalized,
            FromOrigin = (_attackPoint.position - _origin.transform.position).normalized,
            Attack = _curAttack
        };
        //var xMin = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
        //var yMin = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
        //var zMin = Mathf.Min(a.z, Mathf.Min(b.z, c.z));
        //var xMax = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
        //var yMax = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
        //var zMax = Mathf.Max(a.z, Mathf.Max(b.z, c.z));
        //Debug.Log($"{xMax - xMin} {yMax - yMin} {zMax - zMin}");
        foreach (var h in hittables) {
            _alreadyHit.Add(h);
            h.GotHitBy(attackData);
        }
    }
    void Update() {
        _lastPos = _attackPoint.position;
    }
    void Awake() {
        _origin = GetComponentInParent<Character>();
    }
}