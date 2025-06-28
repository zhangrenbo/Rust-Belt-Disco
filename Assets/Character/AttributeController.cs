using UnityEngine;

/// <summary>
/// 通用属性控制器 - 管理等级、经验、基础属性、道德值等（可用于玩家、NPC等任何角色）
/// </summary>
public class AttributeController : MonoBehaviour
{
    [Header("=== 角色类型 ===")]
    public CharacterType characterType = CharacterType.Player;
    public string characterName = "角色";

    [Header("=== 基本属性 ===")]
    public int level = 1;
    public int exp = 0;
    public int expToNextLevel = 100;

    [Header("=== 道德系统 ===")]
    public int startingMorality = 100;
    public int currentMorality;

    [Header("=== 基础属性 ===")]
    public int baseStrength = 5;
    public int baseAgility = 5;
    public int baseIntelligence = 5;
    public int baseStamina = 5;
    public int baseVitality = 5;

    [Header("=== 属性成长 ===")]
    public int strengthGrowthPerLevel = 1;
    public int agilityGrowthPerLevel = 1;
    public int staminaGrowthPerLevel = 2;
    public float expGrowthRate = 1.2f;

    [Header("=== 调试设置 ===")]
    public bool showDebugInfo = false;

    // 组件引用
    private CharacterStateManager stateManager;
    private StateController stateController;
    private HealthUIController healthUIController;

    // 当前计算后的属性
    private int currentStrength;
    private int currentAgility;
    private int currentIntelligence;
    private int currentStamina;
    private int currentVitality;

    // 事件
    public System.Action<int> OnLevelUp;
    public System.Action<int, int> OnExpChanged; // 当前经验, 需要经验
    public System.Action<int> OnMoralityChanged;
    public System.Action<int, int, int, int, int> OnAttributesChanged; // 力量, 敏捷, 智力, 耐力, 活力
    public System.Action<int, int> OnStageChanged; // 旧阶段索引, 新阶段索引
    public System.Action<string> OnStageTransition; // 新阶段名称

    // 属性访问器
    public int Strength => currentStrength;
    public int Agility => currentAgility;
    public int Intelligence => currentIntelligence;
    public int Stamina => currentStamina;
    public int Vitality => currentVitality;

    public int Health => stateManager?.currentHealth ?? 100;
    public int MaxHealth => stateManager?.maxHealth ?? 100;

    // 等级相关属性
    public int Level => level;
    public int Experience => exp;
    public int ExperienceToNext => expToNextLevel;
    public float ExperienceProgress => expToNextLevel > 0 ? (float)exp / expToNextLevel : 1f;

    // 道德值属性
    public int Morality => currentMorality;

    void Awake()
    {
        stateManager = GetComponent<CharacterStateManager>();
        stateController = GetComponent<StateController>();
        healthUIController = GetComponentInChildren<HealthUIController>();
    }

    void Start()
    {
        InitializeAttributes();
    }

    void InitializeAttributes()
    {
        // 初始化道德值
        currentMorality = startingMorality;

        // 计算当前属性（基础属性 + 等级成长）
        RecalculateAttributes();

        // 初始化状态管理器
        if (stateManager != null)
        {
            stateManager.characterType = characterType;
            stateManager.characterName = characterName;

            // 设置属性
            stateManager.strength = currentStrength;
            stateManager.agility = currentAgility;
            stateManager.intelligence = currentIntelligence;
            stateManager.stamina = currentStamina;
            stateManager.vitality = currentVitality;

            // 根据耐力计算生命值
            int calculatedMaxHealth = currentStamina * 20;
            stateManager.maxHealth = calculatedMaxHealth;
            stateManager.currentHealth = calculatedMaxHealth;
        }

        // 初始化UI
        if (healthUIController != null)
        {
            healthUIController.SetMaxHealth(MaxHealth);
            healthUIController.SetCurrentHealth(Health);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[AttributeController] 属性初始化完成 - 等级:{level}, 力量:{currentStrength}, 敏捷:{currentAgility}, 智力:{currentIntelligence}, 耐力:{currentStamina}, 活力:{currentVitality}");
        }
    }

