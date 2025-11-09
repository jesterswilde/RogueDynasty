using System.Collections.Generic;
using UnityEngine;

public class Squad : MonoBehaviour
{
    bool _seesEnemy;
    HashSet<Enemy> _squadMembers = new();
 
    internal void RegisterUnit(Enemy squadMember)=>
        _squadMembers.Add(squadMember);
    internal void UnitDied(Enemy squadMember) {
        _squadMembers.Remove(squadMember);
    }

    internal void SawEnemy() {
        _seesEnemy = true;
        foreach(var e in _squadMembers) {
            e.StopScanForEnemy();
        }
    }
    internal void LostEnemy()
    {

    }
}
