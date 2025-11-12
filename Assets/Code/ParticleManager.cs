using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Video;

public class ParticleManager : MonoBehaviour {
    static ParticleManager t;
    public static ParticleManager T => t;
    [SerializeField]
    List<GameObject> _hitEffects;
    [SerializeField]
    List<GameObject> _objectHitEffects;
    [SerializeField]
    List<GameObject> _stunEffects;
    public void MakeHitAtSpot(Vector3 pos) {
        var i = Random.Range(0, _hitEffects.Count);
        Instantiate(_hitEffects[i], pos, Quaternion.identity);
    }
    public void MakeStunAtSpot(Vector3 pos) {
        var i = Random.Range(0, _stunEffects.Count);
        Instantiate(_hitEffects[i], pos, Quaternion.identity);
    }
    public void MakeObjectHitAtSpot(Vector3 pos) {
        var i = Random.Range(0, _objectHitEffects.Count);
        Instantiate(_hitEffects[i], pos, Quaternion.identity);
    }
    [SerializeField]
    void Awake() {
        if (t != null)
        {
            Destroy(this);
            Debug.LogWarning($"Multiple Particle Managers, one on {name}");
            return;
        }
        t = this;
    }
}
