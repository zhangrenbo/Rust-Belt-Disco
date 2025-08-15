using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ���������� - �洢��ɫ�����ж���ӳ��
/// </summary>
[System.Serializable]
public class AnimationConfig
{
    [Header("=== �������� ===")]
    [Tooltip("�������� - ��/��/��/��")]
    public string[] idleAnimations = new string[4] { "Idle_Down", "Idle_Up", "Idle_Left", "Idle_Right" };

    [Tooltip("���߶��� - ��/��/��/��")]
    public string[] walkAnimations = new string[4] { "Walk_Down", "Walk_Up", "Walk_Left", "Walk_Right" };

    [Tooltip("���ܶ��� - ��/��/��/��")]
    public string[] runAnimations = new string[4] { "Run_Down", "Run_Up", "Run_Left", "Run_Right" };

    [Header("=== ս������ ===")]
    [Tooltip("�������� - ��/��/��/��")]
    public string[] attackAnimations = new string[4] { "Attack_Down", "Attack_Up", "Attack_Left", "Attack_Right" };

    [Tooltip("�ܻ����� - ��/��/��/��")]
    public string[] hitAnimations = new string[4] { "Hit_Down", "Hit_Up", "Hit_Left", "Hit_Right" };

    [Tooltip("�������� - ��/��/��/��")]
    public string[] dieAnimations = new string[4] { "Die_Down", "Die_Up", "Die_Left", "Die_Right" };

    [Header("=== ���⶯�� ===")]
    [Tooltip("��Ծ���� - ��/��/��/��")]
    public string[] jumpAnimations = new string[4] { "Jump_Down", "Jump_Up", "Jump_Left", "Jump_Right" };

    [Tooltip("ʩ������ - ��/��/��/��")]
    public string[] castAnimations = new string[4] { "Cast_Down", "Cast_Up", "Cast_Left", "Cast_Right" };

    [Header("=== ���ܶ��� ===")]
    [Tooltip("���ܶ����б�")]
    public string[] skills = new string[4] { "Skill_1", "Skill_2", "Skill_3", "Skill_4" };

    /// <summary>
    /// ���ݶ������ͺͷ����ȡ��������
    /// </summary>
    public string GetAnimationName(AnimationType type, AnimationDirection direction)
    {
        int dirIndex = (int)direction;

        switch (type)
        {
            case AnimationType.Idle:
                return GetSafeAnimation(idleAnimations, dirIndex, "Idle");
            case AnimationType.Walk:
                return GetSafeAnimation(walkAnimations, dirIndex, "Walk");
            case AnimationType.Run:
                return GetSafeAnimation(runAnimations, dirIndex, "Run");
            case AnimationType.Attack:
                return GetSafeAnimation(attackAnimations, dirIndex, "Attack");
            case AnimationType.Hit:
                return GetSafeAnimation(hitAnimations, dirIndex, "Hit");
            case AnimationType.Die:
                return GetSafeAnimation(dieAnimations, dirIndex, "Die");
            case AnimationType.Jump:
                return GetSafeAnimation(jumpAnimations, dirIndex, "Jump");
            case AnimationType.Cast:
                return GetSafeAnimation(castAnimations, dirIndex, "Cast");
            default:
                return GetSafeAnimation(idleAnimations, 0, "Idle");
        }
    }

    /// <summary>
    /// ��ȫ��ȡ�������ƣ���ֹ����Խ��
    /// </summary>
    private string GetSafeAnimation(string[] animations, int index, string fallback)
    {
        if (animations != null && index >= 0 && index < animations.Length && !string.IsNullOrEmpty(animations[index]))
        {
            return animations[index];
        }

        // ���ָ��������Ч�����Է��ص�һ����Ч����
        if (animations != null && animations.Length > 0 && !string.IsNullOrEmpty(animations[0]))
        {
            return animations[0];
        }

        // ���ı��÷���
        return $"{fallback}_Down";
    }

    /// <summary>
    /// ��֤�����Ƿ���Ч
    /// </summary>
    public bool IsValid()
    {
        return idleAnimations != null && idleAnimations.Length >= 4 &&
               walkAnimations != null && walkAnimations.Length >= 4;
    }

    /// <summary>
    /// ��ȡ���ܶ�������
    /// </summary>
    public string GetSkillAnimation(int skillIndex)
    {
        if (skills != null && skillIndex >= 0 && skillIndex < skills.Length)
        {
            return skills[skillIndex];
        }
        return "";
    }
}

/// <summary>
/// ���������� - ȫ�ֵ������������ж�������
/// </summary>
public class AnimationManager : Singleton<AnimationManager>
{
    [Header("=== �������� ===")]
    public AnimationConfig defaultConfig;
    public AnimationConfig[] namedConfigs;

    private Dictionary<string, AnimationConfig> configDict = new Dictionary<string, AnimationConfig>();

    protected override void Awake()
    {
        base.Awake();
        InitializeConfigs();
    }

