using System;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

public class Character : MonoBehaviour, IHittable
{
    [SerializeField]
    float _speed;
    [SerializeField]
    float _health;
    [SerializeField]
    TeamMask _team;
    float _isDead;
    public void OnHit(Attack attack)
    {
        if ((_team & attack.TeamMask) <= 0)
            return;
        _health -= attack.Damage;
        if (_health <= 0)
            Die();
    }

    private void Die()
    {
        throw new NotImplementedException();
    }

    public void Move(Vector3 dir)
    {

    }
}
