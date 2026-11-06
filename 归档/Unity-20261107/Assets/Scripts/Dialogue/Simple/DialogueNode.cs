using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single dialogue node containing text and a list of options.
/// </summary>
[CreateAssetMenu(menuName = "Dialogue/Simple Node")]
public class DialogueNode : ScriptableObject
{
    public string nodeId;
    public string speaker;
    [TextArea(3, 5)]
    public string text;

    public List<DialogueOption> options = new List<DialogueOption>();
}

