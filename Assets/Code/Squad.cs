using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Squad : MonoBehaviour
{
    bool _seesEnemy;
    HashSet<Enemy> _squadMembers = new();
    public HashSet<Enemy> SquadMembers => _squadMembers;

 
    internal void RegisterUnit(Enemy squadMember) {
        _squadMembers.Add(squadMember);
        //if (!_seesEnemy)
        //    squadMember.StartScanForEnemy();
    }
    internal void UnitDied(Enemy squadMember) {
        _squadMembers.Remove(squadMember);
    }

    internal void SawEnemy() {
        if (_seesEnemy)
            return;
        Debug.Log("Saw Player");
        _seesEnemy = true;
        foreach(var e in _squadMembers) {
            e.StopScanForEnemy();
            e.StartSwarmingPlayer();
        }
    }
    internal void LostEnemy()
    {

    }
}
