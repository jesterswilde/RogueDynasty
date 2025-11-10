using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class Attacker : MonoBehaviour {
    int _queuedAttack = -1;
    int _curAttackI = -1;
    int _curAttackBranch = -1;
    bool _isPlaying = false;
    bool _isAcceptingInput = true;
    AttackDesc _curAttack;
    [SerializeField]
    List<AttackDesc> _basicAttacks;
    public List<AttackDesc> BasicAttacks => _basicAttacks;

    [SerializeField]
    List<AttackDesc> _powerAttacks;
    public List<AttackDesc> PowerAttacks => _powerAttacks;

    Animator _anim;
    AnimWatcher _animWatcher;
    public void QueueAttack(int num) {
        if (!_isAcceptingInput)
            return;
        Debug.Log($"Accepted input {num}");
        _queuedAttack = num;
        if (!_isPlaying) {
            PlayNextAttack();
        }
    }
    void PlayNextAttack() {
        Debug.Log($"Trying to paly next attack");
        if (_queuedAttack == -1) {
            EndAttackChain();
            return;
        }
        _curAttackI++;
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
        Debug.Log($"Playing {_curAttack.AnimName} attackI {_curAttackI} | Branch {_curAttackBranch}");
        _anim.Play(_curAttack.AnimName);
        _animWatcher.OnAnimTime(_curAttack.AnimName, 0.5f, ()=>{
            _isAcceptingInput = true;
        });
        _animWatcher.OnAnimEnd(_curAttack.AnimName, PlayNextAttack);
    }
    void EndAttackChain() {
        Debug.Log("Ending attack chain");
        _curAttackBranch = -1;
        _curAttackI = -1;
        _curAttack = null;
        _queuedAttack = -1;
        _isPlaying = false;
        _isAcceptingInput = false;
        IEnumerator AcceptAfter() {
            yield return new WaitForSeconds(0.5f);
            _isAcceptingInput = true;
        }
        StartCoroutine(AcceptAfter());
    }

    void Awake() {
        _anim = GetComponentInChildren<Animator>();
        _animWatcher = GetComponentInChildren<AnimWatcher>();
    }
}
public struct AttackData {
    public TeamMask TeamMask;
    public float Damage;
}
