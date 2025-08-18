using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds global variables that reflect the player's actions.
/// This is a lightweight alternative to Ink's variable system.
/// </summary>
public class GlobalStateManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance for easy access.
    /// </summary>
    public static GlobalStateManager Instance { get; private set; }

    /// <summary>
    /// Internal storage for integer variables. Additional types can be
    /// added as needed.
    /// </summary>
    private readonly Dictionary<string, int> intVariables = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Set an integer variable in the global state.
    /// </summary>
    public void SetInt(string key, int value)
    {
        intVariables[key] = value;
    }

    /// <summary>
    /// Get an integer variable. Returns 0 if the key does not exist.
    /// </summary>
    public int GetInt(string key)
    {
        return intVariables.TryGetValue(key, out var value) ? value : 0;
    }

    /// <summary>
    /// Increase an integer variable by a delta. Useful for counters.
    /// </summary>
    public void IncrementInt(string key, int delta = 1)
    {
        SetInt(key, GetInt(key) + delta);
    }

    /// <summary>
    /// Convenience method for checking if an integer variable is at least
    /// a required value.
    /// </summary>
    public bool IsIntAtLeast(string key, int required)
    {
        return GetInt(key) >= required;
    }
}

