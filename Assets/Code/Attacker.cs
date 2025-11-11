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
    Character _char;
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
        _queuedAttack = num;
        if (!_isPlaying) {
            PlayNextAttack();
        }
    }
    void PlayNextAttack() {
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
        _char.Weapon.SetAttack(_curAttack);
        _anim.Play(_curAttack.AnimName);
        _animWatcher.OnAnimTime(_curAttack.AnimName, 0.5f, ()=>{
            _isAcceptingInput = true;
        });
        _animWatcher.OnAnimEnd(_curAttack.AnimName, PlayNextAttack);
    }
    void EndAttackChain() {
        _curAttackBranch = -1;
        _curAttackI = -1;
        _curAttack = null;
        _queuedAttack = -1;
        _isPlaying = false;
        _isAcceptingInput = false;
        _char.Weapon.SetAttack(null);
        IEnumerator AcceptAfter() {
            yield return new WaitForSeconds(0.5f);
            _isAcceptingInput = true;
        }
        StartCoroutine(AcceptAfter());
    }

    void Awake() {
        _anim = GetComponentInChildren<Animator>();
        _animWatcher = GetComponentInChildren<AnimWatcher>();
        _char = GetComponent<Character>();
        if (_anim == null)
            Debug.LogWarning($"No anim under Attacker for {name}");
        if (_animWatcher == null)
            Debug.LogWarning($"No animWatcher under Attacker for {name}");
        if (_char == null)
            Debug.LogWarning($"Attacker needs Character on same GO| {name}");
    }
}
