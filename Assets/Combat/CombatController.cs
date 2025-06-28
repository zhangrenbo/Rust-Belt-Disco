using UnityEngine;

/// <summary>
/// 角色战斗控制器 - 处理攻击、技能、武器等功能，与BuffController完全集成
/// </summary>
public class CombatController : MonoBehaviour
{
    [Header("=== 角色类型 ===")]
    public CharacterType characterType = CharacterType.Player;
    public bool enablePlayerInput = true; // 是否启用玩家输入控制

    [Header("=== 攻击设置 ===")]
    public Weapon currentWeapon;
    public GameObject defaultHitboxPrefab;
    public float defaultHitboxRange = 3f;
    public int defaultHitboxDamage = 10;
    public float defaultHitboxDuration = 0.2f;
    public Vector3 defaultHitboxOffset = new Vector3(0, 0.5f, 1f);

    [Header("=== 技能槽 (1-4) ===")]
    public Skill[] skillSlots = new Skill[4];

    [Header("=== 战斗设置 ===")]
    public float attackCooldown = 0.5f;
    public bool autoEnterCombatOnAttack = true;

    [Header("=== 调试设置 ===")]
    public bool showDebugInfo = false;

    // 组件引用
    private StateController stateController;
    private AttributeController attributeController;
    private BuffController buffController;
    private Animator animator;

    // 战斗状态
    private float lastAttackTime = 0f;
    private bool isAttacking = false;

    // 事件
    public System.Action OnAttackPerformed;
    public System.Action<int> OnSkillCast; // 技能施放
    public System.Action<Weapon> OnWeaponChanged;
    public System.Action OnCombatStart;
    public System.Action OnCombatEnd;

    // 属性访问器
    public bool CanAttack => Time.time - lastAttackTime >= attackCooldown &&
                            stateController != null &&
                            stateController.CanAttack();

    public bool IsAttacking => isAttacking;
    public Weapon CurrentWeapon => currentWeapon;

    // 计算后的战斗属性 - 包含BUFF等加成
    public int AttackPower
    {
        get
        {
            int baseAttack = attributeController?.Strength ?? 10;
            float buffAttack = buffController?.GetBuffedValue(BuffType.Attack) ?? 0f;
            int weaponAttack = currentWeapon?.attackBonus ?? 0;
            return baseAttack + Mathf.RoundToInt(buffAttack) + weaponAttack;
        }
    }

    public float AttackSpeed
    {
        get
        {
            float baseSpeed = attributeController?.Agility ?? 10;
            float buffSpeed = buffController?.GetBuffedValue(BuffType.AttackSpeed) ?? 0f;
            float weaponSpeed = currentWeapon?.speedBonus ?? 0;
            return baseSpeed + buffSpeed + weaponSpeed;
        }
    }

    public int SpellPower
    {
        get
        {
            int baseSpell = attributeController?.Intelligence ?? 10;
            float buffSpell = buffController?.GetBuffedValue(BuffType.SpellPower) ?? 0f;
            int weaponSpell = currentWeapon?.spellBonus ?? 0;
            return baseSpell + Mathf.RoundToInt(buffSpell) + weaponSpell;
        }
    }

    void Awake()
    {
        stateController = GetComponent<StateController>();
        attributeController = GetComponent<AttributeController>();
        buffController = GetComponent<BuffController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (enablePlayerInput && characterType == CharacterType.Player &&
            stateController != null && !stateController.IsInDialogue)
        {
            HandleCombatInput();
        }


        UpdateAttackState();
    }

    void HandleCombatInput()
    {
        // 处理攻击输入
        if (Input.GetMouseButtonDown(0) && CanAttack)
        {
            PerformAttack();
        }

        // 处理技能输入 (1-4键)
        if (Input.GetKeyDown(KeyCode.Alpha1)) CastSkill(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) CastSkill(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) CastSkill(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) CastSkill(3);
    }

