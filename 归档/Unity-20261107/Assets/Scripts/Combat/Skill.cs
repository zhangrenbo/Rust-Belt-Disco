using UnityEngine;

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

    [Header("=== 效果设置 ===")]
    public int damage = 20;
    public float duration = 0f;
    public bool isAreaOfEffect = false;
    public float aoeRadius = 3f;

    [Header("=== BUFF效果 ===")]
    public bool applyBuffToSelf = false;
    public bool applyBuffToTarget = false;
    public Buff[] buffsToApply;

    private float lastUseTime = -999f;
    private bool isOnCooldown = false;

    public virtual bool CanUse()
    {
        return !isOnCooldown && Time.time - lastUseTime >= cooldown;
    }

    public float GetRemainingCooldown()
    {
        if (!isOnCooldown) return 0f;
        return Mathf.Max(0f, cooldown - (Time.time - lastUseTime));
    }

    public virtual void Use()
    {
        if (!CanUse()) return;

        lastUseTime = Time.time;
        isOnCooldown = true;
        ExecuteSkill();
        StartCooldown();
    }

    protected virtual void ExecuteSkill()
    {
        Debug.Log($"[Skill] 使用技能: {skillName}");
    }

    protected virtual void StartCooldown()
    {
        // 可扩展冷却回调
    }

    public void ResetCooldown()
    {
        isOnCooldown = false;
        lastUseTime = -999f;
    }

    public void SetCooldown(float newCooldown)
    {
        cooldown = Mathf.Max(0f, newCooldown);
    }
}

/// <summary>
/// 攻击技能
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
        Debug.Log($"[AttackSkill] 释放 {skillName}");
    }
}

/// <summary>
/// 增益技能
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
        Debug.Log($"[BuffSkill] 释放 {skillName} 提供 {buffType} 效果");
    }
}

/// <summary>
/// 治疗技能
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
        int finalHealAmount = isPercentageHeal ?
            Mathf.RoundToInt(healPercentage * 100) : healAmount;
        Debug.Log($"[HealSkill] 释放 {skillName} 回复 {finalHealAmount}");
    }
}
