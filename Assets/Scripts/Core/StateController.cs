using UnityEngine;

/// <summary>
/// ͨ��״̬������ - �����ɫ�ĸ���״̬��������ս�����Ի�������������������ҡ�NPC���κν�ɫ��
/// </summary>
public class StateController : MonoBehaviour
{
    [Header("=== ��ɫ���� ===")]
    public CharacterType characterType = CharacterType.Player;

    [Header("=== ״̬���� ===")]
    public PlayerState currentState = PlayerState.Normal;
    public float combatExitDelay = 3f; // �뿪ս��״̬���ӳ�ʱ��

    [Header("=== �������� ===")]
    public bool showDebugInfo = false;

    // �������
    private Animator animator;
    private HealthUIController healthUIController;

    // ״̬��ʱ��
    private float combatTimer = 0f;
    private bool inCombatMode = false;

    // �¼�
    public System.Action<PlayerState, PlayerState> OnStateChanged;
    public System.Action OnEnterCombat;
    public System.Action OnExitCombat;
    public System.Action OnEnterDialogue;
    public System.Action OnExitDialogue;
    public System.Action OnCharacterDeath;
    public System.Action OnCharacterRevive;

    // ״̬��ѯ����
    public bool IsInNormalState => currentState == PlayerState.Normal;
    public bool IsInCombat => currentState == PlayerState.Combat;
    public bool IsInDialogue => currentState == PlayerState.Dialogue;
    public bool IsDead => currentState == PlayerState.Dead;

    void Awake()
    {
        animator = GetComponent<Animator>();
        healthUIController = GetComponentInChildren<HealthUIController>();
    }

    void Start()
    {
        // ��ʼ��״̬
        SetState(PlayerState.Normal);
    }

    void Update()
    {
        UpdateCombatTimer();
        UpdateAnimatorStates();
    }

    void UpdateCombatTimer()
    {
        // ս��״̬�Զ��˳���ʱ��
        if (currentState == PlayerState.Combat)
        {
            combatTimer += Time.deltaTime;

            if (combatTimer >= combatExitDelay)
            {
                ExitCombatState();
            }
        }
    }

    void UpdateAnimatorStates()
    {
        if (animator == null) return;

        // ���¶�������״̬����
        animator.SetBool("inCombat", currentState == PlayerState.Combat);
        animator.SetBool("inDialogue", currentState == PlayerState.Dialogue);
        animator.SetBool("isDead", currentState == PlayerState.Dead);
    }

    // ========== ״̬�л����� ==========

    /// <summary>
    /// �������״̬
    /// </summary>
    public void SetState(PlayerState newState)
    {
        if (currentState == newState) return;

        PlayerState oldState = currentState;

        // �˳���ǰ״̬
        ExitCurrentState();

        // �л�����״̬
        currentState = newState;

        // ������״̬
        EnterNewState();

        // ����״̬�仯�¼�
        OnStateChanged?.Invoke(oldState, newState);

        if (showDebugInfo)
        {
            Debug.Log($"[PlayerState] ״̬�仯: {oldState} -> {newState}");
        }
    }

    void ExitCurrentState()
    {
        switch (currentState)
        {
            case PlayerState.Combat:
                HandleExitCombat();
                break;
            case PlayerState.Dialogue:
                HandleExitDialogue();
                break;
        }
    }

    void EnterNewState()
    {
        switch (currentState)
        {
            case PlayerState.Normal:
                HandleEnterNormal();
                break;
            case PlayerState.Combat:
                HandleEnterCombat();
                break;
            case PlayerState.Dialogue:
                HandleEnterDialogue();
                break;
            case PlayerState.Dead:
                HandleEnterDead();
                break;
        }
    }

    // ========== ״̬������� ==========

    void HandleEnterNormal()
    {
        // ����ս��UI
        if (healthUIController != null)
        {
            healthUIController.gameObject.SetActive(false);
        }

        // ������ͨ״̬����
        if (animator != null)
        {
            animator.SetTrigger("EnterNormal");
        }
    }