    void UpdateAttackState()
    {
        // 检查攻击动画是否结束
        if (isAttacking && animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName("Attack") && stateInfo.normalizedTime > 0.9f)
            {
                isAttacking = false;
            }
        }
    }

    // ========== 攻击功能 ==========

    /// <summary>
    /// 执行攻击
    /// </summary>
    public void PerformAttack()
    {
        if (!CanAttack) return;

        lastAttackTime = Time.time;
        isAttacking = true;

        // 自动进入战斗状态
        if (autoEnterCombatOnAttack && stateController != null)
        {
            stateController.EnterCombatState();
        }

        // 播放攻击动画
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // 生成攻击碰撞盒
        SpawnAttackHitbox();

        OnAttackPerformed?.Invoke();

        if (showDebugInfo)
        {
            Debug.Log($"[CombatController] 执行攻击 - 攻击力: {AttackPower}");
        }
    }

    /// <summary>
    /// 生成攻击碰撞盒
    /// </summary>
    void SpawnAttackHitbox()
    {
        GameObject prefab = defaultHitboxPrefab;
        float range = defaultHitboxRange;
        int damage = defaultHitboxDamage + AttackPower;
        float duration = defaultHitboxDuration;
        Vector3 offset = defaultHitboxOffset;

        // 如果有武器，使用武器的碰撞盒设置
        if (currentWeapon != null && currentWeapon.HasCustomHitbox())
        {
            prefab = currentWeapon.hitboxPrefab;
            range = currentWeapon.hitboxRange;
            damage = currentWeapon.hitboxDamage + AttackPower;
            duration = currentWeapon.hitboxDuration;
            offset = currentWeapon.hitboxOffset;
        }

        if (prefab != null)
        {
            Vector3 spawnPos = transform.position + transform.forward * offset.z + Vector3.up * offset.y;
            var hitboxObj = Instantiate(prefab, spawnPos, Quaternion.LookRotation(transform.forward));

            var hitboxController = hitboxObj.GetComponent<HitboxController>();
            if (hitboxController != null)
            {
                hitboxController.Setup(range, damage, duration, this.gameObject);
            }

            if (showDebugInfo)
            {
                Debug.Log($"[CombatController] 生成碰撞盒 - 伤害: {damage}, 范围: {range}, 持续: {duration}");
            }
        }
    }

    // ========== 技能功能 ==========

    /// <summary>
    /// 施放技能
    /// </summary>
    public void CastSkill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skillSlots.Length)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[CombatController] 技能施放超出范围: {skillIndex}");
            }
            return;
        }

        var skill = skillSlots[skillIndex];
        if (skill == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[CombatController] 技能槽 {skillIndex + 1} 为空");
            }
            return;
        }

        if (!skill.CanUse())
        {
            if (showDebugInfo)
            {
                float remainingCooldown = skill.GetRemainingCooldown();
                Debug.Log($"[CombatController] 技能 {skill.skillName} 冷却中，剩余: {remainingCooldown:F1}秒");
            }
            return;
        }

        // 播放技能动画
        if (animator != null)
        {
            animator.SetTrigger($"Skill{skillIndex + 1}");
        }

        // 使用技能
        skill.Use();

        OnSkillCast?.Invoke(skillIndex);

        if (showDebugInfo)
        {
            Debug.Log($"[CombatController] 施放技能: {skill.skillName} (槽位 {skillIndex + 1})");
        }
    }

    /// <summary>
    /// 设置技能到指定槽位
    /// </summary>
    public void SetSkill(int slotIndex, Skill skill)
    {
        if (slotIndex >= 0 && slotIndex < skillSlots.Length)
        {
            skillSlots[slotIndex] = skill;

            if (showDebugInfo)
            {
                string skillName = skill?.skillName ?? "空";
                Debug.Log($"[CombatController] 设置技能槽 {slotIndex + 1}: {skillName}");
            }
        }
    }

    /// <summary>
    /// 获取技能信息
    /// </summary>
    public Skill GetSkill(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < skillSlots.Length)
        {
            return skillSlots[slotIndex];
        }
        return null;
    }

    /// <summary>
    /// 检查技能是否可用
    /// </summary>
    public bool IsSkillAvailable(int slotIndex)
    {
        var skill = GetSkill(slotIndex);
        return skill != null && skill.CanUse();
    }

    /// <summary>
    /// 获取技能剩余冷却时间
    /// </summary>
    public float GetSkillCooldown(int slotIndex)
    {
        var skill = GetSkill(slotIndex);
        return skill?.GetRemainingCooldown() ?? 0f;
    }

    // ========== 武器功能 ==========

    /// <summary>
    /// 装备武器
    /// </summary>
    public void EquipWeapon(Weapon weapon)
    {
        Weapon oldWeapon = currentWeapon;
        currentWeapon = weapon;

        OnWeaponChanged?.Invoke(currentWeapon);

        if (showDebugInfo)
        {
            string oldName = oldWeapon?.weaponName ?? "无";
            string newName = currentWeapon?.weaponName ?? "无";
            Debug.Log($"[CombatController] 武器更换: {oldName} -> {newName}");
        }
    }

    /// <summary>
    /// 卸下武器
    /// </summary>
    public void UnequipWeapon()
    {
        EquipWeapon(null);
    }

    // ========== 战斗状态管理 ==========

    /// <summary>
    /// 进入战斗状态
    /// </summary>
    public void EnterCombat()
    {
        if (stateController != null)
        {
            stateController.EnterCombatState();
        }

        OnCombatStart?.Invoke();

        if (showDebugInfo)
        {
            Debug.Log("[CombatController] 进入战斗状态");
        }
    }

    /// <summary>
    /// 退出战斗状态
    /// </summary>
    public void ExitCombat()
    {
        if (stateController != null && stateController.IsInDialogue) return;

        if (stateController != null)
        {
            stateController.ExitCombatState();
        }
        OnCombatEnd?.Invoke();

        if (showDebugInfo)
        {
            Debug.Log("[CombatController] 退出战斗状态");
        }
    }

    // ========== 受伤处理 ==========

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (stateController != null && stateController.IsInDialogue) return;

        // 通过状态管理器处理伤害
        var stateManager = GetComponent<CharacterStateManager>();
        if (stateManager != null)
        {
            // 应用易伤BUFF的伤害修正
            if (buffController != null)
            {
                float vulnerableMultiplier = buffController.GetBuffMultiplier(BuffType.Vulnerable);
                damage = Mathf.RoundToInt(damage * vulnerableMultiplier);
            }

            stateManager.TakeDamage(damage);
        }

        // 播放受伤动画
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }

        // 自动进入战斗状态
        if (autoEnterCombatOnAttack && stateController != null)
        {
            stateController.EnterCombatState();
        }

        // 检查是否死亡
        if (attributeController != null && attributeController.Health <= 0)
        {
            HandleDeath();
        }

        if (showDebugInfo)
        {
            int currentHealth = attributeController?.Health ?? 0;
            int maxHealth = attributeController?.MaxHealth ?? 0;
            Debug.Log($"[CombatController] 受到伤害: {damage}, 剩余生命值: {currentHealth}/{maxHealth}");
        }
    }

    /// <summary>
    /// 处理死亡
    /// </summary>
    void HandleDeath()
    {
        if (stateController != null)
        {
            stateController.EnterDeadState();
        }

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        if (showDebugInfo)
        {
            Debug.Log("[CombatController] 玩家死亡");
        }
    }

    // ========== 公共接口 ==========

    /// <summary>
    /// 设置攻击冷却时间
    /// </summary>
    public void SetAttackCooldown(float cooldown)
    {
        attackCooldown = Mathf.Max(0f, cooldown);
    }

    /// <summary>
    /// 强制重置攻击冷却
    /// </summary>
    public void ResetAttackCooldown()
    {
        lastAttackTime = 0f;
    }

    /// <summary>
    /// 获取攻击冷却剩余时间
    /// </summary>
    public float GetAttackCooldownRemaining()
    {
        return Mathf.Max(0f, attackCooldown - (Time.time - lastAttackTime));
    }

    /// <summary>
    /// 清空所有技能槽
    /// </summary>
    public void ClearAllSkills()
    {
        for (int i = 0; i < skillSlots.Length; i++)
        {
            skillSlots[i] = null;
        }

        if (showDebugInfo)
        {
            Debug.Log("[CombatController] 清空所有技能槽");
        }
    }

    /// <summary>
    /// 应用BUFF效果到攻击
    /// </summary>
    public void ApplyBuffsToAttack(ref int damage, ref float speed)
    {
        if (buffController != null)
        {
            // 应用攻击力BUFF
            damage += Mathf.RoundToInt(buffController.GetBuffedValue(BuffType.Attack));

            // 应用攻击速度BUFF
            speed += buffController.GetBuffedValue(BuffType.AttackSpeed);

            // 应用速度倍率
            speed *= buffController.GetBuffMultiplier(BuffType.AttackSpeed);
        }
    }

    /// <summary>
    /// 治疗
    /// </summary>
    public void Heal(int amount)
    {
        var stateManager = GetComponent<CharacterStateManager>();
        if (stateManager != null)
        {
            stateManager.Heal(amount);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[CombatController] 治疗: {amount}点生命值");
        }
    }

    // ========== 调试功能 ==========

    [ContextMenu("测试攻击")]
    void DebugAttack()
    {
        PerformAttack();
    }

    [ContextMenu("测试技能1")]
    void DebugSkill1()
    {
        CastSkill(0);
    }

    [ContextMenu("测试受伤")]
    void DebugTakeDamage()
    {
        TakeDamage(20);
    }

    [ContextMenu("测试治疗")]
    void DebugHeal()
    {
        Heal(50);
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        // 显示攻击范围
        float range = currentWeapon?.hitboxRange ?? defaultHitboxRange;
        Vector3 offset = currentWeapon?.hitboxOffset ?? defaultHitboxOffset;
        Vector3 attackPos = transform.position + transform.forward * offset.z + Vector3.up * offset.y;

        Gizmos.color = isAttacking ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(attackPos, range);

        // 显示攻击方向
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        // 显示战斗状态
        if (stateController != null && stateController.IsInCombat)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.5f);
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 350));
        GUILayout.BeginVertical("box");

        GUILayout.Label("战斗功能调试", GUI.skin.label);
        GUILayout.Space(5);

        GUILayout.Label($"攻击力: {AttackPower}");
        GUILayout.Label($"攻击速度: {AttackSpeed:F1}");
        GUILayout.Label($"法术强度: {SpellPower}");
        GUILayout.Space(5);

        GUILayout.Label($"攻击冷却: {GetAttackCooldownRemaining():F1}s");
        GUILayout.Label($"正在攻击: {isAttacking}");
        GUILayout.Space(5);

        string weaponName = currentWeapon?.weaponName ?? "无武器";
        GUILayout.Label($"当前武器: {weaponName}");
        GUILayout.Space(5);

        // 显示BUFF加成
        if (buffController != null)
        {
            GUILayout.Label("BUFF加成:");
            GUILayout.Label($"  攻击: +{buffController.GetBuffedValue(BuffType.Attack):F0}");
            GUILayout.Label($"  攻速: +{buffController.GetBuffedValue(BuffType.AttackSpeed):F0}");
            GUILayout.Label($"  法强: +{buffController.GetBuffedValue(BuffType.SpellPower):F0}");
            GUILayout.Label($"  易伤: x{buffController.GetBuffMultiplier(BuffType.Vulnerable):F2}");
            GUILayout.Space(5);
        }

        GUILayout.Label("技能状态:");
        for (int i = 0; i < skillSlots.Length; i++)
        {
            var skill = skillSlots[i];
            if (skill != null)
            {
                float cooldown = skill.GetRemainingCooldown();
                string status = cooldown > 0 ? $"冷却 {cooldown:F1}s" : "就绪";
                GUILayout.Label($"技能{i + 1}: {skill.skillName} ({status})");
            }
            else
            {
                GUILayout.Label($"技能{i + 1}: 空");
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}

// ========== 技能系统相关类定义 ==========

/// <summary>
/// 技能基类
/// </summary>
[System.Serializable]
public class Skill
{
    [Header("=== 技能基本信息 ===")]
    public string skillName = "默认技能";
    public string description = "技能描述";
    public Sprite skillIcon;

    [Header("=== 技能参数 ===")]
    public float cooldown = 5f;
    public int manaCost = 10;
    public float castTime = 0f;
    public float range = 5f;

    [Header("=== 技能效果 ===")]
    public int damage = 20;
    public float duration = 0f;
    public bool isAreaOfEffect = false;
    public float aoeRadius = 3f;

    [Header("=== BUFF效果 ===")]
    public bool applyBuffToSelf = false;
    public bool applyBuffToTarget = false;
    public Buff[] buffsToApply;

    // 私有变量
    private float lastUseTime = -999f;
    private bool isOnCooldown = false;

    // 属性访问器
    public bool CanUse()
    {
        return !isOnCooldown && Time.time - lastUseTime >= cooldown;
    }

    public float GetRemainingCooldown()
    {
        if (!isOnCooldown) return 0f;
        return Mathf.Max(0f, cooldown - (Time.time - lastUseTime));
    }

    /// <summary>
    /// 使用技能
    /// </summary>
    public virtual void Use()
    {
        if (!CanUse()) return;

        lastUseTime = Time.time;
        isOnCooldown = true;

        // 执行技能逻辑
        ExecuteSkill();

        // 开始冷却
        StartCooldown();
    }

    /// <summary>
    /// 执行技能的具体逻辑 - 子类可重写
    /// </summary>
    protected virtual void ExecuteSkill()
    {
        Debug.Log($"[Skill] 使用技能: {skillName}");
    }

    /// <summary>
    /// 开始冷却计时
    /// </summary>
    protected virtual void StartCooldown()
    {
        // 可以在这里添加冷却相关的逻辑
    }

    /// <summary>
    /// 重置冷却
    /// </summary>
    public void ResetCooldown()
    {
        isOnCooldown = false;
        lastUseTime = -999f;
    }

    /// <summary>
    /// 设置冷却时间
    /// </summary>
    public void SetCooldown(float newCooldown)
    {
        cooldown = Mathf.Max(0f, newCooldown);
    }
}

/// <summary>
/// 攻击技能 - 造成伤害的技能
/// </summary>
[System.Serializable]
public class AttackSkill : Skill
{
    [Header("=== 攻击技能设置 ===")]
    public float damageMultiplier = 1.5f;
    public bool usesWeaponDamage = true;
    public GameObject skillEffect;
    public AudioClip skillSound;

    protected override void ExecuteSkill()
    {
        base.ExecuteSkill();

        // 这里可以添加具体的攻击技能逻辑
        // 比如生成伤害区域、播放特效等

        Debug.Log($"[AttackSkill] 攻击技能 {skillName} 造成 {damage} 点伤害");
    }
}

/// <summary>
/// 增益技能 - 提供BUFF效果的技能
/// </summary>
[System.Serializable]
public class BuffSkill : Skill
{
    [Header("=== 增益技能设置 ===")]
    public BuffType buffType = BuffType.Attack;
    public float buffValue = 10f;
    public float buffDuration = 30f;
    public bool isPositiveBuff = true;

    protected override void ExecuteSkill()
    {
        base.ExecuteSkill();

        // 这里可以添加具体的增益技能逻辑
        Debug.Log($"[BuffSkill] 增益技能 {skillName} 提供 {buffType} 效果");
    }
}

/// <summary>
/// 治疗技能 - 恢复生命值的技能
/// </summary>
[System.Serializable]
public class HealSkill : Skill
{
    [Header("=== 治疗技能设置 ===")]
    public int healAmount = 50;
    public bool isPercentageHeal = false;
    public float healPercentage = 0.3f;
    public GameObject healEffect;

    protected override void ExecuteSkill()
    {
        base.ExecuteSkill();

        // 这里可以添加具体的治疗技能逻辑
        int finalHealAmount = isPercentageHeal ?
            Mathf.RoundToInt(healPercentage * 100) : healAmount;

        Debug.Log($"[HealSkill] 治疗技能 {skillName} 恢复 {finalHealAmount} 点生命值");
    }
}