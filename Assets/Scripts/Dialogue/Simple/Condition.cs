using UnityEngine;

/// <summary>
/// Represents a simple condition that must be satisfied based on a
/// value stored in <see cref="GlobalStateManager"/>.
/// </summary>
[System.Serializable]
public class Condition
{
    public string variableName;
    public Comparison comparison = Comparison.Equal;
    public int requiredValue;

    public enum Comparison
    {
        Equal,
        NotEqual,
        GreaterOrEqual,
        LessOrEqual
    }

    /// <summary>
    /// Checks whether this condition is satisfied.
    /// </summary>
    public bool IsMet()
    {
        var current = GlobalStateManager.Instance.GetInt(variableName);
        switch (comparison)
        {
            case Comparison.Equal:
                return current == requiredValue;
            case Comparison.NotEqual:
                return current != requiredValue;
            case Comparison.GreaterOrEqual:
                return current >= requiredValue;
            case Comparison.LessOrEqual:
                return current <= requiredValue;
            default:
                return false;
        }
    }
}

