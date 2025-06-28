using UnityEngine;

public class Weapon : MonoBehaviour
{
    public string weaponName; // 武器名称

    [Header("Base Stats")]
    [Tooltip("武器基础伤害（可选，若不使用新系统可用此值）")]
    public int damage; // 基础伤害值
    [Tooltip("武器攻击范围")]
    public float range; // 武器攻击范围

    [Header("Bonus Stats")]
    [Tooltip("武器增加的攻击力")]
    public int attackBonus;  // 武器增加的攻击力
    [Tooltip("武器增加的攻击速度")]
    public float speedBonus; // 武器增加的攻击速度
    [Tooltip("武器增加的法术强度")]
    public int spellBonus;   // 武器增加的法术强度

    [Header("Attribute Multipliers")]
    [Tooltip("力量转换倍率（决定最终攻击力）")]
    public float attackMultiplier = 1.0f;
    [Tooltip("敏捷转换倍率（决定最终攻击速度）")]
    public float speedMultiplier = 1.0f;
    [Tooltip("智力转换倍率（决定最终法术强度）")]
    public float spellMultiplier = 1.0f;

    [Header("Hitbox Settings")]
    [Tooltip("自定义 Hitbox 预制体，如果配置了，则使用自定义攻击效果")]
    public GameObject hitboxPrefab;
    [Tooltip("Hitbox 持续时间")]
    public float hitboxDuration = 0.2f;
    [Tooltip("Hitbox 偏移量")]
    public Vector3 hitboxOffset = new Vector3(0, 0.5f, 1f);
    [Tooltip("Hitbox 攻击范围")]
    public float hitboxRange = 3f;
    [Tooltip("Hitbox 攻击伤害")]
    public int hitboxDamage = 10;

    // 判断是否配置了自定义 Hitbox
    public bool HasCustomHitbox()
    {
        return hitboxPrefab != null;
    }

    // 可选：扩展 PerformAttack 方法，让武器自行决定攻击逻辑
}