using UnityEngine;

/// <summary>
/// 对话系统工具类
/// </summary>
public static class DialogueSystemUtils
{
    /// <summary>
    /// 显示快速对话
    /// </summary>
    public static void ShowQuickDialogue(string text, float duration = 3f)
    {
        if (GlobalDialogueManager.Instance != null)
        {
            GlobalDialogueManager.Instance.StartDialogue(text);
        }
        else
        {
            Debug.LogWarning("[DialogueSystemUtils] 对话管理器不存在");
        }
    }

    /// <summary>
    /// 检查对话系统是否准备就绪
    /// </summary>
    public static bool IsDialogueSystemReady()
    {
        return GlobalDialogueManager.Instance != null;
    }

    /// <summary>
    /// 查找所有NPC对话控制器
    /// </summary>
    public static NPCDialogueController[] FindAllNPCDialogueControllers()
    {
        return UnityEngine.Object.FindObjectsOfType<NPCDialogueController>();
    }

    /// <summary>
    /// 重置所有NPC对话状态
    /// </summary>
    public static void ResetAllNPCDialogueStates()
    {
        var npcs = FindAllNPCDialogueControllers();
        foreach (var npc in npcs)
        {
            if (npc != null)
                npc.ResetDialogueState();
        }

        Debug.Log($"[DialogueSystemUtils] 已重置 {npcs.Length} 个NPC的对话状态");
    }

    /// <summary>
    /// 根据名称查找NPC
    /// </summary>
    public static NPCDialogueController FindNPCByName(string npcName)
    {
        var allNPCs = FindAllNPCDialogueControllers();
        foreach (var npc in allNPCs)
        {
            if (npc != null && npc.gameObject.name == npcName)
                return npc;
        }
        return null;
    }

    /// <summary>
    /// 触发指定NPC的对话
    /// </summary>
    public static bool TriggerNPCDialogue(string npcName)
    {
        var npc = FindNPCByName(npcName);
        if (npc != null && npc.CanTriggerDialogue())
        {
            npc.ManualTrigger();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 强制结束当前对话
    /// </summary>
    public static void ForceEndDialogue()
    {
        if (GlobalDialogueManager.Instance != null)
        {
            GlobalDialogueManager.Instance.SkipDialogue();
        }
    }

    /// <summary>
    /// 检查是否正在对话中
    /// </summary>
    public static bool IsInDialogue()
    {
        if (GlobalDialogueManager.Instance != null)
            return GlobalDialogueManager.Instance.IsInDialogue();
        return false;
    }

    /// <summary>
    /// 获取场景中最近的NPC
    /// </summary>
    public static NPCDialogueController GetNearestNPC(Vector3 position, float maxDistance = float.MaxValue)
    {
        var allNPCs = FindAllNPCDialogueControllers();
        NPCDialogueController nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var npc in allNPCs)
        {
            if (npc == null) continue;

            float distance = Vector3.Distance(position, npc.transform.position);
            if (distance < nearestDistance && distance <= maxDistance)
            {
                nearestDistance = distance;
                nearest = npc;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 获取指定范围内的所有NPC
    /// </summary>
    public static NPCDialogueController[] GetNPCsInRange(Vector3 position, float range)
    {
        var allNPCs = FindAllNPCDialogueControllers();
        var npcsInRange = new System.Collections.Generic.List<NPCDialogueController>();

        foreach (var npc in allNPCs)
        {
            if (npc != null && Vector3.Distance(position, npc.transform.position) <= range)
            {
                npcsInRange.Add(npc);
            }
        }

        return npcsInRange.ToArray();
    }

    /// <summary>
    /// 批量设置NPC对话内容
    /// </summary>
    public static void SetNPCDialogueContent(string npcName, TextAsset inkFile, string startKnot = "")
    {
        var npc = FindNPCByName(npcName);
        if (npc != null)
        {
            npc.SetDialogueContent(inkFile, startKnot);
            Debug.Log($"[DialogueSystemUtils] 已为 {npcName} 设置新的对话内容");
        }
        else
        {
            Debug.LogWarning($"[DialogueSystemUtils] 未找到名为 {npcName} 的NPC");
        }
    }

    /// <summary>
    /// 批量设置NPC简单对话
    /// </summary>
    public static void SetNPCSimpleDialogue(string npcName, string dialogueText)
    {
        var npc = FindNPCByName(npcName);
        if (npc != null)
        {
            npc.SetSimpleDialogue(dialogueText);
            Debug.Log($"[DialogueSystemUtils] 已为 {npcName} 设置简单对话");
        }
        else
        {
            Debug.LogWarning($"[DialogueSystemUtils] 未找到名为 {npcName} 的NPC");
        }
    }

    /// <summary>
    /// 打印对话系统状态
    /// </summary>
    public static void PrintDialogueSystemStatus()
    {
        Debug.Log("=== 对话系统状态 ===");
        Debug.Log($"GlobalDialogueManager: {(GlobalDialogueManager.Instance != null ? "✓ 已加载" : "✗ 未找到")}");
        Debug.Log($"当前是否在对话中: {IsInDialogue()}");

        var npcs = FindAllNPCDialogueControllers();
        Debug.Log($"场景中NPC对话控制器数量: {npcs.Length}");

        foreach (var npc in npcs)
        {
            if (npc != null)
            {
                Debug.Log($"  - {npc.gameObject.name}: 可触发={npc.CanTriggerDialogue()}");
            }
        }
    }
}

/// <summary>
/// 对话系统调试器 - 提供调试和测试功能
/// </summary>
public class DialogueSystemDebugger : MonoBehaviour
{
    [Header("=== 调试设置 ===")]
    public bool enableDebugMode = false;
    public KeyCode debugKey = KeyCode.F1;
    public bool showDebugInfo = true;

    [Header("=== 测试对话 ===")]
    public string testDialogueText = "这是一个测试对话";
    public TextAsset testINKFile;

    void Update()
    {
        if (enableDebugMode && Input.GetKeyDown(debugKey))
        {
            ShowDebugMenu();
        }
    }

    void ShowDebugMenu()
    {
        DialogueSystemUtils.PrintDialogueSystemStatus();
    }

    [ContextMenu("测试快速对话")]
    public void TestQuickDialogue()
    {
        DialogueSystemUtils.ShowQuickDialogue(testDialogueText);
    }

    [ContextMenu("测试INK对话")]
    public void TestINKDialogue()
    {
        if (testINKFile != null && GlobalDialogueManager.Instance != null)
        {
            GlobalDialogueManager.Instance.StartDialogue(testINKFile);
        }
        else
        {
            Debug.LogWarning("测试INK文件未设置或对话管理器不存在");
        }
    }

    [ContextMenu("重置所有对话状态")]
    public void ResetAllDialogues()
    {
        DialogueSystemUtils.ResetAllNPCDialogueStates();
    }

    [ContextMenu("强制结束对话")]
    public void ForceEndDialogue()
    {
        DialogueSystemUtils.ForceEndDialogue();
    }

    [ContextMenu("查找最近的NPC")]
    public void FindNearestNPC()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            var nearestNPC = DialogueSystemUtils.GetNearestNPC(player.transform.position, 10f);
            if (nearestNPC != null)
            {
                Debug.Log($"最近的NPC: {nearestNPC.gameObject.name}, 距离: {Vector3.Distance(player.transform.position, nearestNPC.transform.position):F2}m");
            }
            else
            {
                Debug.Log("10米范围内没有找到NPC");
            }
        }
    }
}