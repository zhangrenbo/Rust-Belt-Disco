using UnityEngine;

/// <summary>
/// Runtime component that drives a <see cref="DialogueGraph"/>.
/// This class focuses on the core logic of conditions and skill checks
/// and leaves UI representation to external scripts.
/// </summary>
public class SimpleDialogueManager : MonoBehaviour
{
    [Tooltip("Dialogue graph asset containing all nodes.")]
    public DialogueGraph graph;

    private DialogueNode currentNode;

    /// <summary>
    /// Start a dialogue at the given node id.
    /// </summary>
    public void StartDialogue(string startNodeId)
    {
        if (graph == null)
        {
            Debug.LogError("[SimpleDialogueManager] No dialogue graph assigned.");
            return;
        }

        currentNode = graph.GetNode(startNodeId);
        if (currentNode == null)
        {
            Debug.LogError($"[SimpleDialogueManager] Node {startNodeId} not found.");
            return;
        }

        PresentNode();
    }

    /// <summary>
    /// Presents the current node. This example implementation logs to the console.
    /// A real game would connect this to UI elements.
    /// </summary>
    private void PresentNode()
    {
        if (currentNode == null)
            return;

        Debug.Log($"{currentNode.speaker}: {currentNode.text}");
        for (int i = 0; i < currentNode.options.Count; i++)
        {
            var opt = currentNode.options[i];
            if (opt.AreConditionsMet())
                Debug.Log($"Option {i}: {opt.optionText}");
        }
    }

    /// <summary>
    /// Choose an option from the current node.
    /// </summary>
    /// <param name="index">Index of the option.</param>
    /// <param name="skillValue">Player's skill value used for skill checks.</param>
    public void ChooseOption(int index, int skillValue = 0)
    {
        if (currentNode == null)
        {
            Debug.LogWarning("[SimpleDialogueManager] No active dialogue.");
            return;
        }

        if (index < 0 || index >= currentNode.options.Count)
        {
            Debug.LogWarning("[SimpleDialogueManager] Invalid option index.");
            return;
        }

        var option = currentNode.options[index];
        if (!option.AreConditionsMet())
        {
            Debug.Log("[SimpleDialogueManager] Option conditions not met.");
            return;
        }

        if (option.skillCheck != null)
        {
            bool success = option.skillCheck.Roll(skillValue, out int roll);
            Debug.Log($"[SimpleDialogueManager] Rolled {roll} against difficulty {option.skillCheck.difficulty} => {(success ? "Success" : "Fail")}");
            string targetId = success ? option.nextNodeId : option.failNodeId;
            currentNode = graph.GetNode(targetId);
        }
        else
        {
            currentNode = graph.GetNode(option.nextNodeId);
        }

        if (currentNode == null)
        {
            Debug.Log("[SimpleDialogueManager] Dialogue ended.");
        }
        else
        {
            PresentNode();
        }
    }
}

