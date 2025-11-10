using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class AnimWatcher : MonoBehaviour {
    Animator _anim;

    List<TimedAnimCB> _pendingTimeCBs = new();
    List<TimedAnimCB> _pendingEndCBs = new();

    List<TimedAnimCB> _activeTimedCBs = new();
    List<TimedAnimCB> _activeEndCBs = new();

    int _lastHash = -1;
    float _lastTime = -1f;

    /// <summary>
    /// Invoke action once when the given state's normalized time passes 'time'.
    /// </summary>
    public void OnAnimTime(string animName, float time, Action action) {
        var animHash = Animator.StringToHash(animName);
        var cb = new TimedAnimCB {
            HasFired = false,
            Time = time,
            Hash = animHash,
            Action = action
        };

        if (_lastHash == animHash) {
            _activeTimedCBs.Add(cb);
        } else {
            _pendingTimeCBs.Add(cb);
        }
    }

    /// <summary>
    /// Invoke action once when the given state finishes (or is exited).
    /// </summary>
    public void OnAnimEnd(string animName, Action action) {
        var animHash = Animator.StringToHash(animName);
        var cb = new TimedAnimCB {
            HasFired = false,
            Time = -1,
            Hash = animHash,
            Action = action
        };

        if (_lastHash == animHash) {
            _activeEndCBs.Add(cb);
        } else {
            _pendingEndCBs.Add(cb);
        }
    }

    void ClearActive() {
        _activeEndCBs.Clear();
        _activeTimedCBs.Clear();
    }

    void Awake() {
        _anim = GetComponent<Animator>();
        if (_anim == null)
            throw new Exception($"No animator on {name}");
    }

    void LateUpdate() {
        if (_anim == null) return;

        var stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
        int currentHash = stateInfo.shortNameHash;
        float currentTime = stateInfo.normalizedTime;
        bool isLooping = stateInfo.loop;

        var pendingHash = -1;
        if (_pendingEndCBs.Count > 0)
            pendingHash = _pendingEndCBs[0].Hash;

        // First frame initialization
        if (_lastHash == -1) {
            _lastHash = currentHash;
            _lastTime = currentTime;
            ActivatePendingFor(currentHash);
            return;
        }

        if (currentHash != _lastHash) {
            // The previous state just ended / was exited.
            FireEndCallbacksFor(_lastHash);
            ClearActive();

            // Switch to new state, activate its pending callbacks
            _lastHash = currentHash;
            _lastTime = currentTime;
            ActivatePendingFor(currentHash);
            return;
        }

        // Same state: detect clip end for non-looping states
        bool clipEnded = !isLooping && _lastTime < 1f && currentTime >= 1f;
        if (clipEnded) {
            FireEndCallbacksFor(currentHash);
            ClearActive();
        }

        // Fire timed callbacks when we pass their normalized time
        for (int i = 0; i < _activeTimedCBs.Count; i++) {
            var cb = _activeTimedCBs[i];
            if (!cb.HasFired && _lastTime < cb.Time && currentTime >= cb.Time) {
                cb.HasFired = true;
                _activeTimedCBs[i] = cb;

                try {
                    cb.Action?.Invoke();
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        _lastTime = currentTime;
    }

    /// <summary>
    /// Move any pending callbacks whose hash matches the current state into the active lists.
    /// </summary>
    void ActivatePendingFor(int stateHash) {
        // Timed callbacks
        for (int i = _pendingTimeCBs.Count - 1; i >= 0; i--) {
            var cb = _pendingTimeCBs[i];
            if (cb.Hash == stateHash) {
                _activeTimedCBs.Add(cb);
                _pendingTimeCBs.RemoveAt(i);
            }
        }

        // End callbacks
        for (int i = _pendingEndCBs.Count - 1; i >= 0; i--) {
            var cb = _pendingEndCBs[i];
            if (cb.Hash == stateHash) {
                _activeEndCBs.Add(cb);
                _pendingEndCBs.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Fire all end callbacks for the given state.
    /// </summary>
    void FireEndCallbacksFor(int stateHash) {
        for (int i = 0; i < _activeEndCBs.Count; i++) {
            var cb = _activeEndCBs[i];
            if (cb.Hash != stateHash || cb.HasFired)
                continue;

            cb.HasFired = true;
            _activeEndCBs[i] = cb;

            try {
                cb.Action?.Invoke();
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
    }

    struct TimedAnimCB {
        public int Hash;
        public Action Action;
        public float Time;
        public bool HasFired;
    }
}
