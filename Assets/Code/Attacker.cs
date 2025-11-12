using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Attacker : MonoBehaviour {
    int _queuedAttack = -1;
    List<int> _queuedAttacks;
    int _curAttackI = -1;
    int _curAttackBranch = -1;
    bool _isPlaying = false;
    public bool IsPlaying => _isPlaying;
    bool _isAcceptingInput = true;
    AttackDesc _curAttack;
    Character _char;
    ParticleSystem _pSystem;
    [SerializeField]
    List<AttackDesc> _basicAttacks;
    public List<AttackDesc> BasicAttacks => _basicAttacks;

    [SerializeField]
    List<AttackDesc> _powerAttacks;
    public List<AttackDesc> PowerAttacks => _powerAttacks;
    [SerializeField]
    float _pAnimStart = 0.2f;
    [SerializeField]
    float _pAnimEnd = 0.2f;
    public float AttackSpeedMod { get; set; } = 0;

    Animator _anim;
    AnimWatcher _animWatcher;
    public void Interrupt() {
        _curAttackBranch = -1;
        _curAttackI = -1;
        _curAttack = null;
        _queuedAttack = -1;
        _queuedAttacks = new();
        _isPlaying = false;
        _isAcceptingInput = false;
        _char.Weapon.SetAttack(null);
        _isAcceptingInput = false;
        _pSystem?.Stop();
        _anim.speed = 1f;
    }
    void StartParticles()=>
        _pSystem?.Play();
    void StopParticles()=>
        _pSystem?.Stop();
    public void ForceAcceptInput() {
        _isAcceptingInput = true;
    }
    public void QueueAttackSet(List<int> attacks) {
        if (!_isPlaying && _isAcceptingInput) {
            _queuedAttacks = new(attacks);
            _curAttackBranch = -1;
            _curAttackI = -1;
            _curAttack = null;
            _queuedAttack = -1;
            PlayNextAttack();
        }
    }
    public void QueueAttack(int num) {
        if (!_isAcceptingInput)
            return;
        _queuedAttack = num;
        if (!_isPlaying) {
            PlayNextAttack();
        }
    }
    void PlayNextAttack() {
        if(_queuedAttacks != null && _queuedAttacks.Count > 0) {
            _queuedAttack = _queuedAttacks[0];
            _queuedAttacks.RemoveAt(0);
        }
        if (_queuedAttack == -1) {
            EndAttackChain();
            return;
        }
        if(_curAttack?.isLoopable != true)
            _curAttackI++;
        _anim.speed = (_curAttack?.AttackSpeed ?? 1) + AttackSpeedMod;
        _curAttackBranch = _queuedAttack;
        var attackList = _curAttackBranch == 0 ? _basicAttacks : _powerAttacks;
        if (attackList.Count <= _curAttackI || _curAttack?.endsCombo == true) {
            EndAttackChain();
            return;
        }

        _queuedAttack = -1;
        _isPlaying = true;
        _isAcceptingInput = false;
        _curAttack = attackList[_curAttackI];
        _char.Weapon.SetAttack(_curAttack);
        _anim.Play(_curAttack.AnimName);
        _animWatcher.OnAnimTime(_curAttack.AnimName, 0.5f, ()=>{
            _isAcceptingInput = true;
        });
        _animWatcher.OnAnimEnd(_curAttack.AnimName, PlayNextAttack);
        _animWatcher.OnAnimTime(_curAttack.AnimName, _pAnimStart, StartParticles );
        _animWatcher.OnAnimTime(_curAttack.AnimName, _pAnimEnd, StopParticles );
    }
    void EndAttackChain() {
        _curAttackBranch = -1;
        _curAttackI = -1;
        _curAttack = null;
        _queuedAttack = -1;
        _isPlaying = false;
        _isAcceptingInput = false;
        _char.Weapon.SetAttack(null);
        _anim.speed = 1f;
        _pSystem?.Stop();
        //_anim.CrossFade("movementTree", 0.25f);
        IEnumerator AcceptAfter() {
            yield return new WaitForSeconds(0.5f);
            _isAcceptingInput = true;
        }
        StartCoroutine(AcceptAfter());
    }
    void Start() {
        _pSystem?.Stop();
    }
    void Awake() {
        _anim = GetComponentInChildren<Animator>();
        _animWatcher = GetComponentInChildren<AnimWatcher>();
        _char = GetComponent<Character>();
        _pSystem = GetComponentInChildren<ParticleSystem>();
        if (_anim == null)
            Debug.LogWarning($"No anim under Attacker for {name}");
        if (_animWatcher == null)
            Debug.LogWarning($"No animWatcher under Attacker for {name}");
        if (_char == null)
            Debug.LogWarning($"Attacker needs Character on same GO| {name}");
    }
}
