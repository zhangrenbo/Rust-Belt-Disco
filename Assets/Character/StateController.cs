using UnityEngine;

/// <summary>
/// 通用状态控制器 - 管理角色的各种状态（正常、战斗、对话、死亡）（可用于玩家、NPC等任何角色）
/// </summary>
public class StateController : MonoBehaviour
{
    [Header("=== 角色类型 ===")]
    public CharacterType characterType = CharacterType.Player;

    [Header("=== 状态设置 ===")]
    public PlayerState currentState = PlayerState.Normal;
    public float combatExitDelay = 3f; // 离开战斗状态的延迟时间

    [Header("=== 调试设置 ===")]
    public bool showDebugInfo = false;

    // 组件引用
    private Animator animator;
    private HealthUIController healthUIController;

    // 状态计时器
    private float combatTimer = 0f;
    private bool inCombatMode = false;

    // 事件
    public System.Action<PlayerState, PlayerState> OnStateChanged;
    public System.Action OnEnterCombat;
    public System.Action OnExitCombat;
    public System.Action OnEnterDialogue;
    public System.Action OnExitDialogue;
    public System.Action OnCharacterDeath;
    public System.Action OnCharacterRevive;

    // 状态查询属性
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
        // 初始化状态
        SetState(PlayerState.Normal);
    }

    void Update()
    {
        UpdateCombatTimer();
        UpdateAnimatorStates();
    }

    void UpdateCombatTimer()
    {
        // 战斗状态自动退出计时器
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

        // 更新动画器的状态参数
        animator.SetBool("inCombat", currentState == PlayerState.Combat);
        animator.SetBool("inDialogue", currentState == PlayerState.Dialogue);
        animator.SetBool("isDead", currentState == PlayerState.Dead);
    }

    // ========== 状态切换方法 ==========

    /// <summary>
    /// 设置玩家状态
    /// </summary>
    public void SetState(PlayerState newState)
    {
        if (currentState == newState) return;

        PlayerState oldState = currentState;

        // 退出当前状态
        ExitCurrentState();

        // 切换到新状态
        currentState = newState;

        // 进入新状态
        EnterNewState();

        // 触发状态变化事件
        OnStateChanged?.Invoke(oldState, newState);

        if (showDebugInfo)
        {
            Debug.Log($"[PlayerState] 状态变化: {oldState} -> {newState}");
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

    // ========== 状态处理方法 ==========

    void HandleEnterNormal()
    {
        // 隐藏战斗UI
        if (healthUIController != null)
        {
            healthUIController.gameObject.SetActive(false);
        }

        // 播放普通状态动画
        if (animator != null)
        {
            animator.SetTrigger("EnterNormal");
        }
    }

    void HandleEnterCombat()
    {
        combatTimer = 0f;
        inCombatMode = true;

        // 显示战斗UI
        if (healthUIController != null)
        {
            healthUIController.gameObject.SetActive(true);
        }

        // 播放战斗状态动画
        if (animator != null)
        {
            animator.SetTrigger("EnterCombat");
        }

        OnEnterCombat?.Invoke();
    }

    void HandleExitCombat()
    {
        inCombatMode = false;

        // 隐藏战斗UI
        if (healthUIController != null)
        {
            healthUIController.gameObject.SetActive(false);
        }

        OnExitCombat?.Invoke();
    }

    void HandleEnterDialogue()
    {
        // 停止所有移动
        var movementController = GetComponent<MovementController>();
        if (movementController != null)
        {
            movementController.ForceStopMovement();
        }

        // 播放对话动画
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
        // 停止所有移动
        var movementController = GetComponent<MovementController>();
        if (movementController != null)
        {
            movementController.ForceStopMovement();
        }

        // 播放死亡动画
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // 显示死亡UI或处理死亡逻辑
        OnCharacterDeath?.Invoke();
    }

    // ========== 公共状态切换接口 ==========

    /// <summary>
    /// 进入战斗状态
    /// </summary>
    public void EnterCombatState()
    {
        SetState(PlayerState.Combat);
    }

    /// <summary>
    /// 退出战斗状态
    /// </summary>
    public void ExitCombatState()
    {
        if (currentState == PlayerState.Combat)
        {
            SetState(PlayerState.Normal);
        }
    }

    /// <summary>
    /// 重置战斗计时器（保持在战斗状态）
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
    /// 进入对话状态
    /// </summary>
    public void EnterDialogueState()
    {
        SetState(PlayerState.Dialogue);
    }

    /// <summary>
    /// 退出对话状态
    /// </summary>
    public void ExitDialogueState()
    {
        if (currentState == PlayerState.Dialogue)
        {
            SetState(PlayerState.Normal);
        }
    }

    /// <summary>
    /// 进入死亡状态
    /// </summary>
    public void EnterDeadState()
    {
        SetState(PlayerState.Dead);
    }

    /// <summary>
    /// 复活角色
    /// </summary>
    public void ReviveCharacter()
    {
        if (currentState == PlayerState.Dead)
        {
            SetState(PlayerState.Normal);
            OnCharacterRevive?.Invoke();

            if (showDebugInfo)
            {
                Debug.Log("[StateController] 角色复活");
            }
        }
    }

    /// <summary>
    /// 复活玩家（兼容性方法）
    /// </summary>
    public void RevivePlayer()
    {
        ReviveCharacter();
    }

    // ========== 状态检查方法 ==========

    /// <summary>
    /// 检查是否可以移动
    /// </summary>
    public bool CanMove()
    {
        return currentState != PlayerState.Dead &&
               currentState != PlayerState.Dialogue;
    }

    /// <summary>
    /// 检查是否可以攻击
    /// </summary>
    public bool CanAttack()
    {
        return currentState != PlayerState.Dead &&
               currentState != PlayerState.Dialogue;
    }

    /// <summary>
    /// 检查是否可以使用技能
    /// </summary>
    public bool CanUseSkill()
    {
        return currentState != PlayerState.Dead &&
               currentState != PlayerState.Dialogue;
    }

    /// <summary>
    /// 检查是否可以与NPC交互
    /// </summary>
    public bool CanInteract()
    {
        return currentState == PlayerState.Normal;
    }

    /// <summary>
    /// 检查是否可以使用物品
    /// </summary>
    public bool CanUseItems()
    {
        return currentState != PlayerState.Dead &&
               currentState != PlayerState.Dialogue;
    }

    // ========== 强制状态操作 ==========

    /// <summary>
    /// 强制进入指定状态（无条件切换）
    /// </summary>
    public void ForceSetState(PlayerState state)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[StateController] 强制设置状态: {state}");
        }

        SetState(state);
    }

    /// <summary>
    /// 重置到正常状态
    /// </summary>
    public void ResetToNormalState()
    {
        SetState(PlayerState.Normal);
        combatTimer = 0f;
        inCombatMode = false;
    }

    // ========== 事件响应方法 ==========

    /// <summary>
    /// 响应受到伤害事件
    /// </summary>
    public void OnTakeDamage()
    {
        // 受到伤害时自动进入战斗状态
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
    /// 响应执行攻击事件
    /// </summary>
    public void OnPerformAttack()
    {
        // 攻击时自动进入战斗状态
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
    /// 响应使用技能事件
    /// </summary>
    public void OnUseSkill()
    {
        // 使用技能时刷新战斗状态
        if (currentState == PlayerState.Combat)
        {
            RefreshCombatState();
        }
    }

    // ========== 调试功能 ==========

    [ContextMenu("进入战斗状态")]
    void DebugEnterCombat()
    {
        EnterCombatState();
    }

    [ContextMenu("退出战斗状态")]
    void DebugExitCombat()
    {
        ExitCombatState();
    }

    [ContextMenu("进入对话状态")]
    void DebugEnterDialogue()
    {
        EnterDialogueState();
    }

    [ContextMenu("退出对话状态")]
    void DebugExitDialogue()
    {
        ExitDialogueState();
    }

    [ContextMenu("重置状态")]
    void DebugResetState()
    {
        ResetToNormalState();
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 220, 200, 150));
        GUILayout.BeginVertical("box");

        GUILayout.Label("玩家状态调试", GUI.skin.label);
        GUILayout.Space(5);

        GUILayout.Label($"当前状态: {currentState}");
        GUILayout.Label($"可以移动: {CanMove()}");
        GUILayout.Label($"可以攻击: {CanAttack()}");
        GUILayout.Label($"可以交互: {CanInteract()}");

        if (currentState == PlayerState.Combat)
        {
            float remainingTime = combatExitDelay - combatTimer;
            GUILayout.Label($"战斗计时: {remainingTime:F1}s");
        }

        GUILayout.Space(5);

        if (GUILayout.Button("正常状态"))
        {
            SetState(PlayerState.Normal);
        }

        if (GUILayout.Button("战斗状态"))
        {
            SetState(PlayerState.Combat);
        }

        if (GUILayout.Button("对话状态"))
        {
            SetState(PlayerState.Dialogue);
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        // 根据状态显示不同颜色的指示器
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