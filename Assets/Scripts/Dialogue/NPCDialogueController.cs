using UnityEngine;
using System.Collections;

/// <summary>
/// NPC�Ի������� - ������ÿ��NPC��
/// </summary>
public class NPCDialogueController : MonoBehaviour
{
    [Header("=== �Ի��������� ===")]
    [Tooltip("�Ի�ʹ�õ�INK�ļ������ȼ���ߣ�")]
    public TextAsset inkDialogueFile;
    [Tooltip("INK�Ի�����ʼ�ڵ㣨��ѡ����մӿ�ͷ��ʼ��")]
    public string startKnotName = "";
    [Tooltip("���û��INK�ļ���ʹ�ô˼��ı��Ի�")]
    [TextArea(3, 5)]
    public string simpleDialogueText = "��ã�ð���ߣ�";

    [Header("=== ����������� ===")]
    [Tooltip("�����Ի��ľ���")]
    public float interactionRange = 3f;
    [Tooltip("�����Ի��İ���")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("��Ҳ㼶����")]
    public LayerMask playerLayer = -1;

    [Header("=== ������������ ===")]
    [Tooltip("�Ƿ�����ظ��Ի�")]
    public bool canRepeatDialogue = true;
    [Tooltip("�Ի���ȴʱ�䣨�룩")]
    public float cooldownTime = 0f;
    [Tooltip("��󴥷�������-1Ϊ���ޣ�")]
    public int maxTriggerCount = -1;
    [Tooltip("��ұ�������NPC���ܽ���")]
    public bool requirePlayerFacing = false;
    [Tooltip("����Ƕ��ݲ�ȣ�")]
    public float facingAngleTolerance = 45f;

    [Header("=== ������ʾUI ===")]
    [Tooltip("������ʾUI���󣨿���ֱ�����������壩")]
    public GameObject interactionPrompt;
    [Tooltip("��ʾ�ı���ʽ��{0}�ᱻ�滻Ϊ������")]
    public string promptText = "�� {0} �Ի�";
    [Tooltip("��ʾUI�Զ���������")]
    public bool autoCreatePromptUI = true;
    [Tooltip("�Զ���������ʾUI�Ĵ�ֱƫ��")]
    public float promptVerticalOffset = 2f;
    [Tooltip("��ʾUI�����С")]
    public int promptFontSize = 14;
    [Tooltip("��ʾUI������ɫ")]
    public Color promptBackgroundColor = new Color(0, 0, 0, 0.7f);
    [Tooltip("��ʾUI������ɫ")]
    public Color promptTextColor = Color.white;

    [Header("=== ״̬�������� ===")]
    public bool saveDialogueState = true;
    public string dialogueStateKey = "";

    // ˽��״̬
    [SerializeField] private Transform player;
    private bool playerInRange = false;
    private bool isOnCooldown = false;
    private float lastTriggerTime = 0f;
    private int triggerCount = 0;
    private UnityEngine.UI.Text promptTextComponent;

    // ��̬�����ȷ��ֻ�������NPC��ʾ��ʾ
    private static NPCDialogueController currentInteractableNPC = null;
    private static float currentClosestDistance = float.MaxValue;
    private static readonly System.Collections.Generic.List<NPCDialogueController> registeredNPCs = new System.Collections.Generic.List<NPCDialogueController>();

    void Awake()
    {
        registeredNPCs.Add(this);
    }

    void OnDestroy()
    {
        registeredNPCs.Remove(this);
    }

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
        if (player == null)
        {
            Debug.LogWarning($"[NPCDialogueController] Player reference not set for {gameObject.name}");
        }

        // ��ʼ���򴴽�������ʾUI
        SetupInteractionPrompt();

