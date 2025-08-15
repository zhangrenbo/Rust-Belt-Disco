using UnityEngine;

/// <summary>
/// ͨ�����Կ����� - ����ȼ������顢�������ԡ�����ֵ�ȣ���������ҡ�NPC���κν�ɫ��
/// </summary>
public class AttributeController : MonoBehaviour
{
    [Header("=== ��ɫ���� ===")]
    public CharacterType characterType = CharacterType.Player;
    public string characterName = "��ɫ";

    [Header("=== �������� ===")]
    public int level = 1;
    public int exp = 0;
    public int expToNextLevel = 100;

    [Header("=== ����ϵͳ ===")]
    public int startingMorality = 100;
    public int currentMorality;

    [Header("=== �������� ===")]
    public int baseStrength = 5;
    public int baseAgility = 5;
    public int baseIntelligence = 5;
    public int baseStamina = 5;
    public int baseVitality = 5;

    [Header("=== ���Գɳ� ===")]
    public int strengthGrowthPerLevel = 1;
    public int agilityGrowthPerLevel = 1;
    public int staminaGrowthPerLevel = 2;
    public float expGrowthRate = 1.2f;

    [Header("=== �������� ===")]
    public bool showDebugInfo = false;

    // �������
    private CharacterStateManager stateManager;
    private StateController stateController;
    private HealthUIController healthUIController;

    // ��ǰ����������
    private int currentStrength;
    private int currentAgility;
    private int currentIntelligence;
    private int currentStamina;
    private int currentVitality;

    // �¼�
    public System.Action<int> OnLevelUp;
    public System.Action<int, int> OnExpChanged; // ��ǰ����, ��Ҫ����
    public System.Action<int> OnMoralityChanged;
    public System.Action<int, int, int, int, int> OnAttributesChanged; // ����, ���, ����, ����, ����
    public System.Action<int, int> OnStageChanged; // �ɽ׶�����, �½׶�����
    public System.Action<string> OnStageTransition; // �½׶�����

    // ���Է�����
    public int Strength => currentStrength;
    public int Agility => currentAgility;
    public int Intelligence => currentIntelligence;
    public int Stamina => currentStamina;
    public int Vitality => currentVitality;

    public int Health => stateManager?.currentHealth ?? 100;
    public int MaxHealth => stateManager?.maxHealth ?? 100;

    // �ȼ��������
    public int Level => level;
    public int Experience => exp;
    public int ExperienceToNext => expToNextLevel;
    public float ExperienceProgress => expToNextLevel > 0 ? (float)exp / expToNextLevel : 1f;

    // ����ֵ����
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
        // ��ʼ������ֵ
        currentMorality = startingMorality;

        // ���㵱ǰ���ԣ��������� + �ȼ��ɳ���
        RecalculateAttributes();

        // ��ʼ��״̬������
        if (stateManager != null)
        {
            stateManager.characterType = characterType;
            stateManager.characterName = characterName;

            // ��������
            stateManager.strength = currentStrength;
            stateManager.agility = currentAgility;
            stateManager.intelligence = currentIntelligence;
            stateManager.stamina = currentStamina;
            stateManager.vitality = currentVitality;

            // ����������������ֵ
            int calculatedMaxHealth = currentStamina * 20;
            stateManager.maxHealth = calculatedMaxHealth;
            stateManager.currentHealth = calculatedMaxHealth;
        }

