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
    [SerializeField]
    Transform _partcilePoint;
    [SerializeField]
    AudioClip _defaultAttackSound;
    Vector3 _lastPos = Vector3.zero;
    HashSet<Sliceable> currentlySliced = new();
    List<IHittable> _alreadyHit = new();
    public List<IHittable> Slices => _alreadyHit;
    public void SetAttack(AttackDesc attack) {
        _curAttack = attack;
        _alreadyHit = new();
        if (_curAttack == null)
            return;
        var soundToPlay = _defaultAttackSound;
        if (_curAttack.SoundFX != null)
            soundToPlay = _curAttack.SoundFX;
        GameManager.T.PlayAudio(soundToPlay);
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
        foreach (var h in hittables) {
            _alreadyHit.Add(h);
            h.GotHitBy(attackData);
            var pos = transform.position + transform.up;
            if (_partcilePoint != null)
                pos = _partcilePoint.position;
            pos += h.transform.position;
            pos /= 2f;
            if(h.HType == HittableType.Object) 
                ParticleManager.T.MakeObjectHitAtSpot(pos);
            else 
                if (attackData.Attack.Stuns) 
                    ParticleManager.T.MakeStunAtSpot(pos);
                else
                    ParticleManager.T.MakeHitAtSpot(pos);
        }
    }
    void Update() {
        _lastPos = _attackPoint.position;
    }
    void Awake() {
        _origin = GetComponentInParent<Character>();
    }
}