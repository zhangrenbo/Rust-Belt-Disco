using UnityEngine;

/// <summary>
/// �Ի��ű������� - ���жԻ��ű�����Ҫ�̳������
/// </summary>
public abstract class BaseDialogueScript : MonoBehaviour
{
    [Header("=== �Ի��������� ===")]
    public string dialogueId = "";
    public bool canRepeat = true;
    public int maxTriggerCount = -1; // -1��ʾ������
    public float cooldownTime = 0f;

    protected int triggerCount = 0;
    protected float lastTriggerTime = 0f;

    /// <summary>
    /// ����Ƿ���Դ����Ի�
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
    /// ��ʼ�Ի� - �������ʵ��
    /// </summary>
    public abstract void StartDialogue(InteractiveDialogueTrigger trigger);

    /// <summary>
    /// ���Ϊ�Ѵ���
    /// </summary>
    protected void MarkAsTriggered()
    {
        triggerCount++;
        lastTriggerTime = Time.time;
    }

    /// <summary>
    /// ���öԻ�״̬
    /// </summary>
    public virtual void ResetDialogueState()
    {
        triggerCount = 0;
        lastTriggerTime = 0f;
    }
}

/// <summary>
/// ����ʽ�Ի������� - ���������NPC�ĶԻ�����
/// </summary>
public class InteractiveDialogueTrigger : MonoBehaviour
{
    [Header("=== �������� ===")]
    public KeyCode interactKey = KeyCode.E;
    public float interactionRange = 3f;
    public LayerMask playerLayer = -1;
    public string promptText = "�� E �Ի�";

    [Header("=== �Ի��ű� ===")]
    public BaseDialogueScript[] dialogueScripts;
    public bool executeInOrder = false;
    public bool repeatLastScript = true;

    [Header("=== UI���� ===")]
    public GameObject interactionPrompt;

    // ״̬����
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
    /// �����Ի�
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
    /// ��ȡ��һ��Ҫִ�еĶԻ��ű�
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
            // ���ѡ����õĽű�
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
    /// ����Ƿ����κζԻ����Դ���
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
    /// �Ի���ɻص�
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
    /// �ֶ�����
    /// </summary>
    public void ManualTrigger()
    {
        TriggerDialogue();
    }

    void OnDrawGizmosSelected()
    {
        // ��ʾ������Χ
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}