    void InitializeConfigs()
    {
        // ���Ĭ������
        if (defaultConfig != null)
        {
            configDict["Default"] = defaultConfig;
        }

        // �����������
        if (namedConfigs != null)
        {
            for (int i = 0; i < namedConfigs.Length; i++)
            {
                if (namedConfigs[i] != null)
                {
                    configDict[$"Config_{i}"] = namedConfigs[i];
                }
            }
        }
    }

    /// <summary>
    /// ��ȡ��������
    /// </summary>
    public AnimationConfig GetConfig(string configName)
    {
        if (configDict.ContainsKey(configName))
        {
            return configDict[configName];
        }

        Debug.LogWarning($"[AnimationManager] δ�ҵ�����: {configName}������Ĭ������");
        return defaultConfig ?? new AnimationConfig();
    }

    /// <summary>
    /// ��Ӷ�������
    /// </summary>
    public void AddConfig(string name, AnimationConfig config)
    {
        configDict[name] = config;
    }
}

/// <summary>
/// ��ɫ���������� - �����汾
/// </summary>
public class CharacterAnimationController : MonoBehaviour
{
    [Header("=== �������������� ===")]
    [Tooltip("ʹ�õĶ����������ƣ���ӦAnimationManager�е�����")]
    public string animationConfigName = "Default";

    [Header("=== ������� ===")]
    [Tooltip("Animator��������ڲ��Ŷ���")]
    public Animator animator;

    [Tooltip("��ɫ״̬������")]
    public CharacterStateManager characterStateManager;

    [Header("=== �������� ===")]
    public bool autoRegisterToManager = true;
    public float animationBlendTime = 0.1f;

    // ˽�б���
    private AnimationConfig config;
    private string currentAnimation = "";
    private CharacterState lastState = CharacterState.Idle;
    private Vector2 lastMovementDirection = Vector2.zero;

    void Start()
    {
        InitializeAnimationController();
        RegisterToCharacterEvents();
    }

    void OnDestroy()
    {
        UnregisterFromCharacterEvents();
    }

    void InitializeAnimationController()
    {
        // ��ȡ�������
        if (animator == null)
            animator = GetComponent<Animator>();

        if (characterStateManager == null)
            characterStateManager = GetComponent<CharacterStateManager>();

        if (characterStateManager == null)
        {
            Debug.LogError($"[CharacterAnimationController] {gameObject.name} û���ҵ� CharacterStateManager �����");
            enabled = false;
            return;
        }

        // ��ȡ��������
        if (AnimationManager.Instance != null)
        {
            config = AnimationManager.Instance.GetConfig(animationConfigName);
        }
        else
        {
            config = new AnimationConfig();
            Debug.LogWarning("[CharacterAnimationController] AnimationManager �����ڣ�ʹ��Ĭ������");
        }

        // ���ų�ʼ����
        PlayInitialAnimation();

        Debug.Log($"[CharacterAnimationController] {gameObject.name} ������������ʼ�����");
    }

    void RegisterToCharacterEvents()
    {
        if (characterStateManager != null)
        {
            characterStateManager.OnStateChanged += OnCharacterStateChanged;
            characterStateManager.OnMovementChanged += OnCharacterMovementChanged;
        }
    }

    void UnregisterFromCharacterEvents()
    {
        if (characterStateManager != null)
        {
            characterStateManager.OnStateChanged -= OnCharacterStateChanged;
            characterStateManager.OnMovementChanged -= OnCharacterMovementChanged;
        }
    }

    // ========== ���Ķ��������߼� ==========

    /// <summary>
    /// ��Ӧ��ɫ״̬�仯���Զ����Ŷ�Ӧ����
    /// </summary>
    void OnCharacterStateChanged(CharacterState oldState, CharacterState newState)
    {
        Debug.Log($"[CharacterAnimationController] {gameObject.name} ״̬�仯: {oldState} -> {newState}");

        PlayStateAnimation(newState);
        lastState = newState;
    }

    /// <summary>
    /// ��Ӧ��ɫ�ƶ��仯�������ƶ���������
    /// </summary>
    void OnCharacterMovementChanged(Vector2 movementDirection)
    {
        lastMovementDirection = movementDirection;

        // ���������ƶ�״̬�����¶�������
        if (characterStateManager.currentState == CharacterState.Walking ||
            characterStateManager.currentState == CharacterState.Running)
        {
            PlayMovementAnimation(characterStateManager.currentState, movementDirection);
        }
    }

    void PlayInitialAnimation()
    {
        PlayStateAnimation(CharacterState.Idle);
    }

