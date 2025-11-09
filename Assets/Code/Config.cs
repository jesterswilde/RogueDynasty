using UnityEngine;

[CreateAssetMenu(fileName = "config", menuName = "RogueDynasty/Config")]
public class Config : ScriptableObject
{
    public Vector2 EnemyScanInterval;
    public LayerMask EnemyScanMask;
}