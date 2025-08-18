using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Collection of dialogue nodes that forms a dialogue tree/graph.
/// </summary>
[CreateAssetMenu(menuName = "Dialogue/Simple Graph")]
public class DialogueGraph : ScriptableObject
{
    public List<DialogueNode> nodes = new List<DialogueNode>();

    /// <summary>
    /// Finds a node by its identifier.
    /// </summary>
    public DialogueNode GetNode(string id)
    {
        return nodes.Find(n => n != null && n.nodeId == id);
    }
}

