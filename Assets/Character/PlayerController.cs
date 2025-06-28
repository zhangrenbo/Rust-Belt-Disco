using UnityEngine;

/// <summary>
/// 玩家控制器 - 核心控制器，自动管理所有相关组件
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("=== 玩家基础配置 ===")]
    public string playerName = "玩家";
    public int startingLevel = 1;
    public bool enableDebugUI = false;

    [Header("=== 移动配置 ===")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.5f;
    public bool faceCamera = true;

    [Header("=== 属性配置 ===")]
    public int baseStrength = 5;
    public int baseAgility = 5;
    public int baseIntelligence = 5;
    public int baseStamina = 5;
    public int baseVitality = 5;

    [Header("=== 战斗配置 ===")]
    public float attackCooldown = 0.5f;
    public int baseAttackPower = 10;

    // 自动管理的组件引用
    private MovementController movementController;
    private AttributeController attributeController;
    private StateController stateController;
    private CharacterStateManager characterStateManager;
    private Animator animator;
    private Rigidbody rb;

    // 公共事件
    public System.Action<int> OnLevelUp;
    public System.Action<PlayerState, PlayerState> OnStateChanged;
    public System.Action OnPlayerDeath;
    public System.Action OnPlayerRevive;

    // 快速访问属性 - 修正循环引用问题
    public int Level => attributeController?.Level ?? startingLevel;
    public int Health => characterStateManager?.currentHealth ?? 100;
    public int MaxHealth => characterStateManager?.maxHealth ?? 100;
    public PlayerState CurrentState => stateController?.currentState ?? PlayerState.Normal;
    public bool IsMoving => movementController?.IsMoving ?? false;
    public bool IsInCombat => stateController?.IsInCombat ?? false;
    public bool IsInDialogue => stateController?.IsInDialogue ?? false;

    // 属性访问器
    public int Strength => attributeController?.Strength ?? baseStrength;
    public int Agility => attributeController?.Agility ?? baseAgility;
    public int Intelligence => attributeController?.Intelligence ?? baseIntelligence;
    public int Stamina => attributeController?.Stamina ?? baseStamina;
    public int Vitality => attributeController?.Vitality ?? baseVitality;
    public int Morality => attributeController?.Morality ?? 100;
    public int Experience => attributeController?.Experience ?? 0;
    public int ExperienceToNext => attributeController?.ExperienceToNext ?? 100;

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        ConfigureComponents();
        BindEvents();
        InitializePlayer();
    }

    /// <summary>
    /// 自动获取或添加所有必需的组件
    /// </summary>
    void InitializeComponents()
    {
        // 基础组件
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // 核心控制组件 - 自动添加
        movementController = GetOrAddComponent<MovementController>();
        attributeController = GetOrAddComponent<AttributeController>();
        stateController = GetOrAddComponent<StateController>();
        characterStateManager = GetOrAddComponent<CharacterStateManager>();

        // 确保有碰撞器
        if (GetComponent<Collider>() == null)
        {
            var capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = Vector3.up;
        }

        Debug.Log("[PlayerController] 所有组件初始化完成");
    }

    /// <summary>
    /// 配置所有组件的参数
    /// </summary>
    void ConfigureComponents()
    {
        // 配置移动控制器
        if (movementController != null)
        {
            movementController.moveSpeed = moveSpeed;
            movementController.runSpeedMultiplier = runSpeedMultiplier;
            movementController.faceCamera = faceCamera;
        }

        // 配置属性控制器
        if (attributeController != null)
        {
            attributeController.characterType = CharacterType.Player;
            attributeController.characterName = playerName;
            attributeController.level = startingLevel;
            attributeController.baseStrength = baseStrength;
            attributeController.baseAgility = baseAgility;
            attributeController.baseIntelligence = baseIntelligence;
            attributeController.baseStamina = baseStamina;
            attributeController.baseVitality = baseVitality;
        }

        // 配置状态控制器
        if (stateController != null)
        {
            stateController.characterType = CharacterType.Player;
        }

        // 配置角色状态管理器
        if (characterStateManager != null)
        {
            characterStateManager.characterType = CharacterType.Player;
            characterStateManager.characterName = playerName;
            characterStateManager.baseMovementSpeed = moveSpeed;
        }

        Debug.Log("[PlayerController] 所有组件配置完成");
    }

    /// <summary>
    /// 绑定组件间的事件监听
    /// </summary>
    void BindEvents()
    {
        // 属性控制器事件
        if (attributeController != null)
        {
            attributeController.OnLevelUp += HandleLevelUp;
            attributeController.OnAttributesChanged += HandleAttributesChanged;
        }

        // 状态控制器事件
        if (stateController != null)
        {
            stateController.OnStateChanged += HandleStateChanged;
            stateController.OnCharacterDeath += HandlePlayerDeath;
            stateController.OnCharacterRevive += HandlePlayerRevive;
        }

        // 移动控制器事件
        if (movementController != null)
        {
            movementController.OnRunningStateChanged += HandleRunningStateChanged;
        }

        Debug.Log("[PlayerController] 事件绑定完成");
    }

    void InitializePlayer()
    {
        // 设置玩家标签
        gameObject.tag = "Player";

        // 初始化物理设置
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.useGravity = true;
        }

        Debug.Log($"[PlayerController] 玩家 '{playerName}' 初始化完成");
    }

    // ========== 事件处理 ==========
    void HandleLevelUp(int newLevel)
    {
        Debug.Log($"[PlayerController] 玩家升级到 {newLevel} 级");
        OnLevelUp?.Invoke(newLevel);
    }

    void HandleAttributesChanged(int str, int agi, int intel, int sta, int vit)
    {
        // 同步属性到角色状态管理器
        if (characterStateManager != null)
        {
            characterStateManager.strength = str;
            characterStateManager.agility = agi;
            characterStateManager.intelligence = intel;
            characterStateManager.stamina = sta;
            characterStateManager.vitality = vit;
        }
    }

    void HandleStateChanged(PlayerState oldState, PlayerState newState)
    {
        // 更新动画器状态
        if (animator != null)
        {
            animator.SetBool("inCombat", newState == PlayerState.Combat);
            animator.SetBool("inDialogue", newState == PlayerState.Dialogue);
            animator.SetBool("isDead", newState == PlayerState.Dead);
        }

        OnStateChanged?.Invoke(oldState, newState);
    }

    void HandlePlayerDeath()
    {
        Debug.Log("[PlayerController] 玩家死亡");
        OnPlayerDeath?.Invoke();
    }

    void HandlePlayerRevive()
    {
        Debug.Log("[PlayerController] 玩家复活");
        OnPlayerRevive?.Invoke();
    }

    void HandleRunningStateChanged(bool isRunning)
    {
        if (animator != null)
        {
            animator.SetBool("isRunning", isRunning);
        }
    }

    // ========== 公共接口 - 玩家控制 ==========

    /// <summary>
    /// 移动相关接口
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        movementController?.SetMoveSpeed(speed);
    }

    public void ForceStopMovement()
    {
        movementController?.ForceStopMovement();
    }

    /// <summary>
    /// 属性相关接口
    /// </summary>
    public void AddExp(int amount)
    {
        attributeController?.AddExp(amount);
    }

    public void SetLevel(int newLevel)
    {
        attributeController?.SetLevel(newLevel);
    }

    public void AddMorality(int amount)
    {
        attributeController?.AddMorality(amount);
    }

    public void DeductMorality(int amount)
    {
        attributeController?.DeductMorality(amount);
    }

    /// <summary>
    /// 战斗相关接口
    /// </summary>
    public void TakeDamage(int damage)
    {
        characterStateManager?.TakeDamage(damage);
        stateController?.OnTakeDamage();
    }

    public void Heal(int amount)
    {
        characterStateManager?.Heal(amount);
    }

    /// <summary>
    /// 状态相关接口
    /// </summary>
    public void EnterCombatState()
    {
        stateController?.EnterCombatState();
    }

    public void ExitCombatState()
    {
        stateController?.ExitCombatState();
    }

    public void EnterDialogueState()
    {
        stateController?.EnterDialogueState();
    }

    public void ExitDialogueState()
    {
        stateController?.ExitDialogueState();
    }

    /// <summary>
    /// 检查玩家能力
    /// </summary>
    public bool CanMove() => stateController?.CanMove() ?? false;
    public bool CanAttack() => stateController?.CanAttack() ?? false;
    public bool CanInteract() => stateController?.CanInteract() ?? false;

    /// <summary>
    /// 获取详细状态信息
    /// </summary>
    public string GetDetailedStatus()
    {
        return $"玩家: {playerName}\n" +
               $"等级: {Level} (经验: {Experience}/{ExperienceToNext})\n" +
               $"生命: {Health}/{MaxHealth}\n" +
               $"道德: {Morality}\n" +
               $"力量: {Strength}, 敏捷: {Agility}, 智力: {Intelligence}\n" +
               $"体力: {Stamina}, 活力: {Vitality}\n" +
               $"状态: {CurrentState}";
    }

    // ========== 辅助方法 ==========
    T GetOrAddComponent<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
            Debug.Log($"[PlayerController] 自动添加组件: {typeof(T).Name}");
        }
        return component;
    }

    // ========== 调试功能 ==========
    [ContextMenu("重置玩家")]
    public void ResetPlayer()
    {
        stateController?.ResetToNormalState();
        characterStateManager?.ResetCharacter();
        movementController?.ForceStopMovement();

        if (attributeController != null)
        {
            attributeController.level = startingLevel;
            attributeController.SendMessage("DebugResetAttributes", SendMessageOptions.DontRequireReceiver);
        }
    }

    [ContextMenu("测试升级")]
    public void DebugLevelUp()
    {
        AddExp(100);
    }

    [ContextMenu("测试受伤")]
    public void DebugTakeDamage()
    {
        TakeDamage(20);
    }

    [ContextMenu("完全治疗")]
    public void DebugFullHeal()
    {
        Heal(MaxHealth);
    }

    [ContextMenu("打印状态")]
    public void DebugPrintStatus()
    {
        Debug.Log(GetDetailedStatus());
    }

    void OnGUI()
    {
        if (!enableDebugUI) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 250));
        GUILayout.BeginVertical("box");

        GUILayout.Label($"玩家: {playerName}", GUI.skin.label);
        GUILayout.Label($"等级: {Level} | 生命: {Health}/{MaxHealth}");
        GUILayout.Label($"经验: {Experience}/{ExperienceToNext}");
        GUILayout.Label($"道德: {Morality}");
        GUILayout.Label($"状态: {CurrentState}");
        GUILayout.Label($"移动: {(IsMoving ? "是" : "否")} | 战斗: {(IsInCombat ? "是" : "否")}");

        GUILayout.Space(5);
        GUILayout.Label($"力量: {Strength} | 敏捷: {Agility}");
        GUILayout.Label($"智力: {Intelligence} | 体力: {Stamina}");
        GUILayout.Label($"活力: {Vitality}");

        GUILayout.Space(10);

        if (GUILayout.Button("升级")) DebugLevelUp();
        if (GUILayout.Button("受伤")) DebugTakeDamage();
        if (GUILayout.Button("治疗")) DebugFullHeal();
        if (GUILayout.Button("重置")) ResetPlayer();
        if (GUILayout.Button("打印状态")) DebugPrintStatus();

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        // 清理事件绑定
        if (attributeController != null)
        {
            attributeController.OnLevelUp -= HandleLevelUp;
            attributeController.OnAttributesChanged -= HandleAttributesChanged;
        }

        if (stateController != null)
        {
            stateController.OnStateChanged -= HandleStateChanged;
            stateController.OnCharacterDeath -= HandlePlayerDeath;
            stateController.OnCharacterRevive -= HandlePlayerRevive;
        }

        if (movementController != null)
        {
            movementController.OnRunningStateChanged -= HandleRunningStateChanged;
        }
    }
}