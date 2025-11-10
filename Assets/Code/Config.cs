using UnityEngine;

[CreateAssetMenu(fileName = "config", menuName = "RogueDynasty/Config")]
public class Config : ScriptableObject
{
    public Vector2 EnemyScanInterval;
    public LayerMask EnemyScanMask;
    public float DesiredDistance;
    public float RepulseAtDistance;
    public float GroupForce;
    public float RepulseForce;
    public float TargetDrawForce;
}