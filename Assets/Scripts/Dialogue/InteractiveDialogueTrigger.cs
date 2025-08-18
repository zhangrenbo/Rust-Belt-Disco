using UnityEngine;

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
    [SerializeField] private Transform player;
    private bool playerInRange = false;
    private int currentScriptIndex = 0;

    void Start()
    {
        if (player == null)
        {
            Debug.LogWarning($"[InteractiveDialogueTrigger] Player reference not set for {gameObject.name}");
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
    /// ���������л���������λ�ã�
    /// </summary>
    public void SetPlayer(Transform p)
    {
        player = p;
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
