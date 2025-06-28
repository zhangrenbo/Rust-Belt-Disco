using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BUFF类型枚举 - 定义所有BUFF相关的类型定义
/// </summary>
public enum BuffType
{
    Attack,         // 攻击力
    AttackSpeed,    // 攻击速度
    SpellPower,     // 法术强度
    Slow,           // 减速
    Vulnerable,     // 易伤
    Poison,         // 中毒
    Defense,        // 防御力
    MoveSpeed,      // 移动速度
    Health,         // 生命值
    Mana            // 法力值
}

/// <summary>
/// 通用的BUFF类 - 替代原有的GameBuff和Buff
/// </summary>
[System.Serializable]
public class Buff
{
    [Header("BUFF基本信息")]
    public string buffName;         // BUFF名称
    public BuffType type;           // BUFF类型
    public float value;             // BUFF数值
    public float duration;          // 持续时间
    public bool isPositive = true;  // 是否为正面效果

    [Header("BUFF效果设置")]
    public bool stackable = false;  // 是否可叠加
    public int maxStacks = 1;       // 最大叠加层数
    public float tickInterval = 0f; // 跳动间隔（用于持续伤害等）

    public Buff()
    {
        buffName = "默认BUFF";
        type = BuffType.Attack;
        value = 0f;
        duration = 10f;
        isPositive = true;
    }

    public Buff(BuffType buffType, float buffValue, float buffDuration, bool positive = true)
    {
        buffName = buffType.ToString();
        type = buffType;
        value = buffValue;
        duration = buffDuration;
        isPositive = positive;
        stackable = false;
        maxStacks = 1;
        tickInterval = 0f;
    }

    public Buff(string name, BuffType buffType, float buffValue, float buffDuration, bool positive = true)
    {
        buffName = name;
        type = buffType;
        value = buffValue;
        duration = buffDuration;
        isPositive = positive;
        stackable = false;
        maxStacks = 1;
        tickInterval = 0f;
    }

    /// <summary>
    /// 创建BUFF的便捷方法
    /// </summary>
    public static Buff Create(BuffType type, float value, float duration, bool positive = true)
    {
        return new Buff(type, value, duration, positive);
    }

    /// <summary>
    /// 从字符串名称创建BUFF（兼容接口）
    /// </summary>
    public static Buff CreateFromString(string buffName, float value, float duration, bool positive = true)
    {
        BuffType buffType = BuffType.Attack; // 默认类型

        // 根据名称推断BUFF类型
        switch (buffName.ToLower())
        {
            case "attack": buffType = BuffType.Attack; break;
            case "attackspeed": buffType = BuffType.AttackSpeed; break;
            case "spellpower": buffType = BuffType.SpellPower; break;
            case "slow": buffType = BuffType.Slow; break;
            case "vulnerable": buffType = BuffType.Vulnerable; break;
            case "poison": buffType = BuffType.Poison; break;
            case "defense": buffType = BuffType.Defense; break;
            case "movespeed": buffType = BuffType.MoveSpeed; break;
            case "health": buffType = BuffType.Health; break;
            case "mana": buffType = BuffType.Mana; break;
        }

        return new Buff(buffName, buffType, value, duration, positive);
    }

    /// <summary>
    /// 复制BUFF（深拷贝）
    /// </summary>
    public Buff Clone()
    {
        var newBuff = new Buff(buffName, type, value, duration, isPositive);
        newBuff.stackable = stackable;
        newBuff.maxStacks = maxStacks;
        newBuff.tickInterval = tickInterval;
        return newBuff;
    }
}

/// <summary>
/// GameBuff别名 - 保持向后兼容性
/// </summary>
[System.Serializable]
public class GameBuff : Buff
{
    public GameBuff() : base() { }
    public GameBuff(BuffType buffType, float buffValue, float buffDuration, bool positive = true)
        : base(buffType, buffValue, buffDuration, positive) { }
    public GameBuff(string name, BuffType buffType, float buffValue, float buffDuration, bool positive = true)
        : base(name, buffType, buffValue, buffDuration, positive) { }
}

