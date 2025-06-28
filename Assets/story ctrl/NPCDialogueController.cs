using UnityEngine;
using System.Collections;

/// <summary>
/// NPC对话控制器 - 挂载在每个NPC上
/// </summary>
public class NPCDialogueController : MonoBehaviour
{
    [Header("=== 对话内容设置 ===")]
    [Tooltip("对话使用的INK文件（优先级最高）")]
    public TextAsset inkDialogueFile;
    [Tooltip("INK对话的起始节点（可选，留空从开头开始）")]
    public string startKnotName = "";
    [Tooltip("如果没有INK文件，使用此简单文本对话")]
    [TextArea(3, 5)]
    public string simpleDialogueText = "你好，冒险者！";

    [Header("=== 交互检测设置 ===")]
    [Tooltip("触发对话的距离")]
    public float interactionRange = 3f;
    [Tooltip("触发对话的按键")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("玩家层级遮罩")]
    public LayerMask playerLayer = -1;

    [Header("=== 交互限制设置 ===")]
    [Tooltip("是否可以重复对话")]
    public bool canRepeatDialogue = true;
    [Tooltip("对话冷却时间（秒）")]
    public float cooldownTime = 0f;
    [Tooltip("最大触发次数（-1为无限）")]
    public int maxTriggerCount = -1;
    [Tooltip("玩家必须面向NPC才能交互")]
    public bool requirePlayerFacing = false;
    [Tooltip("面向角度容差（度）")]
    public float facingAngleTolerance = 45f;

    [Header("=== 交互提示UI ===")]
    [Tooltip("交互提示UI对象（可以直接拖入子物体）")]
    public GameObject interactionPrompt;
    [Tooltip("提示文本格式，{0}会被替换为按键名")]
    public string promptText = "按 {0} 对话";
    [Tooltip("提示UI自动创建设置")]
    public bool autoCreatePromptUI = true;
    [Tooltip("自动创建的提示UI的垂直偏移")]
    public float promptVerticalOffset = 2f;
    [Tooltip("提示UI字体大小")]
    public int promptFontSize = 14;
    [Tooltip("提示UI背景颜色")]
    public Color promptBackgroundColor = new Color(0, 0, 0, 0.7f);
    [Tooltip("提示UI文字颜色")]
    public Color promptTextColor = Color.white;

    [Header("=== 状态保存设置 ===")]
    public bool saveDialogueState = true;
    public string dialogueStateKey = "";

    // 私有状态
    private Transform player;
    private bool playerInRange = false;
    private bool isOnCooldown = false;
    private float lastTriggerTime = 0f;
    private int triggerCount = 0;
    private UnityEngine.UI.Text promptTextComponent;

    // 静态管理，确保只有最近的NPC显示提示
    private static NPCDialogueController currentInteractableNPC = null;
    private static float currentClosestDistance = float.MaxValue;

    void Start()
    {
        InitializeNPC();
    }

    void Update()
    {
        CheckPlayerDistance();
        HandleInteraction();
        UpdateCooldown();
    }

    void InitializeNPC()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // 初始化或创建交互提示UI
        SetupInteractionPrompt();

