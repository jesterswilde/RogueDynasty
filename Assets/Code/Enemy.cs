using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Character))]
public class Enemy : MonoBehaviour
{
    [SerializeField]
    Transform _eyePos;
    Squad _squad;
    Coroutine scanCo;
    public void StartScanForEnemy()=>
        scanCo = StartCoroutine(ScanForEnemies());
    IEnumerator ScanForEnemies(){
        while (true)
        {
            var scanInterval = GameManager.T.Config.EnemyScanInterval;
            var scanMask = GameManager.T.Config.EnemyScanMask;
            float waitTime = UnityEngine.Random.Range(scanInterval.x, scanInterval.y);
            yield return new WaitForSeconds(waitTime);
            bool didSee = HasUnobstructedView(_eyePos.position, GameManager.T.Player.gameObject, scanMask);
            if (didSee)
                _squad.SawEnemy();
        }
    }
    public void StopScanForEnemy() {
        if(scanCo != null)
            StopCoroutine(scanCo);
    }
    public void RegisterWithSquad()
    {
        _squad = GetComponentInParent<Squad>();
        if (_squad == null)
            throw new Exception($"{name} was not made as part of a squad!!!!!!");

        _squad.RegisterUnit(this);
    }
    bool HasUnobstructedView(Vector3 eye, GameObject target, LayerMask mask)
    {
        Collider col = target.GetComponent<Collider>();
        if (col == null) {
            Debug.LogWarning("Target has no collider!");
            return false;
        }

        Vector3 randomPoint = new(
            UnityEngine.Random.Range(col.bounds.min.x, col.bounds.max.x),
            UnityEngine.Random.Range(col.bounds.min.y, col.bounds.max.y),
            UnityEngine.Random.Range(col.bounds.min.z, col.bounds.max.z)
        );
        Vector3 dir = randomPoint - eye;
        float distance = dir.magnitude;

        if (Physics.Raycast(eye, dir.normalized, out RaycastHit hit, distance, mask)) {
            return hit.collider.gameObject == target;
        }
        return true;
    }
}