/// <summary>
/// 通用BUFF控制器 - 管理所有正面/负面效果和临时状态
/// </summary>
public class BuffController : MonoBehaviour
{
    [Header("=== 角色类型 ===")]
    public CharacterType characterType = CharacterType.Player;

    [Header("=== BUFF设置 ===")]
    public bool enableBuffVisualEffects = true;
    public float buffCheckInterval = 0.1f; // BUFF更新频率
    public int maxBuffCount = 20; // 最大BUFF数量

    [Header("=== 视觉效果 ===")]
    public GameObject buffEffectPrefab; // BUFF效果预制体
    public Transform buffEffectParent; // BUFF效果挂载点

    [Header("=== 调试设置 ===")]
    public bool showDebugInfo = false;
    public bool showBuffUI = true;

    // 组件引用
    private CharacterStateManager stateManager;
    private AttributeController attributeController;
    private MovementController movementController;

    // BUFF管理 - 使用通用的Buff类型
    private List<Buff> activeBuffs = new List<Buff>();
    private List<GameObject> buffEffects = new List<GameObject>();
    private float lastBuffCheckTime = 0f;

    // BUFF缓存系统
    private Dictionary<BuffType, float> buffValueCache = new Dictionary<BuffType, float>();
    private Dictionary<BuffType, float> buffMultiplierCache = new Dictionary<BuffType, float>();
    private Dictionary<string, float> stringBuffValueCache = new Dictionary<string, float>(); // 兼容接口
    private bool cacheNeedsUpdate = true;

    // 事件
    public System.Action<Buff> OnBuffAdded;
    public System.Action<Buff> OnBuffRemoved;
    public System.Action<Buff> OnBuffExpired;
    public System.Action OnBuffCacheUpdated;

    // 属性访问器
    public int ActiveBuffCount => activeBuffs.Count;
    public bool HasAnyBuffs => activeBuffs.Count > 0;
    public List<Buff> ActiveBuffs => new List<Buff>(activeBuffs);

    void Awake()
    {
        stateManager = GetComponent<CharacterStateManager>();
        attributeController = GetComponent<AttributeController>();
        movementController = GetComponent<MovementController>();
    }

    void Start()
    {
        InitializeBuffSystem();
    }

    void Update()
    {
        UpdateBuffs();
        UpdateBuffCache();
    }

    void InitializeBuffSystem()
    {
        // 设置BUFF效果挂载点
        if (buffEffectParent == null)
        {
            buffEffectParent = transform;
        }

        // 初始化BUFF缓存
        InitializeBuffCache();

        if (showDebugInfo)
        {
            Debug.Log("[BuffController] BUFF系统初始化完成");
        }
    }

    void InitializeBuffCache()
    {
        buffValueCache.Clear();
        buffMultiplierCache.Clear();
        stringBuffValueCache.Clear();

        // 初始化所有BUFF类型的缓存
        foreach (BuffType buffType in System.Enum.GetValues(typeof(BuffType)))
        {
            buffValueCache[buffType] = 0f;
            buffMultiplierCache[buffType] = 1f;
        }

        // 初始化字符串缓存（向后兼容）
        string[] buffTypes = { "Attack", "AttackSpeed", "SpellPower", "Slow", "Vulnerable", "Poison", "Defense", "MoveSpeed", "Health", "Mana" };
        foreach (string buffType in buffTypes)
        {
            stringBuffValueCache[buffType] = 0f;
        }

        cacheNeedsUpdate = true;
    }

    void UpdateBuffs()
    {
        if (Time.time - lastBuffCheckTime < buffCheckInterval) return;

        lastBuffCheckTime = Time.time;
        bool anyBuffExpired = false;

        // 倒序遍历，安全移除过期BUFF
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = activeBuffs[i];
            buff.duration -= buffCheckInterval;

            // 处理跳动效果（如持续伤害）
            if (buff.tickInterval > 0)
            {
                HandleBuffTick(buff);
            }

            if (buff.duration <= 0)
            {
                RemoveBuffAt(i);
                anyBuffExpired = true;
            }
        }

