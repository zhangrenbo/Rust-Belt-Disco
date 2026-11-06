using UnityEngine;

/// <summary>
/// Represents a selectable option in a dialogue node.
/// It can have conditions and an optional skill check.
/// </summary>
[System.Serializable]
public class DialogueOption
{
    [Tooltip("Text shown to the player for this option.")]
    public string optionText;
    [Tooltip("The node to jump to when this option is chosen and succeeds.")]
    public string nextNodeId;
    [Tooltip("Optional node to jump to if a skill check fails.")]
    public string failNodeId;

    [Tooltip("Conditions that must be met for this option to be available.")]
    public Condition[] conditions;

    [Tooltip("Optional skill check that determines success or failure.")]
    public SkillCheck skillCheck;

    /// <summary>
    /// Returns true if all conditions are satisfied.
    /// </summary>
    public bool AreConditionsMet()
    {
        if (conditions == null || conditions.Length == 0)
            return true;

        foreach (var c in conditions)
        {
            if (!c.IsMet())
                return false;
        }
        return true;
    }
}

