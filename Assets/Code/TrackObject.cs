using UnityEngine;

public class TrackObject : MonoBehaviour {
    [SerializeField]
    Transform _target;

    void Update() {
        if (_target != null)
            transform.position = _target.position;
        else
            Destroy(this);
    }
}