        if (string.IsNullOrEmpty(dialogueStateKey))
        {
            dialogueStateKey = $"npc_dialogue_{gameObject.name}_{gameObject.GetInstanceID()}";
        }
    }

    void SetupInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            // 如果已经设置了提示UI，直接配置文本
            promptTextComponent = interactionPrompt.GetComponentInChildren<UnityEngine.UI.Text>();
            if (promptTextComponent != null)
            {
                promptTextComponent.text = string.Format(promptText, interactKey.ToString());
            }
            interactionPrompt.SetActive(false);
        }
        else if (autoCreatePromptUI)
        {
            // 自动创建提示UI
            CreateInteractionPromptUI();
        }
    }

    void CreateInteractionPromptUI()
    {
        // 检查是否已有自动创建的提示UI
        Transform existingPrompt = transform.Find("InteractionPrompt_Auto");
        if (existingPrompt != null)
        {
            interactionPrompt = existingPrompt.gameObject;
            promptTextComponent = interactionPrompt.GetComponentInChildren<UnityEngine.UI.Text>();
            if (promptTextComponent != null)
            {
                promptTextComponent.text = string.Format(promptText, interactKey.ToString());
            }
            interactionPrompt.SetActive(false);
            return;
        }

        // 创建Canvas
        GameObject canvasObj = new GameObject("InteractionPrompt_Auto");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.up * promptVerticalOffset;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(3, 0.8f);

        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.dynamicPixelsPerUnit = 100;

        // 添加GraphicRaycaster以支持鼠标交互（虽然这个UI不需要交互）
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 创建背景
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform);

        var bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = promptBackgroundColor;

        RectTransform bgRect = bgImage.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // 创建Text对象
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(canvasObj.transform);

        var text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = string.Format(promptText, interactKey.ToString());
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = promptFontSize;
        text.color = promptTextColor;
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        canvasObj.SetActive(false);
        interactionPrompt = canvasObj;
        promptTextComponent = text;

        Debug.Log($"[NPCDialogueController] 为 {gameObject.name} 自动创建了交互提示UI");
    }

    void CheckPlayerDistance()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool inRange = distance <= interactionRange;

        if (inRange)
        {
            if (distance < currentClosestDistance || currentInteractableNPC == null)
            {
                if (currentInteractableNPC != null && currentInteractableNPC != this)
                {
                    currentInteractableNPC.HidePrompt();
                }

                currentInteractableNPC = this;
                currentClosestDistance = distance;
                ShowPrompt();
            }
            playerInRange = true;
        }
        else
        {
            if (playerInRange)
            {
                HidePrompt();
                if (currentInteractableNPC == this)
                {
                    currentInteractableNPC = null;
                    currentClosestDistance = float.MaxValue;
                    FindNextClosestNPC();
                }
            }
            playerInRange = false;
        }
    }

    void FindNextClosestNPC()
    {
        NPCDialogueController[] allNPCs = FindObjectsOfType<NPCDialogueController>();
        NPCDialogueController nextClosest = null;
        float nextClosestDistance = float.MaxValue;

        foreach (var npc in allNPCs)
        {
            if (npc == this || npc.player == null) continue;

            float distance = Vector3.Distance(npc.transform.position, npc.player.position);
            if (distance <= npc.interactionRange && distance < nextClosestDistance && npc.CanTriggerDialogue())
            {
                nextClosest = npc;
                nextClosestDistance = distance;
            }
        }

        if (nextClosest != null)
        {
            currentInteractableNPC = nextClosest;
            currentClosestDistance = nextClosestDistance;
            nextClosest.ShowPrompt();
        }
    }

    void ShowPrompt()
    {
        if (interactionPrompt != null && CanTriggerDialogue())
        {
            interactionPrompt.SetActive(true);
        }
    }

    void HidePrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    void HandleInteraction()
    {
        if (currentInteractableNPC != this) return;

        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            TriggerDialogue();
        }
    }

    public void TriggerDialogue()
    {
        if (!CanTriggerDialogue()) return;

        if (GlobalDialogueManager.Instance == null)
        {
            Debug.LogError("[NPCDialogueController] 未找到全局对话管理器！");
            return;
        }

        if (GlobalDialogueManager.Instance.IsInDialogue())
        {
            return;
        }

        if (requirePlayerFacing && !IsPlayerFacingNPC())
        {
            return;
        }

        HidePrompt();

        if (inkDialogueFile != null)
        {
            if (saveDialogueState)
            {
                LoadDialogueState();
            }
            GlobalDialogueManager.Instance.StartDialogue(inkDialogueFile, startKnotName, this);
        }
        else if (!string.IsNullOrEmpty(simpleDialogueText))
        {
            GlobalDialogueManager.Instance.StartDialogue(simpleDialogueText);
        }

        triggerCount++;
        lastTriggerTime = Time.time;
        isOnCooldown = cooldownTime > 0;
    }

    public bool CanTriggerDialogue()
    {
        if (!canRepeatDialogue && triggerCount > 0)
            return false;

        if (maxTriggerCount > 0 && triggerCount >= maxTriggerCount)
            return false;

        if (isOnCooldown)
            return false;

        if (inkDialogueFile == null && string.IsNullOrEmpty(simpleDialogueText))
            return false;

        return true;
    }

    bool IsPlayerFacingNPC()
    {
        if (player == null) return true;

        Vector3 playerToNPC = (transform.position - player.position).normalized;
        Vector3 playerForward = player.forward;
        float angle = Vector3.Angle(playerForward, playerToNPC);
        return angle <= facingAngleTolerance;
    }

    void UpdateCooldown()
    {
        if (isOnCooldown && Time.time - lastTriggerTime >= cooldownTime)
        {
            isOnCooldown = false;
        }
    }

    public void OnDialogueComplete()
    {
        if (saveDialogueState)
        {
            SaveDialogueState();
        }

        if (playerInRange && CanTriggerDialogue() && currentInteractableNPC == this)
        {
            ShowPrompt();
        }
    }

    void SaveDialogueState()
    {
        PlayerPrefs.SetInt($"{dialogueStateKey}_trigger_count", triggerCount);
        PlayerPrefs.SetFloat($"{dialogueStateKey}_last_trigger", lastTriggerTime);
        PlayerPrefs.Save();
    }

    void LoadDialogueState()
    {
        if (PlayerPrefs.HasKey($"{dialogueStateKey}_trigger_count"))
        {
            triggerCount = PlayerPrefs.GetInt($"{dialogueStateKey}_trigger_count");
            lastTriggerTime = PlayerPrefs.GetFloat($"{dialogueStateKey}_last_trigger");
        }
    }

    [ContextMenu("重置对话状态")]
    public void ResetDialogueState()
    {
        triggerCount = 0;
        lastTriggerTime = 0f;
        isOnCooldown = false;

        if (PlayerPrefs.HasKey($"{dialogueStateKey}_trigger_count"))
        {
            PlayerPrefs.DeleteKey($"{dialogueStateKey}_trigger_count");
            PlayerPrefs.DeleteKey($"{dialogueStateKey}_last_trigger");
        }
    }

    public void ManualTrigger()
    {
        TriggerDialogue();
    }

    /// <summary>
    /// 设置新的对话内容
    /// </summary>
    public void SetDialogueContent(TextAsset newINKFile, string newStartKnot = "")
    {
        inkDialogueFile = newINKFile;
        startKnotName = newStartKnot;
    }

    /// <summary>
    /// 设置简单对话文本
    /// </summary>
    public void SetSimpleDialogue(string newText)
    {
        simpleDialogueText = newText;
    }

    /// <summary>
    /// 动态设置交互距离
    /// </summary>
    public void SetInteractionRange(float newRange)
    {
        interactionRange = Mathf.Max(0.1f, newRange);
    }

    /// <summary>
    /// 动态设置交互按键
    /// </summary>
    public void SetInteractKey(KeyCode newKey)
    {
        interactKey = newKey;
        // 更新提示文本
        if (promptTextComponent != null)
        {
            promptTextComponent.text = string.Format(promptText, interactKey.ToString());
        }
    }

    /// <summary>
    /// 动态设置提示文本
    /// </summary>
    public void SetPromptText(string newPromptText)
    {
        promptText = newPromptText;
        if (promptTextComponent != null)
        {
            promptTextComponent.text = string.Format(promptText, interactKey.ToString());
        }
    }

    /// <summary>
    /// 动态设置交互提示UI
    /// </summary>
    public void SetInteractionPrompt(GameObject newPrompt)
    {
        if (interactionPrompt != null && interactionPrompt.name == "InteractionPrompt_Auto")
        {
            // 如果当前使用的是自动创建的UI，先销毁它
            DestroyImmediate(interactionPrompt);
        }

        interactionPrompt = newPrompt;
        if (interactionPrompt != null)
        {
            promptTextComponent = interactionPrompt.GetComponentInChildren<UnityEngine.UI.Text>();
            if (promptTextComponent != null)
            {
                promptTextComponent.text = string.Format(promptText, interactKey.ToString());
            }
            interactionPrompt.SetActive(false);
        }
    }

    /// <summary>
    /// 强制重新创建交互提示UI
    /// </summary>
    [ContextMenu("重新创建提示UI")]
    public void RecreateInteractionPrompt()
    {
        if (interactionPrompt != null && interactionPrompt.name == "InteractionPrompt_Auto")
        {
            DestroyImmediate(interactionPrompt);
            interactionPrompt = null;
            promptTextComponent = null;
        }

        if (autoCreatePromptUI)
        {
            CreateInteractionPromptUI();
        }
    }

    void OnDrawGizmosSelected()
    {
        // 绘制交互范围
        Gizmos.color = playerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        // 绘制到玩家的连线
        if (player != null && playerInRange)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // 绘制面向要求的角度范围
        if (requirePlayerFacing && player != null)
        {
            Gizmos.color = Color.red;
            Vector3 playerToNPC = (transform.position - player.position).normalized;
            Vector3 leftBound = Quaternion.Euler(0, -facingAngleTolerance, 0) * playerToNPC;
            Vector3 rightBound = Quaternion.Euler(0, facingAngleTolerance, 0) * playerToNPC;

            Gizmos.DrawLine(player.position, player.position + leftBound * interactionRange);
            Gizmos.DrawLine(player.position, player.position + rightBound * interactionRange);
        }

        // 绘制提示UI位置
        if (autoCreatePromptUI || interactionPrompt != null)
        {
            Gizmos.color = Color.white;
            Vector3 promptPos = transform.position + Vector3.up * promptVerticalOffset;
            Gizmos.DrawWireCube(promptPos, Vector3.one * 0.2f);
            Gizmos.DrawLine(transform.position, promptPos);
        }

#if UNITY_EDITOR
        // 显示状态信息
        UnityEditor.Handles.color = Color.white;
        string statusText = $"对话状态:\n";
        statusText += $"可触发: {CanTriggerDialogue()}\n";
        statusText += $"触发次数: {triggerCount}";
        if (maxTriggerCount > 0)
            statusText += $"/{maxTriggerCount}";
        statusText += $"\n冷却中: {isOnCooldown}";

        UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, statusText);
#endif
    }

    void OnDrawGizmos()
    {
        // 始终显示交互范围的半透明球体
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, interactionRange);

        // 显示对话内容类型
        Gizmos.color = inkDialogueFile != null ? Color.blue :
                      !string.IsNullOrEmpty(simpleDialogueText) ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 4f, Vector3.one * 0.3f);
    }
}