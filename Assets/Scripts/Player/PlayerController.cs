using UnityEngine;

/// <summary>
/// ��ҿ����� - ���Ŀ��������Զ���������������
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("=== ��һ������� ===")]
    public string playerName = "���";
    public int startingLevel = 1;
    public bool enableDebugUI = false;

    [Header("=== �ƶ����� ===")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.5f;
    public bool faceCamera = true;

    [Header("=== �������� ===")]
    public int baseStrength = 5;
    public int baseAgility = 5;
    public int baseIntelligence = 5;
    public int baseStamina = 5;
    public int baseVitality = 5;

    [Header("=== ս������ ===")]
    public float attackCooldown = 0.5f;
    public int baseAttackPower = 10;

    // �Զ�������������
    private MovementController movementController;
    private AttributeController attributeController;
    private StateController stateController;
    private CharacterStateManager characterStateManager;
    private Animator animator;
    private Rigidbody rb;

    // �����¼�
    public System.Action<int> OnLevelUp;
    public System.Action<PlayerState, PlayerState> OnStateChanged;
    public System.Action OnPlayerDeath;
    public System.Action OnPlayerRevive;

    // ���ٷ������� - ��������ñ���
    public int Level => attributeController?.Level ?? startingLevel;
    public int Health => characterStateManager?.currentHealth ?? 100;
    public int MaxHealth => characterStateManager?.maxHealth ?? 100;
    public PlayerState CurrentState => stateController?.currentState ?? PlayerState.Normal;
    public bool IsMoving => movementController?.IsMoving ?? false;
    public bool IsInCombat => stateController?.IsInCombat ?? false;
    public bool IsInDialogue => stateController?.IsInDialogue ?? false;

    // ���Է�����
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
    /// �Զ���ȡ��������б�������
    /// </summary>
    void InitializeComponents()
    {
        // �������
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // ���Ŀ������ - �Զ����
        movementController = GetOrAddComponent<MovementController>();
        attributeController = GetOrAddComponent<AttributeController>();
        stateController = GetOrAddComponent<StateController>();
        characterStateManager = GetOrAddComponent<CharacterStateManager>();

        // ȷ������ײ��
        if (GetComponent<Collider>() == null)
        {
            var capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = Vector3.up;
        }

        Debug.Log("[PlayerController] ���������ʼ�����");
    }

    /// <summary>
    /// ������������Ĳ���
    /// </summary>
    void ConfigureComponents()
    {
        // �����ƶ�������
        if (movementController != null)
        {
            movementController.moveSpeed = moveSpeed;
            movementController.runSpeedMultiplier = runSpeedMultiplier;
            movementController.faceCamera = faceCamera;
        }

        // �������Կ�����
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

        // ����״̬������
        if (stateController != null)
        {
            stateController.characterType = CharacterType.Player;
        }

        // ���ý�ɫ״̬������
        if (characterStateManager != null)
        {
            characterStateManager.characterType = CharacterType.Player;
            characterStateManager.characterName = playerName;
            characterStateManager.baseMovementSpeed = moveSpeed;
        }

        Debug.Log("[PlayerController] ��������������");
    }

    /// <summary>
    /// ���������¼�����
    /// </summary>
    void BindEvents()
    {
        // ���Կ������¼�
        if (attributeController != null)
        {
            attributeController.OnLevelUp += HandleLevelUp;
            attributeController.OnAttributesChanged += HandleAttributesChanged;
        }

        // ״̬�������¼�
        if (stateController != null)
        {
            stateController.OnStateChanged += HandleStateChanged;
            stateController.OnCharacterDeath += HandlePlayerDeath;
            stateController.OnCharacterRevive += HandlePlayerRevive;
        }

        // �ƶ��������¼�
        if (movementController != null)
        {
            movementController.OnRunningStateChanged += HandleRunningStateChanged;
        }

        Debug.Log("[PlayerController] �¼������");
    }

    void InitializePlayer()
    {
        // ������ұ�ǩ
        gameObject.tag = "Player";

        // ��ʼ����������
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.useGravity = true;
        }

        Debug.Log($"[PlayerController] ��� '{playerName}' ��ʼ�����");
    }

    // ========== �¼����� ==========
    void HandleLevelUp(int newLevel)
    {
        Debug.Log($"[PlayerController] ��������� {newLevel} ��");
        OnLevelUp?.Invoke(newLevel);
    }

    void HandleAttributesChanged(int str, int agi, int intel, int sta, int vit)
    {
        // ͬ�����Ե���ɫ״̬������
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
        // ���¶�����״̬
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
        Debug.Log("[PlayerController] �������");
        OnPlayerDeath?.Invoke();
    }

    void HandlePlayerRevive()
    {
        Debug.Log("[PlayerController] ��Ҹ���");
        OnPlayerRevive?.Invoke();
    }

    void HandleRunningStateChanged(bool isRunning)
    {
        if (animator != null)
        {
            animator.SetBool("isRunning", isRunning);
        }
    }

    // ========== �����ӿ� - ��ҿ��� ==========

    /// <summary>
    /// �ƶ���ؽӿ�
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
    /// ������ؽӿ�
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
    /// ս����ؽӿ� - ֧��IDamageable�ӿ�
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
    /// ״̬��ؽӿ�
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
    /// ����������
    /// </summary>
    public bool CanMove() => stateController?.CanMove() ?? false;
    public bool CanAttack() => stateController?.CanAttack() ?? false;
    public bool CanInteract() => stateController?.CanInteract() ?? false;

    // ========== IDamageable �ӿ�ʵ�� ==========

    /// <summary>
    /// ��ȡ��ǰ����ֵ - IDamageable�ӿ�
    /// </summary>
    public int GetCurrentHealth()
    {
        return characterStateManager?.currentHealth ?? 0;
    }

    /// <summary>
    /// ��ȡ�������ֵ - IDamageable�ӿ�
    /// </summary>
    public int GetMaxHealth()
    {
        return characterStateManager?.maxHealth ?? 0;
    }

    /// <summary>
    /// ����Ƿ����� - IDamageable�ӿ�
    /// </summary>
    public bool IsDead()
    {
        return stateController?.IsDead ?? false;
    }

    // ע�⣺TakeDamage ���������"ս����ؽӿ�"����ʵ��

    /// <summary>
    /// ��ȡ��ϸ״̬��Ϣ
    /// </summary>
    public string GetDetailedStatus()
    {
        return $"���: {playerName}\n" +
               $"�ȼ�: {Level} (����: {Experience}/{ExperienceToNext})\n" +
               $"����: {Health}/{MaxHealth}\n" +
               $"����: {Morality}\n" +
               $"����: {Strength}, ���: {Agility}, ����: {Intelligence}\n" +
               $"����: {Stamina}, ����: {Vitality}\n" +
               $"״̬: {CurrentState}";
    }

    // ========== �������� ==========
    T GetOrAddComponent<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
            Debug.Log($"[PlayerController] �Զ�������: {typeof(T).Name}");
        }
        return component;
    }

    // ========== ���Թ��� ==========
    [ContextMenu("�������")]
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

    [ContextMenu("��������")]
    public void DebugLevelUp()
    {
        AddExp(100);
    }

    [ContextMenu("��������")]
    public void DebugTakeDamage()
    {
        TakeDamage(20);
    }

    [ContextMenu("��ȫ����")]
    public void DebugFullHeal()
    {
        Heal(MaxHealth);
    }

    [ContextMenu("��ӡ״̬")]
    public void DebugPrintStatus()
    {
        Debug.Log(GetDetailedStatus());
    }

    void OnGUI()
    {
        if (!enableDebugUI) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 250));
        GUILayout.BeginVertical("box");

        GUILayout.Label($"���: {playerName}", GUI.skin.label);
        GUILayout.Label($"�ȼ�: {Level} | ����: {Health}/{MaxHealth}");
        GUILayout.Label($"����: {Experience}/{ExperienceToNext}");
        GUILayout.Label($"����: {Morality}");
        GUILayout.Label($"״̬: {CurrentState}");
        GUILayout.Label($"�ƶ�: {(IsMoving ? "��" : "��")} | ս��: {(IsInCombat ? "��" : "��")}");

        GUILayout.Space(5);
        GUILayout.Label($"����: {Strength} | ���: {Agility}");
        GUILayout.Label($"����: {Intelligence} | ����: {Stamina}");
        GUILayout.Label($"����: {Vitality}");

        GUILayout.Space(10);

        if (GUILayout.Button("����")) DebugLevelUp();
        if (GUILayout.Button("����")) DebugTakeDamage();
        if (GUILayout.Button("����")) DebugFullHeal();
        if (GUILayout.Button("����")) ResetPlayer();
        if (GUILayout.Button("��ӡ״̬")) DebugPrintStatus();

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        // �����¼���
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