    /// <summary>
    /// ���ķ���������״̬���Ŷ���
    /// </summary>
    void PlayStateAnimation(CharacterState state)
    {
        string animationName = "";

        switch (state)
        {
            case CharacterState.Idle:
                animationName = GetDirectionalAnimation(AnimationType.Idle);
                break;

            case CharacterState.Walking:
                animationName = GetDirectionalAnimation(AnimationType.Walk);
                break;

            case CharacterState.Running:
                animationName = GetDirectionalAnimation(AnimationType.Run);
                break;

            case CharacterState.Attacking:
                animationName = config.GetAnimationName(AnimationType.Attack, AnimationDirection.Down);
                break;

            case CharacterState.Hit:
                animationName = config.GetAnimationName(AnimationType.Hit, AnimationDirection.Down);
                StartCoroutine(ReturnToIdleAfterDelay(0.5f));
                break;

            case CharacterState.Dead:
                animationName = config.GetAnimationName(AnimationType.Die, AnimationDirection.Down);
                break;

            case CharacterState.Dialogue:
                animationName = GetDirectionalAnimation(AnimationType.Idle);
                break;

            case CharacterState.Jumping:
                animationName = GetDirectionalAnimation(AnimationType.Jump);
                break;

            case CharacterState.Casting:
                animationName = GetDirectionalAnimation(AnimationType.Cast);
                break;
        }

        PlayAnimation(animationName);
    }

    /// <summary>
    /// �����ƶ�����
    /// </summary>
    void PlayMovementAnimation(CharacterState movementState, Vector2 direction)
    {
        AnimationType animType = movementState switch
        {
            CharacterState.Walking => AnimationType.Walk,
            CharacterState.Running => AnimationType.Run,
            _ => AnimationType.Idle
        };

        AnimationDirection animDirection = GetAnimationDirection(direction);
        string animationName = config.GetAnimationName(animType, animDirection);

        PlayAnimation(animationName);
    }

    /// <summary>
    /// ��ȡ�����Զ�������
    /// </summary>
    string GetDirectionalAnimation(AnimationType type)
    {
        AnimationDirection direction = GetAnimationDirection(lastMovementDirection);
        return config.GetAnimationName(type, direction);
    }

    /// <summary>
    /// ����������ȡ��������
    /// </summary>
    AnimationDirection GetAnimationDirection(Vector2 direction)
    {
        if (direction.magnitude < 0.1f)
            return AnimationDirection.Down; // Ĭ�ϳ���

        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            return direction.y > 0 ? AnimationDirection.Up : AnimationDirection.Down;
        }
        else
        {
            return direction.x > 0 ? AnimationDirection.Right : AnimationDirection.Left;
        }
    }

    /// <summary>
    /// ���յĶ������ŷ���
    /// </summary>
    void PlayAnimation(string animationName)
    {
        if (animator == null || string.IsNullOrEmpty(animationName)) return;

        if (currentAnimation != animationName)
        {
            animator.Play(animationName);
            currentAnimation = animationName;

            Debug.Log($"[CharacterAnimationController] {gameObject.name} ���Ŷ���: {animationName}");
        }
    }

    /// <summary>
    /// �ӳٺ󷵻ش���״̬
    /// </summary>
    System.Collections.IEnumerator ReturnToIdleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (characterStateManager != null && characterStateManager.currentState == CharacterState.Hit)
        {
            characterStateManager.SetState(CharacterState.Idle);
        }
    }

    // ========== �����ӿڷ��� ==========

    /// <summary>
    /// ǿ�Ʋ���ָ������
    /// </summary>
    public void ForcePlayAnimation(string animationName)
    {
        PlayAnimation(animationName);
    }

    /// <summary>
    /// ���ż��ܶ���
    /// </summary>
    public void PlaySkillAnimation(int skillIndex)
    {
        if (config != null && skillIndex >= 0 && skillIndex < config.skills.Length)
        {
            string skillAnimation = config.skills[skillIndex];
            if (!string.IsNullOrEmpty(skillAnimation))
            {
                PlayAnimation(skillAnimation);
            }
        }
    }

    /// <summary>
    /// �ֶ����Ŵ�������
    /// </summary>
    public void PlayIdleAnimation(string direction = "Down")
    {
        AnimationDirection dir = direction switch
        {
            "Up" => AnimationDirection.Up,
            "Down" => AnimationDirection.Down,
            "Left" => AnimationDirection.Left,
            "Right" => AnimationDirection.Right,
            _ => AnimationDirection.Down
        };

        string animName = config.GetAnimationName(AnimationType.Idle, dir);
        PlayAnimation(animName);
    }

    /// <summary>
    /// �ֶ��������߶���
    /// </summary>
    public void PlayWalkAnimation(string direction = "Down")
    {
        AnimationDirection dir = direction switch
        {
            "Up" => AnimationDirection.Up,
            "Down" => AnimationDirection.Down,
            "Left" => AnimationDirection.Left,
            "Right" => AnimationDirection.Right,
            _ => AnimationDirection.Down
        };

        string animName = config.GetAnimationName(AnimationType.Walk, dir);
        PlayAnimation(animName);
    }

    /// <summary>
    /// �ֶ����ű��ܶ���
    /// </summary>
    public void PlayRunAnimation(string direction = "Down")
    {
        AnimationDirection dir = direction switch
        {
            "Up" => AnimationDirection.Up,
            "Down" => AnimationDirection.Down,
            "Left" => AnimationDirection.Left,
            "Right" => AnimationDirection.Right,
            _ => AnimationDirection.Down
        };

        string animName = config.GetAnimationName(AnimationType.Run, dir);
        PlayAnimation(animName);
    }
}