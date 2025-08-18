using UnityEngine;

/// <summary>
/// Adapter that bridges IStoryService to existing StoryCtrl.
/// </summary>
public class StoryServiceAdapter : MonoBehaviour, IStoryService
{
    public void Set(string key, int value)
    {
        if (StoryCtrl.Instance != null)
        {
            StoryCtrl.Instance.Set(key, value);
        }
    }

    public int Get(string key)
    {
        return StoryCtrl.Instance != null ? StoryCtrl.Instance.Get(key) : 0;
    }
}