    void HandleEnterCombat()
    {
        combatTimer = 0f;
        inCombatMode = true;

        // ��ʾս��UI
        if (healthUIController != null)
        {
            healthUIController.gameObject.SetActive(true);
        }

        // ����ս��״̬����
        if (animator != null)
        {
            animator.SetTrigger("EnterCombat");
        }

        OnEnterCombat?.Invoke();
    }

    void HandleExitCombat()
    {
        inCombatMode = false;

        // ����ս��UI
        if (healthUIController != null)
        {
            healthUIController.gameObject.SetActive(false);
        }

        OnExitCombat?.Invoke();
    }

    void HandleEnterDialogue()
    {
        // ֹͣ�����ƶ�
        var movementController = GetComponent<MovementController>();
        if (movementController != null)
        {
            movementController.ForceStopMovement();
        }

        // ���ŶԻ�����
        if (animator != null)
        {
            animator.SetTrigger("Dialogue");
        }

        OnEnterDialogue?.Invoke();
    }

    void HandleExitDialogue()
    {
        OnExitDialogue?.Invoke();
    }

    void HandleEnterDead()
    {
        // ֹͣ�����ƶ�
        var movementController = GetComponent<MovementController>();
        if (movementController != null)
        {
            movementController.ForceStopMovement();
        }

        // ������������
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // ��ʾ����UI���������߼�
        OnCharacterDeath?.Invoke();
    }

    // ========== ����״̬�л��ӿ� ==========

    /// <summary>
    /// ����ս��״̬
    /// </summary>
    public void EnterCombatState()
    {
        SetState(PlayerState.Combat);
    }

    /// <summary>
    /// �˳�ս��״̬
    /// </summary>
    public void ExitCombatState()
    {
        if (currentState == PlayerState.Combat)
        {
            SetState(PlayerState.Normal);
        }
    }

    /// <summary>
    /// ����ս����ʱ����������ս��״̬��
    /// </summary>
    public void RefreshCombatState()
    {
        if (currentState == PlayerState.Combat)
        {
            combatTimer = 0f;
        }
        else
        {
            EnterCombatState();
        }
    }

    /// <summary>
    /// ����Ի�״̬
    /// </summary>
    public void EnterDialogueState()
    {
        SetState(PlayerState.Dialogue);
    }

    /// <summary>
    /// �˳��Ի�״̬
    /// </summary>
    public void ExitDialogueState()
    {
        if (currentState == PlayerState.Dialogue)
        {
            SetState(PlayerState.Normal);
        }
    }

    /// <summary>
    /// ��������״̬
    /// </summary>
    public void EnterDeadState()
    {
        SetState(PlayerState.Dead);
    }

    /// <summary>
    /// �����ɫ
    /// </summary>
    public void ReviveCharacter()
    {
        if (currentState == PlayerState.Dead)
        {
            SetState(PlayerState.Normal);
            OnCharacterRevive?.Invoke();

            if (showDebugInfo)
            {
                Debug.Log("[StateController] ��ɫ����");
            }
        }
    }

    /// <summary>
    /// ������ң������Է�����
    /// </summary>
    public void RevivePlayer()
    {
        ReviveCharacter();
    }

    // ========== ״̬��鷽�� ==========

    /// <summary>
    /// ����Ƿ�����ƶ�
    /// </summary>
    public bool CanMove()
    {
        return currentState != PlayerState.Dead &&
               currentState != PlayerState.Dialogue;
    }

    /// <summary>
    /// ����Ƿ���Թ���
    /// </summary>
    public bool CanAttack()
    {
        return currentState != PlayerState.Dead &&
               currentState != PlayerState.Dialogue;
    }

    /// <summary>
    /// ����Ƿ����ʹ�ü���
    /// </summary>
    public bool CanUseSkill()
    {
        return currentState != PlayerState.Dead &&
               currentState != PlayerState.Dialogue;
    }

