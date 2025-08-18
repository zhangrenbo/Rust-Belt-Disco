using UnityEngine;

/// <summary>
/// Handles probability checks similar to Disco Elysium's dice system.
/// The result determines which dialogue branch will be taken.
/// </summary>
[System.Serializable]
public class SkillCheck
{
    public string skillName;
    [Tooltip("Target number that the dice roll plus skill must meet or exceed.")]
    public int difficulty = 10;
    [Tooltip("Number of sides on the dice, defaults to 20 for a d20 roll.")]
    public int diceSides = 20;

    /// <summary>
    /// Perform the dice roll using the player's skill value.
    /// </summary>
    /// <param name="skillValue">The value of the relevant player skill.</param>
    /// <param name="roll">The raw dice roll result.</param>
    /// <returns>True if the check succeeds, false otherwise.</returns>
    public bool Roll(int skillValue, out int roll)
    {
        roll = Random.Range(1, diceSides + 1);
        int total = roll + skillValue;
        return total >= difficulty;
    }
}

