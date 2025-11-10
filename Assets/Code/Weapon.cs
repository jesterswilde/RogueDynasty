using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using UnityEngine;

public class Weapon : MonoBehaviour {
    AttackDesc _curAttack;
    [SerializeField]
    Transform _attackPoint;
    [SerializeField]
    LayerMask _mask;
    Vector3 _lastPos = Vector3.zero;
    HashSet<Sliceable> currentlySliced = new();
    List<IHittable> _slices = new();
    public List<IHittable> Slices => _slices;
    public void SetAttack(AttackDesc attack) {
        _curAttack = attack;
    }
    void OnTriggerEnter(Collider other) {
        if (_curAttack == null)
            return;
        if (!_mask.Contains(other.gameObject))
            return;
        var hittables = other.gameObject.GetComponentsInParent<Component>().Where(c => c is IHittable).Select(c => c as IHittable);
        if (hittables.Count() == 0)
            return;
        var attackData = new AttackData {
            Damage = _curAttack.Damage,
            HitPlane = Slicer.CreatePlaneFromPoints(_attackPoint.position, _attackPoint.position + _attackPoint.forward, _lastPos)
        };
        foreach(var h in hittables) {
            h.GotHitBy(attackData);
        }
        
        
    }
    void Update() {
        _lastPos = _attackPoint.position;
    }
}