    /// <summary>
    /// ����Ƿ������NPC����
    /// </summary>
    public bool CanInteract()
    {
        return currentState == PlayerState.Normal;
    }

    /// <summary>
    /// ����Ƿ����ʹ����Ʒ
    /// </summary>
    public bool CanUseItems()
    {
        return currentState != PlayerState.Dead &&
               currentState != PlayerState.Dialogue;
    }

    // ========== ǿ��״̬���� ==========

    /// <summary>
    /// ǿ�ƽ���ָ��״̬���������л���
    /// </summary>
    public void ForceSetState(PlayerState state)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[StateController] ǿ������״̬: {state}");
        }

        SetState(state);
    }

    /// <summary>
    /// ���õ�����״̬
    /// </summary>
    public void ResetToNormalState()
    {
        SetState(PlayerState.Normal);
        combatTimer = 0f;
        inCombatMode = false;
    }

    // ========== �¼���Ӧ���� ==========

    /// <summary>
    /// ��Ӧ�ܵ��˺��¼�
    /// </summary>
    public void OnTakeDamage()
    {
        // �ܵ��˺�ʱ�Զ�����ս��״̬
        if (currentState == PlayerState.Normal)
        {
            EnterCombatState();
        }
        else if (currentState == PlayerState.Combat)
        {
            RefreshCombatState();
        }
    }

    /// <summary>
    /// ��Ӧִ�й����¼�
    /// </summary>
    public void OnPerformAttack()
    {
        // ����ʱ�Զ�����ս��״̬
        if (currentState == PlayerState.Normal)
        {
            EnterCombatState();
        }
        else if (currentState == PlayerState.Combat)
        {
            RefreshCombatState();
        }
    }

    /// <summary>
    /// ��Ӧʹ�ü����¼�
    /// </summary>
    public void OnUseSkill()
    {
        // ʹ�ü���ʱˢ��ս��״̬
        if (currentState == PlayerState.Combat)
        {
            RefreshCombatState();
        }
    }

    // ========== ���Թ��� ==========

    [ContextMenu("����ս��״̬")]
    void DebugEnterCombat()
    {
        EnterCombatState();
    }

    [ContextMenu("�˳�ս��״̬")]
    void DebugExitCombat()
    {
        ExitCombatState();
    }

    [ContextMenu("����Ի�״̬")]
    void DebugEnterDialogue()
    {
        EnterDialogueState();
    }

    [ContextMenu("�˳��Ի�״̬")]
    void DebugExitDialogue()
    {
        ExitDialogueState();
    }

    [ContextMenu("����״̬")]
    void DebugResetState()
    {
        ResetToNormalState();
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 220, 200, 150));
        GUILayout.BeginVertical("box");

        GUILayout.Label("���״̬����", GUI.skin.label);
        GUILayout.Space(5);

        GUILayout.Label($"��ǰ״̬: {currentState}");
        GUILayout.Label($"�����ƶ�: {CanMove()}");
        GUILayout.Label($"���Թ���: {CanAttack()}");
        GUILayout.Label($"���Խ���: {CanInteract()}");

        if (currentState == PlayerState.Combat)
        {
            float remainingTime = combatExitDelay - combatTimer;
            GUILayout.Label($"ս����ʱ: {remainingTime:F1}s");
        }

        GUILayout.Space(5);

        if (GUILayout.Button("����״̬"))
        {
            SetState(PlayerState.Normal);
        }

        if (GUILayout.Button("ս��״̬"))
        {
            SetState(PlayerState.Combat);
        }

        if (GUILayout.Button("�Ի�״̬"))
        {
            SetState(PlayerState.Dialogue);
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        // ����״̬��ʾ��ͬ��ɫ��ָʾ��
        Color stateColor = currentState switch
        {
            PlayerState.Normal => Color.green,
            PlayerState.Combat => Color.red,
            PlayerState.Dialogue => Color.blue,
            PlayerState.Dead => Color.black,
            _ => Color.white
        };

        Gizmos.color = stateColor;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.5f);
    }
}