using UnityEngine;

public class ZoneSpawner : MonoBehaviour {
    [SerializeField]
    LayerMask _layer = 6;

    [SerializeField]
    GameObject _prefab;
    bool _hasSpawned = false;

    void OnTriggerEnter(Collider other) {
        if (_layer.Contains(other.gameObject)  && !_hasSpawned) {
            Debug.Log($"{name} was entered by {other.gameObject.name}");
            Instantiate(_prefab);
            _hasSpawned = true;
        }
            
    }
}