    void RecalculateAttributes()
    {
        // 计算等级成长加成
        int levelBonus = level - 1; // 1级时没有成长加成

        currentStrength = baseStrength + (levelBonus * strengthGrowthPerLevel);
        currentAgility = baseAgility + (levelBonus * agilityGrowthPerLevel);
        currentIntelligence = baseIntelligence + (levelBonus * 0); // 智力暂不自动成长
        currentStamina = baseStamina + (levelBonus * staminaGrowthPerLevel);
        currentVitality = baseVitality + (levelBonus * 0); // 活力暂不自动成长

        // 触发属性变化事件
        OnAttributesChanged?.Invoke(currentStrength, currentAgility, currentIntelligence, currentStamina, currentVitality);
    }

    // ========== 经验和等级系统 ==========

    /// <summary>
    /// 添加经验
    /// </summary>
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        exp += amount;
        OnExpChanged?.Invoke(exp, expToNextLevel);

        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAttributes] 获得经验: {amount}, 当前经验: {exp}/{expToNextLevel}");
        }

        // 检查是否升级
        while (exp >= expToNextLevel)
        {
            LevelUp();
        }
    }

    /// <summary>
    /// 升级处理
    /// </summary>
    void LevelUp()
    {
        int oldLevel = level;

        exp -= expToNextLevel;
        level++;

        // 计算下一级所需经验
        expToNextLevel = Mathf.RoundToInt(expToNextLevel * expGrowthRate);

        // 重新计算属性
        RecalculateAttributes();

        // 更新状态管理器属性
        if (stateManager != null)
        {
            stateManager.strength = currentStrength;
            stateManager.agility = currentAgility;
            stateManager.intelligence = currentIntelligence;
            stateManager.stamina = currentStamina;
            stateManager.vitality = currentVitality;

            // 更新生命值上限并完全恢复
            int newMaxHealth = currentStamina * 20;
            stateManager.maxHealth = newMaxHealth;
            stateManager.currentHealth = newMaxHealth;
        }

        // 更新UI
        if (healthUIController != null)
        {
            healthUIController.SetMaxHealth(MaxHealth);
            healthUIController.SetCurrentHealth(Health);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAttributes] 升级！当前等级: {level}, 新属性 - 力量:{currentStrength}, 敏捷:{currentAgility}, 智力:{currentIntelligence}, 耐力:{currentStamina}, 活力:{currentVitality}");
        }

        // 触发升级事件
        OnLevelUp?.Invoke(level);
        OnExpChanged?.Invoke(exp, expToNextLevel);

        // 检查阶段变化
        CheckStageChange(oldLevel, level);
    }

    /// <summary>
    /// 检查阶段变化
    /// </summary>
    void CheckStageChange(int oldLevel, int newLevel)
    {
        string oldStage = GetStageNameForLevel(oldLevel);
        string newStage = GetStageNameForLevel(newLevel);

        if (oldStage != newStage)
        {
            int oldStageIndex = GetStageIndexForLevel(oldLevel);
            int newStageIndex = GetStageIndexForLevel(newLevel);

            OnStageChanged?.Invoke(oldStageIndex, newStageIndex);
            OnStageTransition?.Invoke(newStage);

            if (showDebugInfo)
            {
                Debug.Log($"[PlayerAttributes] 阶段变化: {oldStage} -> {newStage}");
            }
        }
    }

    // ========== 道德系统 ==========

    /// <summary>
    /// 扣除道德值
    /// </summary>
    public void DeductMorality(int amount)
    {
        if (amount <= 0) return;

        currentMorality = Mathf.Max(0, currentMorality - amount);
        OnMoralityChanged?.Invoke(currentMorality);

        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAttributes] 道德值减少: {amount}, 当前道德值: {currentMorality}");
        }
    }

    /// <summary>
    /// 增加道德值
    /// </summary>
    public void AddMorality(int amount)
    {
        if (amount <= 0) return;

        currentMorality = Mathf.Min(100, currentMorality + amount);
        OnMoralityChanged?.Invoke(currentMorality);

        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAttributes] 道德值增加: {amount}, 当前道德值: {currentMorality}");
        }
    }

    /// <summary>
    /// 设置道德值
    /// </summary>
    public void SetMorality(int value)
    {
        currentMorality = Mathf.Clamp(value, 0, 100);
        OnMoralityChanged?.Invoke(currentMorality);
    }

    // ========== 属性修改接口 ==========

    /// <summary>
    /// 直接设置等级（调试用）
    /// </summary>
    public void SetLevel(int newLevel)
    {
        if (newLevel <= 0) return;

        int oldLevel = level;
        level = newLevel;

        RecalculateAttributes();
        CheckStageChange(oldLevel, level);

        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAttributes] 等级设置为: {level}");
        }
    }

    /// <summary>
    /// 添加临时属性加成（通过修改基础属性）
    /// </summary>
    public void AddTemporaryStrength(int amount)
    {
        baseStrength += amount;
        RecalculateAttributes();
        UpdateStateManagerAttributes();
    }

    public void AddTemporaryAgility(int amount)
    {
        baseAgility += amount;
        RecalculateAttributes();
        UpdateStateManagerAttributes();
    }

    public void AddTemporaryIntelligence(int amount)
    {
        baseIntelligence += amount;
        RecalculateAttributes();
        UpdateStateManagerAttributes();
    }

    public void AddTemporaryStamina(int amount)
    {
        baseStamina += amount;
        RecalculateAttributes();
        UpdateStateManagerAttributes();

        // 耐力变化时需要重新计算生命值
        if (stateManager != null)
        {
            int newMaxHealth = currentStamina * 20;
            int healthDiff = newMaxHealth - stateManager.maxHealth;
            stateManager.maxHealth = newMaxHealth;
            stateManager.currentHealth += healthDiff; // 保持相同的生命值比例

            if (healthUIController != null)
            {
                healthUIController.SetMaxHealth(MaxHealth);
                healthUIController.SetCurrentHealth(Health);
            }
        }
    }

    public void AddTemporaryVitality(int amount)
    {
        baseVitality += amount;
        RecalculateAttributes();
        UpdateStateManagerAttributes();
    }

    void UpdateStateManagerAttributes()
    {
        if (stateManager != null)
        {
            stateManager.strength = currentStrength;
            stateManager.agility = currentAgility;
            stateManager.intelligence = currentIntelligence;
            stateManager.stamina = currentStamina;
            stateManager.vitality = currentVitality;
        }
    }

    // ========== 阶段相关辅助方法 ==========

    public string GetCurrentStageName()
    {
        return GetStageNameForLevel(level);
    }

    public int GetCurrentStageIndex()
    {
        return GetStageIndexForLevel(level);
    }

    string GetStageNameForLevel(int level)
    {
        if (level >= 1 && level <= 10) return "初级阶段";
        else if (level >= 11 && level <= 20) return "中级阶段";
        else if (level >= 21 && level <= 30) return "高级阶段";
        else return "大师阶段";
    }

    int GetStageIndexForLevel(int level)
    {
        if (level >= 1 && level <= 10) return 0;
        else if (level >= 11 && level <= 20) return 1;
        else if (level >= 21 && level <= 30) return 2;
        else return 3;
    }

    // ========== 调试和检视器方法 ==========

    [ContextMenu("增加100经验")]
    void DebugAddExp()
    {
        AddExp(100);
    }

    [ContextMenu("升1级")]
    void DebugLevelUp()
    {
        SetLevel(level + 1);
    }

    [ContextMenu("重置属性")]
    void DebugResetAttributes()
    {
        level = 1;
        exp = 0;
        expToNextLevel = 100;
        currentMorality = startingMorality;
        InitializeAttributes();
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 250, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label($"等级: {level}");
        GUILayout.Label($"经验: {exp}/{expToNextLevel} ({ExperienceProgress:P1})");
        GUILayout.Label($"道德值: {currentMorality}");
        GUILayout.Space(5);
        GUILayout.Label($"力量: {currentStrength}");
        GUILayout.Label($"敏捷: {currentAgility}");
        GUILayout.Label($"智力: {currentIntelligence}");
        GUILayout.Label($"耐力: {currentStamina}");
        GUILayout.Label($"活力: {currentVitality}");
        GUILayout.Space(5);
        GUILayout.Label($"生命值: {Health}/{MaxHealth}");
        GUILayout.Label($"当前阶段: {GetCurrentStageName()}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}