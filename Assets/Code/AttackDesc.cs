using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "RogueDynasty/Attack")]
public class AttackDesc: ScriptableObject {
    public string Name;
    public string AnimName;
    public float Damage;
    public bool isHoldable;
    public bool endsCombo;
    public bool isLoopable;
    public bool Stuns;
}