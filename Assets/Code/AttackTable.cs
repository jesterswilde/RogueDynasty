using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "Table", menuName = "RogueDynasty/AttackTable")]
public class AttackTable : SerializedScriptableObject
{
    [HorizontalGroup("Roll"), LabelText("Roll:"), LabelWidth(50)]
    public int Num;

    [HorizontalGroup("Roll"), LabelText("D"), LabelWidth(20)]
    public int Die;

    public List<AttackTableEntry> Entries;

    // Not serialized so it won't touch your asset data, only runtime behavior.
    [NonSerialized]
    private bool _isSorted;

    /// <summary>
    /// Ensures the Entries list is sorted by DC descending.
    /// This is done lazily the first time we need it at runtime.
    /// </summary>
    private void EnsureSorted()
    {
        if (_isSorted || Entries == null)
            return;
        Entries.Sort((a, b) => b.DC.CompareTo(a.DC)); // highest DC first
        _isSorted = true;
    }

    /// <summary>
    /// Rolls Num d Die (e.g. 2d6) and returns the total.
    /// </summary>
    private int Roll()
    {
        if (Num <= 0 || Die <= 0)
        {
            Debug.LogWarning($"AttackTable '{name}' has invalid dice settings: {Num}d{Die}.");
            return 0;
        }

        int total = 0;
        for (int i = 0; i < Num; i++)
        {
            // Unity's Random.Range with ints is [min, maxExclusive)
            total += UnityEngine.Random.Range(1, Die + 1);
        }

        return total;
    }

    /// <summary>
    /// Makes a dice roll and returns the first entry whose DC is passed (roll >= DC),
    /// evaluating from highest DC to lowest. Returns null if none are passed.
    /// </summary>
    public AttackTableEntry? GetEntry()
    {
        if (Entries == null || Entries.Count == 0)
        {
            Debug.LogWarning($"AttackTable '{name}' has no entries.");
            return null;
        }

        EnsureSorted();

        int roll = Roll();

        foreach (var entry in Entries)
        {
            if (roll >= entry.DC)
            {
                return entry;
            }
        }

        // No entry passed
        return null;
    }
}

[Serializable]
public struct AttackTableEntry
{
    public AttackType AttackType;
    public List<int> Combo;
    public int DC;
    public string Info;
}

public enum AttackType
{
    Combo,
    SpecialMove
}
