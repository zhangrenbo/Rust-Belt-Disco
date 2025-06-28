using UnityEngine;

/// <summary>
/// 对话脚本基础类 - 所有对话脚本都需要继承这个类
/// </summary>
public abstract class BaseDialogueScript : MonoBehaviour
{
    [Header("=== 对话基础设置 ===")]
    public string dialogueId = "";
    public bool canRepeat = true;
    public int maxTriggerCount = -1; // -1表示无限制
    public float cooldownTime = 0f;

    protected int triggerCount = 0;
    protected float lastTriggerTime = 0f;

    /// <summary>
    /// 检查是否可以触发对话
    /// </summary>
    public virtual bool CanTrigger()
    {
        if (!canRepeat && triggerCount > 0)
            return false;

        if (maxTriggerCount > 0 && triggerCount >= maxTriggerCount)
            return false;

        if (Time.time - lastTriggerTime < cooldownTime)
            return false;

        return true;
    }

    /// <summary>
    /// 开始对话 - 子类必须实现
    /// </summary>
    public abstract void StartDialogue(InteractiveDialogueTrigger trigger);

    /// <summary>
    /// 标记为已触发
    /// </summary>
    protected void MarkAsTriggered()
    {
        triggerCount++;
        lastTriggerTime = Time.time;
    }

    /// <summary>
    /// 重置对话状态
    /// </summary>
    public virtual void ResetDialogueState()
    {
        triggerCount = 0;
        lastTriggerTime = 0f;
    }
}

/// <summary>
/// 交互式对话触发器 - 处理玩家与NPC的对话交互
/// </summary>
public class InteractiveDialogueTrigger : MonoBehaviour
{
    [Header("=== 交互设置 ===")]
    public KeyCode interactKey = KeyCode.E;
    public float interactionRange = 3f;
    public LayerMask playerLayer = -1;
    public string promptText = "按 E 对话";

    [Header("=== 对话脚本 ===")]
    public BaseDialogueScript[] dialogueScripts;
    public bool executeInOrder = false;
    public bool repeatLastScript = true;

    [Header("=== UI设置 ===")]
    public GameObject interactionPrompt;

    // 状态变量
    private Transform player;
    private bool playerInRange = false;
    private int currentScriptIndex = 0;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    void Update()
    {
        CheckPlayerDistance();
        HandleInteraction();
    }

    void CheckPlayerDistance()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool inRange = distance <= interactionRange;

        if (inRange != playerInRange)
        {
            playerInRange = inRange;

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(inRange && CanTriggerAnyDialogue());
            }
        }
    }

    void HandleInteraction()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            TriggerDialogue();
        }
    }

    /// <summary>
    /// 触发对话
    /// </summary>
    public void TriggerDialogue()
    {
        if (!CanTriggerAnyDialogue()) return;

        BaseDialogueScript scriptToExecute = GetNextDialogueScript();
        if (scriptToExecute != null && scriptToExecute.CanTrigger())
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }

            scriptToExecute.StartDialogue(this);
        }
    }

    /// <summary>
    /// 获取下一个要执行的对话脚本
    /// </summary>
    BaseDialogueScript GetNextDialogueScript()
    {
        if (dialogueScripts == null || dialogueScripts.Length == 0)
            return null;

        if (executeInOrder)
        {
            if (currentScriptIndex < dialogueScripts.Length)
            {
                var script = dialogueScripts[currentScriptIndex];
                if (script != null && script.CanTrigger())
                {
                    currentScriptIndex++;
                    return script;
                }
            }

            if (repeatLastScript && currentScriptIndex > 0)
            {
                return dialogueScripts[currentScriptIndex - 1];
            }
        }
        else
        {
            // 随机选择可用的脚本
            for (int i = 0; i < dialogueScripts.Length; i++)
            {
                var script = dialogueScripts[i];
                if (script != null && script.CanTrigger())
                {
                    return script;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 检查是否有任何对话可以触发
    /// </summary>
    bool CanTriggerAnyDialogue()
    {
        if (dialogueScripts == null) return false;

        foreach (var script in dialogueScripts)
        {
            if (script != null && script.CanTrigger())
                return true;
        }

        return false;
    }

    /// <summary>
    /// 对话完成回调
    /// </summary>
    public void OnDialogueComplete()
    {
        if (playerInRange && CanTriggerAnyDialogue())
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 手动触发
    /// </summary>
    public void ManualTrigger()
    {
        TriggerDialogue();
    }

    void OnDrawGizmosSelected()
    {
        // 显示交互范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}