using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 动画配置类 - 存储角色的所有动画映射
/// </summary>
[System.Serializable]
public class AnimationConfig
{
    [Header("=== 基础动画 ===")]
    [Tooltip("待机动画 - 下/上/左/右")]
    public string[] idleAnimations = new string[4] { "Idle_Down", "Idle_Up", "Idle_Left", "Idle_Right" };

    [Tooltip("行走动画 - 下/上/左/右")]
    public string[] walkAnimations = new string[4] { "Walk_Down", "Walk_Up", "Walk_Left", "Walk_Right" };

    [Tooltip("奔跑动画 - 下/上/左/右")]
    public string[] runAnimations = new string[4] { "Run_Down", "Run_Up", "Run_Left", "Run_Right" };

    [Header("=== 战斗动画 ===")]
    [Tooltip("攻击动画 - 下/上/左/右")]
    public string[] attackAnimations = new string[4] { "Attack_Down", "Attack_Up", "Attack_Left", "Attack_Right" };

    [Tooltip("受击动画 - 下/上/左/右")]
    public string[] hitAnimations = new string[4] { "Hit_Down", "Hit_Up", "Hit_Left", "Hit_Right" };

    [Tooltip("死亡动画 - 下/上/左/右")]
    public string[] dieAnimations = new string[4] { "Die_Down", "Die_Up", "Die_Left", "Die_Right" };

    [Header("=== 特殊动画 ===")]
    [Tooltip("跳跃动画 - 下/上/左/右")]
    public string[] jumpAnimations = new string[4] { "Jump_Down", "Jump_Up", "Jump_Left", "Jump_Right" };

    [Tooltip("施法动画 - 下/上/左/右")]
    public string[] castAnimations = new string[4] { "Cast_Down", "Cast_Up", "Cast_Left", "Cast_Right" };

    [Header("=== 技能动画 ===")]
    [Tooltip("技能动画列表")]
    public string[] skills = new string[4] { "Skill_1", "Skill_2", "Skill_3", "Skill_4" };

    /// <summary>
    /// 根据动画类型和方向获取动画名称
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
    /// 安全获取动画名称，防止数组越界
    /// </summary>
    private string GetSafeAnimation(string[] animations, int index, string fallback)
    {
        if (animations != null && index >= 0 && index < animations.Length && !string.IsNullOrEmpty(animations[index]))
        {
            return animations[index];
        }

        // 如果指定索引无效，尝试返回第一个有效动画
        if (animations != null && animations.Length > 0 && !string.IsNullOrEmpty(animations[0]))
        {
            return animations[0];
        }

        // 最后的备用方案
        return $"{fallback}_Down";
    }

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        return idleAnimations != null && idleAnimations.Length >= 4 &&
               walkAnimations != null && walkAnimations.Length >= 4;
    }

    /// <summary>
    /// 获取技能动画名称
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
/// 动画管理器 - 全局单例，管理所有动画配置
/// </summary>
public class AnimationManager : Singleton<AnimationManager>
{
    [Header("=== 动画配置 ===")]
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
        // 添加默认配置
        if (defaultConfig != null)
        {
            configDict["Default"] = defaultConfig;
        }

        // 添加命名配置
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
    /// 获取动画配置
    /// </summary>
    public AnimationConfig GetConfig(string configName)
    {
        if (configDict.ContainsKey(configName))
        {
            return configDict[configName];
        }

        Debug.LogWarning($"[AnimationManager] 未找到配置: {configName}，返回默认配置");
        return defaultConfig ?? new AnimationConfig();
    }

    /// <summary>
    /// 添加动画配置
    /// </summary>
    public void AddConfig(string name, AnimationConfig config)
    {
        configDict[name] = config;
    }
}

/// <summary>
/// 角色动画控制器 - 基础版本
/// </summary>
public class CharacterAnimationController : MonoBehaviour
{
    [Header("=== 动画控制器设置 ===")]
    [Tooltip("使用的动画配置名称，对应AnimationManager中的配置")]
    public string animationConfigName = "Default";

    [Header("=== 组件引用 ===")]
    [Tooltip("Animator组件，用于播放动画")]
    public Animator animator;

    [Tooltip("角色状态管理器")]
    public CharacterStateManager characterStateManager;

    [Header("=== 动画设置 ===")]
    public bool autoRegisterToManager = true;
    public float animationBlendTime = 0.1f;

    // 私有变量
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
        // 获取组件引用
        if (animator == null)
            animator = GetComponent<Animator>();

        if (characterStateManager == null)
            characterStateManager = GetComponent<CharacterStateManager>();

        if (characterStateManager == null)
        {
            Debug.LogError($"[CharacterAnimationController] {gameObject.name} 没有找到 CharacterStateManager 组件！");
            enabled = false;
            return;
        }

        // 获取动画配置
        if (AnimationManager.Instance != null)
        {
            config = AnimationManager.Instance.GetConfig(animationConfigName);
        }
        else
        {
            config = new AnimationConfig();
            Debug.LogWarning("[CharacterAnimationController] AnimationManager 不存在，使用默认配置");
        }

        // 播放初始动画
        PlayInitialAnimation();

        Debug.Log($"[CharacterAnimationController] {gameObject.name} 动画控制器初始化完成");
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

    // ========== 核心动画控制逻辑 ==========

    /// <summary>
    /// 响应角色状态变化，自动播放对应动画
    /// </summary>
    void OnCharacterStateChanged(CharacterState oldState, CharacterState newState)
    {
        Debug.Log($"[CharacterAnimationController] {gameObject.name} 状态变化: {oldState} -> {newState}");

        PlayStateAnimation(newState);
        lastState = newState;
    }

    /// <summary>
    /// 响应角色移动变化，更新移动动画方向
    /// </summary>
    void OnCharacterMovementChanged(Vector2 movementDirection)
    {
        lastMovementDirection = movementDirection;

        // 仅当处于移动状态，更新动画方向
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
    /// 核心方法：根据状态播放动画
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
    /// 播放移动动画
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
    /// 获取方向性动画名称
    /// </summary>
    string GetDirectionalAnimation(AnimationType type)
    {
        AnimationDirection direction = GetAnimationDirection(lastMovementDirection);
        return config.GetAnimationName(type, direction);
    }

    /// <summary>
    /// 根据向量获取动画方向
    /// </summary>
    AnimationDirection GetAnimationDirection(Vector2 direction)
    {
        if (direction.magnitude < 0.1f)
            return AnimationDirection.Down; // 默认朝下

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
    /// 最终的动画播放方法
    /// </summary>
    void PlayAnimation(string animationName)
    {
        if (animator == null || string.IsNullOrEmpty(animationName)) return;

        if (currentAnimation != animationName)
        {
            animator.Play(animationName);
            currentAnimation = animationName;

            Debug.Log($"[CharacterAnimationController] {gameObject.name} 播放动画: {animationName}");
        }
    }

    /// <summary>
    /// 延迟后返回待机状态
    /// </summary>
    System.Collections.IEnumerator ReturnToIdleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (characterStateManager != null && characterStateManager.currentState == CharacterState.Hit)
        {
            characterStateManager.SetState(CharacterState.Idle);
        }
    }

    // ========== 公共接口方法 ==========

    /// <summary>
    /// 强制播放指定动画
    /// </summary>
    public void ForcePlayAnimation(string animationName)
    {
        PlayAnimation(animationName);
    }

    /// <summary>
    /// 播放技能动画
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
    /// 手动播放待机动画
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
    /// 手动播放行走动画
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
    /// 手动播放奔跑动画
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