        // ��ʼ��UI
        if (healthUIController != null)
        {
            healthUIController.SetMaxHealth(MaxHealth);
            healthUIController.SetCurrentHealth(Health);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[AttributeController] ���Գ�ʼ����� - �ȼ�:{level}, ����:{currentStrength}, ���:{currentAgility}, ����:{currentIntelligence}, ����:{currentStamina}, ����:{currentVitality}");
        }
    }

    void RecalculateAttributes()
    {
        // ����ȼ��ɳ��ӳ�
        int levelBonus = level - 1; // 1��ʱû�гɳ��ӳ�

        currentStrength = baseStrength + (levelBonus * strengthGrowthPerLevel);
        currentAgility = baseAgility + (levelBonus * agilityGrowthPerLevel);
        currentIntelligence = baseIntelligence + (levelBonus * 0); // �����ݲ��Զ��ɳ�
        currentStamina = baseStamina + (levelBonus * staminaGrowthPerLevel);
        currentVitality = baseVitality + (levelBonus * 0); // �����ݲ��Զ��ɳ�

        // �������Ա仯�¼�
        OnAttributesChanged?.Invoke(currentStrength, currentAgility, currentIntelligence, currentStamina, currentVitality);
    }

    // ========== ����͵ȼ�ϵͳ ==========

    /// <summary>
    /// ��Ӿ���
    /// </summary>
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        exp += amount;
        OnExpChanged?.Invoke(exp, expToNextLevel);

        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAttributes] ��þ���: {amount}, ��ǰ����: {exp}/{expToNextLevel}");
        }

        // ����Ƿ�����
        while (exp >= expToNextLevel)
        {
            LevelUp();
        }
    }

    /// <summary>
    /// ��������
    /// </summary>
    void LevelUp()
    {
        int oldLevel = level;

        exp -= expToNextLevel;
        level++;

        // ������һ�����辭��
        expToNextLevel = Mathf.RoundToInt(expToNextLevel * expGrowthRate);

        // ���¼�������
        RecalculateAttributes();

        // ����״̬����������
        if (stateManager != null)
        {
            stateManager.strength = currentStrength;
            stateManager.agility = currentAgility;
            stateManager.intelligence = currentIntelligence;
            stateManager.stamina = currentStamina;
            stateManager.vitality = currentVitality;

            // ��������ֵ���޲���ȫ�ָ�
            int newMaxHealth = currentStamina * 20;
            stateManager.maxHealth = newMaxHealth;
            stateManager.currentHealth = newMaxHealth;
        }

        // ����UI
        if (healthUIController != null)
        {
            healthUIController.SetMaxHealth(MaxHealth);
            healthUIController.SetCurrentHealth(Health);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAttributes] ��������ǰ�ȼ�: {level}, ������ - ����:{currentStrength}, ���:{currentAgility}, ����:{currentIntelligence}, ����:{currentStamina}, ����:{currentVitality}");
        }

        // ���������¼�
        OnLevelUp?.Invoke(level);
        OnExpChanged?.Invoke(exp, expToNextLevel);

        // ���׶α仯
        CheckStageChange(oldLevel, level);
    }

    /// <summary>
    /// ���׶α仯
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
                Debug.Log($"[PlayerAttributes] �׶α仯: {oldStage} -> {newStage}");
            }
        }
    }

    // ========== ����ϵͳ ==========

    /// <summary>
    /// �۳�����ֵ
    /// </summary>
    public void DeductMorality(int amount)
    {
        if (amount <= 0) return;

        currentMorality = Mathf.Max(0, currentMorality - amount);
        OnMoralityChanged?.Invoke(currentMorality);

        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAttributes] ����ֵ����: {amount}, ��ǰ����ֵ: {currentMorality}");
        }
    }

    /// <summary>
    /// ���ӵ���ֵ
    /// </summary>
    public void AddMorality(int amount)
    {
        if (amount <= 0) return;

        currentMorality = Mathf.Min(100, currentMorality + amount);
        OnMoralityChanged?.Invoke(currentMorality);

        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAttributes] ����ֵ����: {amount}, ��ǰ����ֵ: {currentMorality}");
        }
    }

    /// <summary>
    /// ���õ���ֵ
    /// </summary>
    public void SetMorality(int value)
    {
        currentMorality = Mathf.Clamp(value, 0, 100);
        OnMoralityChanged?.Invoke(currentMorality);
    }

    // ========== �����޸Ľӿ� ==========

    /// <summary>
    /// ֱ�����õȼ��������ã�
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
            Debug.Log($"[PlayerAttributes] �ȼ�����Ϊ: {level}");
        }
    }

    /// <summary>
    /// �����ʱ���Լӳɣ�ͨ���޸Ļ������ԣ�
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

        // �����仯ʱ��Ҫ���¼�������ֵ
        if (stateManager != null)
        {
            int newMaxHealth = currentStamina * 20;
            int healthDiff = newMaxHealth - stateManager.maxHealth;
            stateManager.maxHealth = newMaxHealth;
            stateManager.currentHealth += healthDiff; // ������ͬ������ֵ����

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

    // ========== �׶���ظ������� ==========

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
        if (level >= 1 && level <= 10) return "�����׶�";
        else if (level >= 11 && level <= 20) return "�м��׶�";
        else if (level >= 21 && level <= 30) return "�߼��׶�";
        else return "��ʦ�׶�";
    }

    int GetStageIndexForLevel(int level)
    {
        if (level >= 1 && level <= 10) return 0;
        else if (level >= 11 && level <= 20) return 1;
        else if (level >= 21 && level <= 30) return 2;
        else return 3;
    }

    // ========== ���Ժͼ��������� ==========

    [ContextMenu("����100����")]
    void DebugAddExp()
    {
        AddExp(100);
    }

    [ContextMenu("��1��")]
    void DebugLevelUp()
    {
        SetLevel(level + 1);
    }

    [ContextMenu("��������")]
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

        GUILayout.Label($"�ȼ�: {level}");
        GUILayout.Label($"����: {exp}/{expToNextLevel} ({ExperienceProgress:P1})");
        GUILayout.Label($"����ֵ: {currentMorality}");
        GUILayout.Space(5);
        GUILayout.Label($"����: {currentStrength}");
        GUILayout.Label($"���: {currentAgility}");
        GUILayout.Label($"����: {currentIntelligence}");
        GUILayout.Label($"����: {currentStamina}");
        GUILayout.Label($"����: {currentVitality}");
        GUILayout.Space(5);
        GUILayout.Label($"����ֵ: {Health}/{MaxHealth}");
        GUILayout.Label($"��ǰ�׶�: {GetCurrentStageName()}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}