        if (anyBuffExpired)
        {
            cacheNeedsUpdate = true;
        }
    }

    void UpdateBuffCache()
    {
        if (!cacheNeedsUpdate) return;

        // 重新计算所有BUFF效果
        InitializeBuffCache();

        foreach (var buff in activeBuffs)
        {
            // 更新枚举类型缓存
            switch (buff.type)
            {
                case BuffType.Attack:
                case BuffType.AttackSpeed:
                case BuffType.SpellPower:
                case BuffType.Defense:
                case BuffType.Health:
                case BuffType.Mana:
                    buffValueCache[buff.type] += buff.isPositive ? buff.value : -buff.value;
                    break;

                case BuffType.Slow:
                case BuffType.MoveSpeed:
                    float speedEffect = buff.isPositive ? (1f + buff.value / 100f) : (1f - buff.value / 100f);
                    buffMultiplierCache[buff.type] *= speedEffect;
                    break;

                case BuffType.Vulnerable:
                    float damageEffect = buff.isPositive ? (1f - buff.value / 100f) : (1f + buff.value / 100f);
                    buffMultiplierCache[buff.type] *= damageEffect;
                    break;

                case BuffType.Poison:
                    buffValueCache[buff.type] += buff.value;
                    break;
            }

            // 更新字符串缓存（向后兼容）
            string buffTypeName = buff.type.ToString();
            if (stringBuffValueCache.ContainsKey(buffTypeName))
            {
                if (buff.isPositive)
                {
                    stringBuffValueCache[buffTypeName] += buff.value;
                }
                else
                {
                    stringBuffValueCache[buffTypeName] -= buff.value;
                }
            }

            // 也支持自定义名称的缓存
            if (!string.IsNullOrEmpty(buff.buffName) && buff.buffName != buff.type.ToString())
            {
                if (!stringBuffValueCache.ContainsKey(buff.buffName))
                {
                    stringBuffValueCache[buff.buffName] = 0f;
                }

                if (buff.isPositive)
                {
                    stringBuffValueCache[buff.buffName] += buff.value;
                }
                else
                {
                    stringBuffValueCache[buff.buffName] -= buff.value;
                }
            }
        }

        // 确保速度倍数不低于10%
        buffMultiplierCache[BuffType.Slow] = Mathf.Max(0.1f, buffMultiplierCache[BuffType.Slow]);
        buffMultiplierCache[BuffType.MoveSpeed] = Mathf.Max(0.1f, buffMultiplierCache[BuffType.MoveSpeed]);

        cacheNeedsUpdate = false;
        OnBuffCacheUpdated?.Invoke();
    }

    void HandleBuffTick(Buff buff)
    {
        // 处理持续效果，如中毒伤害
        if (buff.type == BuffType.Poison)
        {
            ApplyPoisonTick(buff);
        }
        // 可以添加其他持续效果的处理
    }

    void ApplyPoisonTick(Buff poisonBuff)
    {
        if (stateManager != null)
        {
            int poisonDamage = Mathf.RoundToInt(poisonBuff.value);
            stateManager.TakeDamage(poisonDamage);

            if (showDebugInfo)
            {
                Debug.Log($"[BuffController] 中毒伤害: {poisonDamage}");
            }
        }
    }

    // ========== BUFF管理接口 ==========

    /// <summary>
    /// 添加BUFF - 新版接口
    /// </summary>
    public bool AddBuff(Buff buff)
    {
        if (buff == null) return false;

        if (activeBuffs.Count >= maxBuffCount)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[BuffController] BUFF数量已达上限: {maxBuffCount}");
            }
            return false;
        }

        // 检查是否已存在同类型BUFF
        var existingBuff = FindBuff(buff.type);
        if (existingBuff != null)
        {
            if (ShouldStackBuff(buff, existingBuff))
            {
                StackBuff(buff, existingBuff);
            }
            else
            {
                ReplaceBuff(buff, existingBuff);
            }
        }
        else
        {
            activeBuffs.Add(buff);
            CreateBuffEffect(buff);

            if (showDebugInfo)
            {
                Debug.Log($"[BuffController] 添加BUFF: {buff.buffName}, 数值: {buff.value}, 持续: {buff.duration}秒");
            }
        }

        ApplyBuffEffect(buff);
        cacheNeedsUpdate = true;
        OnBuffAdded?.Invoke(buff);

        // 通知角色状态管理器
        if (stateManager != null)
        {
            stateManager.AddBuff(buff);
        }

        return true;
    }

    /// <summary>
    /// 添加GameBuff - 兼容接口
    /// </summary>
    public bool AddBuff(GameBuff gameBuff)
    {
        if (gameBuff == null) return false;

        // 创建Buff副本避免引用问题
        var buff = gameBuff.Clone();
        return AddBuff(buff);
    }

    /// <summary>
    /// 移除指定BUFF
    /// </summary>
    public bool RemoveBuff(Buff buff)
    {
        int index = activeBuffs.FindIndex(b => b == buff);
        if (index >= 0)
        {
            RemoveBuffAt(index);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 移除指定类型的所有BUFF
    /// </summary>
    public int RemoveBuffsByType(BuffType buffType)
    {
        int removedCount = 0;

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].type == buffType)
            {
                RemoveBuffAt(i);
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            cacheNeedsUpdate = true;
            if (showDebugInfo)
            {
                Debug.Log($"[BuffController] 移除了 {removedCount} 个 {buffType} 类型的BUFF");
            }
        }

        return removedCount;
    }

    /// <summary>
    /// 清除所有BUFF
    /// </summary>
    public void ClearAllBuffs()
    {
        int buffCount = activeBuffs.Count;

        // 清除所有BUFF效果
        foreach (var effect in buffEffects)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }
        buffEffects.Clear();

        // 移除BUFF效果
        foreach (var buff in activeBuffs)
        {
            RemoveBuffEffect(buff);
        }

        activeBuffs.Clear();
        cacheNeedsUpdate = true;

        // 通知角色状态管理器
        if (stateManager != null)
        {
            stateManager.ClearAllBuffs();
        }

        if (showDebugInfo)
        {
            Debug.Log($"[BuffController] 清除了所有BUFF，共 {buffCount} 个");
        }
    }

    void RemoveBuffAt(int index)
    {
        if (index < 0 || index >= activeBuffs.Count) return;

        var buff = activeBuffs[index];
        activeBuffs.RemoveAt(index);

        RemoveBuffEffect(buff);

        if (index < buffEffects.Count && buffEffects[index] != null)
        {
            Destroy(buffEffects[index]);
            buffEffects.RemoveAt(index);
        }

        OnBuffRemoved?.Invoke(buff);
        OnBuffExpired?.Invoke(buff);

        // 通知角色状态管理器
        if (stateManager != null)
        {
            stateManager.RemoveBuff(buff);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[BuffController] 移除BUFF: {buff.buffName}");
        }
    }

    // ========== BUFF查询接口 ==========

    /// <summary>
    /// 检查是否有指定类型的BUFF
    /// </summary>
    public bool HasBuff(BuffType buffType)
    {
        return FindBuff(buffType) != null;
    }

    /// <summary>
    /// 查找指定类型的BUFF
    /// </summary>
    public Buff FindBuff(BuffType buffType)
    {
        return activeBuffs.Find(b => b.type == buffType);
    }

    /// <summary>
    /// 获取指定类型的所有BUFF
    /// </summary>
    public List<Buff> GetBuffsByType(BuffType buffType)
    {
        return activeBuffs.FindAll(b => b.type == buffType);
    }

    /// <summary>
    /// 获取BUFF加成数值
    /// </summary>
    public float GetBuffedValue(BuffType buffType)
    {
        return buffValueCache.ContainsKey(buffType) ? buffValueCache[buffType] : 0f;
    }

    /// <summary>
    /// 获取BUFF乘法效果
    /// </summary>
    public float GetBuffMultiplier(BuffType buffType)
    {
        return buffMultiplierCache.ContainsKey(buffType) ? buffMultiplierCache[buffType] : 1f;
    }

    /// <summary>
    /// 获取BUFF剩余时间
    /// </summary>
    public float GetBuffRemainingTime(BuffType buffType)
    {
        var buff = FindBuff(buffType);
        return buff?.duration ?? 0f;
    }

    // ========== 向后兼容接口 ==========

    /// <summary>
    /// 检查是否有指定名称的BUFF（兼容版）
    /// </summary>
    public bool HasBuff(string buffName)
    {
        return activeBuffs.Find(b => b.buffName == buffName) != null;
    }

    /// <summary>
    /// 查找指定名称的BUFF（兼容版）
    /// </summary>
    public Buff FindBuff(string buffName)
    {
        return activeBuffs.Find(b => b.buffName == buffName);
    }

    /// <summary>
    /// 获取BUFF加成数值（兼容版）
    /// </summary>
    public float GetBuffedValue(string buffName)
    {
        return stringBuffValueCache.ContainsKey(buffName) ? stringBuffValueCache[buffName] : 0f;
    }

    // ========== BUFF效果处理 ==========

    bool ShouldStackBuff(Buff newBuff, Buff existingBuff)
    {
        return newBuff.stackable && existingBuff.stackable;
    }

    void StackBuff(Buff newBuff, Buff existingBuff)
    {
        existingBuff.value += newBuff.value;
        existingBuff.duration = Mathf.Max(existingBuff.duration, newBuff.duration);

        if (showDebugInfo)
        {
            Debug.Log($"[BuffController] 叠加BUFF: {newBuff.buffName}, 新数值: {existingBuff.value}");
        }
    }

    void ReplaceBuff(Buff newBuff, Buff existingBuff)
    {
        if (newBuff.value > existingBuff.value || newBuff.duration > existingBuff.duration)
        {
            existingBuff.value = newBuff.value;
            existingBuff.duration = newBuff.duration;

            if (showDebugInfo)
            {
                Debug.Log($"[BuffController] 替换BUFF: {newBuff.buffName}, 新数值: {newBuff.value}");
            }
        }
    }

    void ApplyBuffEffect(Buff buff)
    {
        switch (buff.type)
        {
            case BuffType.Slow:
            case BuffType.MoveSpeed:
                ApplyMovementSpeedChange();
                break;

            case BuffType.Poison:
                if (buff.tickInterval <= 0)
                {
                    buff.tickInterval = 1f; // 设置默认跳动间隔
                }
                StartPoisonEffect(buff);
                break;
        }
    }

    void RemoveBuffEffect(Buff buff)
    {
        switch (buff.type)
        {
            case BuffType.Slow:
            case BuffType.MoveSpeed:
                ApplyMovementSpeedChange();
                break;
        }
    }

    void ApplyMovementSpeedChange()
    {
        if (movementController != null)
        {
            float speedMultiplier = GetBuffMultiplier(BuffType.MoveSpeed) * GetBuffMultiplier(BuffType.Slow);
            // 这里需要根据具体的移动控制器接口来调整
            // movementController.SetSpeedMultiplier(speedMultiplier);

            if (showDebugInfo)
            {
                Debug.Log($"[BuffController] 移动速度倍数: {speedMultiplier:F2}");
            }
        }
    }

    void StartPoisonEffect(Buff poisonBuff)
    {
        // 启动中毒效果协程
        StartCoroutine(PoisonEffectCoroutine(poisonBuff));
    }

    System.Collections.IEnumerator PoisonEffectCoroutine(Buff poisonBuff)
    {
        float tickInterval = poisonBuff.tickInterval;
        float elapsed = 0f;

        while (elapsed < poisonBuff.duration && activeBuffs.Contains(poisonBuff))
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;

            // 应用中毒伤害
            ApplyPoisonTick(poisonBuff);
        }
    }

    void CreateBuffEffect(Buff buff)
    {
        if (!enableBuffVisualEffects || buffEffectPrefab == null) return;

        var effect = Instantiate(buffEffectPrefab, buffEffectParent);
        effect.transform.localPosition = Vector3.zero;

        var renderer = effect.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color buffColor = GetBuffColor(buff.type);
            renderer.material.color = buffColor;
        }

        buffEffects.Add(effect);
    }

    Color GetBuffColor(BuffType buffType)
    {
        return buffType switch
        {
            BuffType.Attack => Color.red,
            BuffType.AttackSpeed => Color.yellow,
            BuffType.SpellPower => Color.blue,
            BuffType.Slow => Color.cyan,
            BuffType.MoveSpeed => Color.green,
            BuffType.Vulnerable => Color.magenta,
            BuffType.Poison => Color.green,
            BuffType.Defense => Color.gray,
            BuffType.Health => new Color(1f, 0.5f, 0.5f),
            BuffType.Mana => new Color(0.5f, 0.5f, 1f),
            _ => Color.white
        };
    }

    // ========== 便捷接口 ==========

    /// <summary>
    /// 添加攻击力BUFF
    /// </summary>
    public void AddAttackBuff(float value, float duration)
    {
        AddBuff(Buff.Create(BuffType.Attack, value, duration, true));
    }

    /// <summary>
    /// 添加攻击速度BUFF
    /// </summary>
    public void AddAttackSpeedBuff(float value, float duration)
    {
        AddBuff(Buff.Create(BuffType.AttackSpeed, value, duration, true));
    }

    /// <summary>
    /// 添加法术强度BUFF
    /// </summary>
    public void AddSpellPowerBuff(float value, float duration)
    {
        AddBuff(Buff.Create(BuffType.SpellPower, value, duration, true));
    }

    /// <summary>
    /// 添加减速DEBUFF
    /// </summary>
    public void AddSlowDebuff(float percentage, float duration)
    {
        AddBuff(Buff.Create(BuffType.Slow, percentage, duration, false));
    }

    /// <summary>
    /// 添加中毒DEBUFF
    /// </summary>
    public void AddPoisonDebuff(float damagePerSecond, float duration)
    {
        var poisonBuff = Buff.Create(BuffType.Poison, damagePerSecond, duration, false);
        poisonBuff.tickInterval = 1f; // 每秒跳动一次
        AddBuff(poisonBuff);
    }

    /// <summary>
    /// 添加易伤DEBUFF
    /// </summary>
    public void AddVulnerableDebuff(float percentage, float duration)
    {
        AddBuff(Buff.Create(BuffType.Vulnerable, percentage, duration, false));
    }

    /// <summary>
    /// 添加防御力BUFF
    /// </summary>
    public void AddDefenseBuff(float value, float duration)
    {
        AddBuff(Buff.Create(BuffType.Defense, value, duration, true));
    }

    /// <summary>
    /// 添加移动速度BUFF
    /// </summary>
    public void AddMoveSpeedBuff(float percentage, float duration)
    {
        AddBuff(Buff.Create(BuffType.MoveSpeed, percentage, duration, true));
    }

    /// <summary>
    /// 添加生命值BUFF
    /// </summary>
    public void AddHealthBuff(float value, float duration)
    {
        AddBuff(Buff.Create(BuffType.Health, value, duration, true));
    }

    /// <summary>
    /// 添加法力值BUFF
    /// </summary>
    public void AddManaBuff(float value, float duration)
    {
        AddBuff(Buff.Create(BuffType.Mana, value, duration, true));
    }

    // ========== 调试功能 ==========

    [ContextMenu("添加测试攻击BUFF")]
    void DebugAddAttackBuff()
    {
        AddAttackBuff(10f, 30f);
    }

    [ContextMenu("添加测试减速DEBUFF")]
    void DebugAddSlowDebuff()
    {
        AddSlowDebuff(50f, 10f);
    }

    [ContextMenu("添加测试中毒DEBUFF")]
    void DebugAddPoisonDebuff()
    {
        AddPoisonDebuff(5f, 15f);
    }

    [ContextMenu("清除所有BUFF")]
    void DebugClearAllBuffs()
    {
        ClearAllBuffs();
    }

    [ContextMenu("打印BUFF状态")]
    void DebugPrintBuffStatus()
    {
        Debug.Log("=== BUFF状态报告 ===");
        Debug.Log($"活跃BUFF数量: {activeBuffs.Count}/{maxBuffCount}");

        foreach (var buff in activeBuffs)
        {
            Debug.Log($"- {buff.buffName} ({buff.type}): 数值={buff.value}, 剩余={buff.duration:F1}秒");
        }

        Debug.Log($"攻击力加成: +{GetBuffedValue(BuffType.Attack)}");
        Debug.Log($"移动速度倍数: x{GetBuffMultiplier(BuffType.MoveSpeed):F2}");
    }

    void OnGUI()
    {
        if (!showBuffUI || !showDebugInfo) return;

        GUILayout.BeginArea(new Rect(Screen.width - 320, Screen.height - 220, 310, 210));
        GUILayout.BeginVertical("box");

        GUILayout.Label("BUFF状态", GUI.skin.label);
        GUILayout.Space(5);

        GUILayout.Label($"活跃BUFF数量: {activeBuffs.Count}/{maxBuffCount}");
        GUILayout.Space(3);

        if (activeBuffs.Count > 0)
        {
            foreach (var buff in activeBuffs)
            {
                Color originalColor = GUI.color;
                GUI.color = GetBuffColor(buff.type);

                string buffInfo = $"{buff.buffName}: {buff.value} ({buff.duration:F1}s)";
                GUILayout.Label(buffInfo);

                GUI.color = originalColor;
            }
        }
        else
        {
            GUILayout.Label("无活跃BUFF");
        }

        GUILayout.Space(5);

        // 显示BUFF汇总
        GUILayout.Label("BUFF加成:");
        GUILayout.Label($"攻击力: +{GetBuffedValue(BuffType.Attack)}");
        GUILayout.Label($"攻击速度: +{GetBuffedValue(BuffType.AttackSpeed)}");
        GUILayout.Label($"法术强度: +{GetBuffedValue(BuffType.SpellPower)}");
        GUILayout.Label($"移动速度: x{GetBuffMultiplier(BuffType.MoveSpeed):F2}");
        GUILayout.Label($"防御力: +{GetBuffedValue(BuffType.Defense)}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        // 显示BUFF状态指示器
        if (activeBuffs.Count > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.3f + (activeBuffs.Count * 0.1f));

            // 为每个BUFF类型显示不同颜色的小球
            for (int i = 0; i < activeBuffs.Count && i < 10; i++)
            {
                Gizmos.color = GetBuffColor(activeBuffs[i].type);
                Vector3 pos = transform.position + Vector3.up * (2.5f + i * 0.2f);
                Gizmos.DrawSphere(pos, 0.1f);
            }
        }
    }

    /// <summary>
    /// 获取详细的BUFF状态信息
    /// </summary>
    public string GetDetailedBuffStatus()
    {
        if (activeBuffs.Count == 0)
        {
            return "当前无活跃BUFF";
        }

        string status = $"活跃BUFF ({activeBuffs.Count}/{maxBuffCount}):\n";

        foreach (var buff in activeBuffs)
        {
            string effect = buff.isPositive ? "+" : "-";
            status += $"• {buff.buffName}: {effect}{buff.value} (剩余 {buff.duration:F1}秒)\n";
        }

        status += "\n总体效果:\n";
        status += $"攻击力: +{GetBuffedValue(BuffType.Attack):F0}\n";
        status += $"攻击速度: +{GetBuffedValue(BuffType.AttackSpeed):F0}\n";
        status += $"法术强度: +{GetBuffedValue(BuffType.SpellPower):F0}\n";
        status += $"防御力: +{GetBuffedValue(BuffType.Defense):F0}\n";
        status += $"移动速度: x{GetBuffMultiplier(BuffType.MoveSpeed):F2}\n";
        status += $"减速效果: x{GetBuffMultiplier(BuffType.Slow):F2}\n";

        return status;
    }

    /// <summary>
    /// 获取简化的BUFF状态信息
    /// </summary>
    public string GetSimpleBuffStatus()
    {
        if (activeBuffs.Count == 0)
        {
            return "无BUFF";
        }

        var positiveBuffs = activeBuffs.FindAll(b => b.isPositive);
        var negativeBuffs = activeBuffs.FindAll(b => !b.isPositive);

        return $"BUFF: +{positiveBuffs.Count} -{negativeBuffs.Count}";
    }

    /// <summary>
    /// 强制刷新BUFF缓存
    /// </summary>
    public void ForceRefreshCache()
    {
        cacheNeedsUpdate = true;
        UpdateBuffCache();
    }

    /// <summary>
    /// 暂停所有BUFF倒计时
    /// </summary>
    public void PauseAllBuffs()
    {
        // 可以添加暂停标志来控制Update中的倒计时
        enabled = false;
    }

    /// <summary>
    /// 恢复所有BUFF倒计时
    /// </summary>
    public void ResumeAllBuffs()
    {
        enabled = true;
    }

    /// <summary>
    /// 延长指定类型BUFF的持续时间
    /// </summary>
    public bool ExtendBuffDuration(BuffType buffType, float additionalTime)
    {
        var buff = FindBuff(buffType);
        if (buff != null)
        {
            buff.duration += additionalTime;
            if (showDebugInfo)
            {
                Debug.Log($"[BuffController] 延长BUFF持续时间: {buff.buffName} +{additionalTime}秒");
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 修改指定类型BUFF的数值
    /// </summary>
    public bool ModifyBuffValue(BuffType buffType, float newValue)
    {
        var buff = FindBuff(buffType);
        if (buff != null)
        {
            float oldValue = buff.value;
            buff.value = newValue;
            cacheNeedsUpdate = true;

            if (showDebugInfo)
            {
                Debug.Log($"[BuffController] 修改BUFF数值: {buff.buffName} {oldValue} -> {newValue}");
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取所有正面BUFF
    /// </summary>
    public List<Buff> GetPositiveBuffs()
    {
        return activeBuffs.FindAll(b => b.isPositive);
    }

    /// <summary>
    /// 获取所有负面BUFF
    /// </summary>
    public List<Buff> GetNegativeBuffs()
    {
        return activeBuffs.FindAll(b => !b.isPositive);
    }

    /// <summary>
    /// 清除所有负面BUFF
    /// </summary>
    public int ClearNegativeBuffs()
    {
        var negativeBuffs = GetNegativeBuffs();
        int count = 0;

        foreach (var buff in negativeBuffs)
        {
            if (RemoveBuff(buff))
            {
                count++;
            }
        }

        if (showDebugInfo && count > 0)
        {
            Debug.Log($"[BuffController] 清除了 {count} 个负面BUFF");
        }

        return count;
    }

    /// <summary>
    /// 清除所有正面BUFF
    /// </summary>
    public int ClearPositiveBuffs()
    {
        var positiveBuffs = GetPositiveBuffs();
        int count = 0;

        foreach (var buff in positiveBuffs)
        {
            if (RemoveBuff(buff))
            {
                count++;
            }
        }

        if (showDebugInfo && count > 0)
        {
            Debug.Log($"[BuffController] 清除了 {count} 个正面BUFF");
        }

        return count;
    }

    /// <summary>
    /// 检查是否免疫指定类型的BUFF
    /// </summary>
    public bool IsImmuneToBuffType(BuffType buffType)
    {
        // 可以扩展实现免疫系统
        // 例如：某些角色可能免疫中毒、减速等
        return false;
    }

    void OnDestroy()
    {
        // 清理所有协程和效果
        StopAllCoroutines();
        ClearAllBuffs();

        // 清理事件引用
        OnBuffAdded = null;
        OnBuffRemoved = null;
        OnBuffExpired = null;
        OnBuffCacheUpdated = null;
    }

    void OnDisable()
    {
        // 停止所有BUFF效果，但保持BUFF列表
        StopAllCoroutines();
    }

    void OnEnable()
    {
        // 重新应用所有BUFF效果
        if (Application.isPlaying)
        {
            foreach (var buff in activeBuffs)
            {
                ApplyBuffEffect(buff);
            }
        }
    }
}