        if (string.IsNullOrEmpty(dialogueStateKey))
        {
            dialogueStateKey = $"npc_dialogue_{gameObject.name}_{gameObject.GetInstanceID()}";
        }
    }

    /// <summary>
    /// ���ð�Ҷ�ο�
    /// </summary>
    public void SetPlayer(Transform p)
    {
        player = p;
    }

    void SetupInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            // ����Ѿ���������ʾUI��ֱ�������ı�
            promptTextComponent = interactionPrompt.GetComponentInChildren<UnityEngine.UI.Text>();
            if (promptTextComponent != null)
            {
                promptTextComponent.text = string.Format(promptText, interactKey.ToString());
            }
            interactionPrompt.SetActive(false);
        }
        else if (autoCreatePromptUI)
        {
            // �Զ�������ʾUI
            CreateInteractionPromptUI();
        }
    }

    void CreateInteractionPromptUI()
    {
        // ����Ƿ������Զ���������ʾUI
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

        // ����Canvas
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

        // ���GraphicRaycaster��֧����꽻������Ȼ���UI����Ҫ������
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // ��������
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform);

        var bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = promptBackgroundColor;

        RectTransform bgRect = bgImage.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // ����Text����
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

        Debug.Log($"[NPCDialogueController] Ϊ {gameObject.name} �Զ������˽�����ʾUI");
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
        NPCDialogueController nextClosest = null;
        float nextClosestDistance = float.MaxValue;

        foreach (var npc in registeredNPCs)
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
            Debug.LogError("[NPCDialogueController] δ�ҵ�ȫ�ֶԻ���������");
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

    [ContextMenu("���öԻ�״̬")]
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
    /// �����µĶԻ�����
    /// </summary>
    public void SetDialogueContent(TextAsset newINKFile, string newStartKnot = "")
    {
        inkDialogueFile = newINKFile;
        startKnotName = newStartKnot;
    }

    /// <summary>
    /// ���ü򵥶Ի��ı�
    /// </summary>
    public void SetSimpleDialogue(string newText)
    {
        simpleDialogueText = newText;
    }

    /// <summary>
    /// ��̬���ý�������
    /// </summary>
    public void SetInteractionRange(float newRange)
    {
        interactionRange = Mathf.Max(0.1f, newRange);
    }

    /// <summary>
    /// ��̬���ý�������
    /// </summary>
    public void SetInteractKey(KeyCode newKey)
    {
        interactKey = newKey;
        // ������ʾ�ı�
        if (promptTextComponent != null)
        {
            promptTextComponent.text = string.Format(promptText, interactKey.ToString());
        }
    }

    /// <summary>
    /// ��̬������ʾ�ı�
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
    /// ��̬���ý�����ʾUI
    /// </summary>
    public void SetInteractionPrompt(GameObject newPrompt)
    {
        if (interactionPrompt != null && interactionPrompt.name == "InteractionPrompt_Auto")
        {
            // �����ǰʹ�õ����Զ�������UI����������
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
    /// ǿ�����´���������ʾUI
    /// </summary>
    [ContextMenu("���´�����ʾUI")]
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
        // ���ƽ�����Χ
        Gizmos.color = playerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        // ���Ƶ���ҵ�����
        if (player != null && playerInRange)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // ��������Ҫ��ĽǶȷ�Χ
        if (requirePlayerFacing && player != null)
        {
            Gizmos.color = Color.red;
            Vector3 playerToNPC = (transform.position - player.position).normalized;
            Vector3 leftBound = Quaternion.Euler(0, -facingAngleTolerance, 0) * playerToNPC;
            Vector3 rightBound = Quaternion.Euler(0, facingAngleTolerance, 0) * playerToNPC;

            Gizmos.DrawLine(player.position, player.position + leftBound * interactionRange);
            Gizmos.DrawLine(player.position, player.position + rightBound * interactionRange);
        }

        // ������ʾUIλ��
        if (autoCreatePromptUI || interactionPrompt != null)
        {
            Gizmos.color = Color.white;
            Vector3 promptPos = transform.position + Vector3.up * promptVerticalOffset;
            Gizmos.DrawWireCube(promptPos, Vector3.one * 0.2f);
            Gizmos.DrawLine(transform.position, promptPos);
        }

#if UNITY_EDITOR
        // ��ʾ״̬��Ϣ
        UnityEditor.Handles.color = Color.white;
        string statusText = $"�Ի�״̬:\n";
        statusText += $"�ɴ���: {CanTriggerDialogue()}\n";
        statusText += $"��������: {triggerCount}";
        if (maxTriggerCount > 0)
            statusText += $"/{maxTriggerCount}";
        statusText += $"\n��ȴ��: {isOnCooldown}";

        UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, statusText);
#endif
    }

    void OnDrawGizmos()
    {
        // ʼ����ʾ������Χ�İ�͸������
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, interactionRange);

        // ��ʾ�Ի���������
        Gizmos.color = inkDialogueFile != null ? Color.blue :
                      !string.IsNullOrEmpty(simpleDialogueText) ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 4f, Vector3.one * 0.3f);
    }
}