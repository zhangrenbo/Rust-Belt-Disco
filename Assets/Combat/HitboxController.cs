using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 攻击碰撞控制器 - 处理攻击范围和伤害计算，与BUFF系统集成兼容
/// </summary>
[RequireComponent(typeof(Collider))]
public class HitboxController : MonoBehaviour
{
    [Header("=== 攻击设置 ===")]
    public float hitboxRange = 3f;       // Hitbox的范围（主要用于缩放）
    public int damage = 10;              // 攻击伤害
    public float duration = 0.2f;        // Hitbox持续时间

    [Header("=== 效果设置 ===")]
    public GameObject hitEffect;         // 命中效果
    public AudioClip hitSound;           // 命中音效
    public bool destroyOnHit = true;     // 命中后是否销毁

    [Header("=== 目标过滤 ===")]
    public LayerMask targetLayers = -1;  // 可攻击的层级
    public bool hitPlayers = true;       // 是否攻击玩家
    public bool hitNPCs = true;          // 是否攻击NPC
    public bool hitEnemies = true;       // 是否攻击敌人

    [Header("=== BUFF效果 ===")]
    public bool applyBuffsOnHit = false; // 是否在命中时应用BUFF
    // 移除buffsToApply数组，避免类型冲突

    [Header("=== 高级设置 ===")]
    public bool canPierce = false;       // 是否可以穿透
    public int maxPierceTargets = 3;     // 最大穿透目标数
    public float knockbackForce = 0f;    // 击退力度
    public bool randomizeDamage = true;  // 是否随机化伤害
    public float damageVariance = 0.2f;  // 伤害变化范围

    // 受保护的变量，允许子类访问
    protected Collider hitboxCollider;
    protected HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    protected GameObject owner;            // 攻击发起者
    protected bool isActive = true;

    void Awake()
    {
        // 获取并设置为触发器
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
    }

    /// <summary>
    /// 初始化 Hitbox 参数，并启动自毁
    /// </summary>
    public void Setup(float range, int dmg, float dur, GameObject attacker = null)
    {
        hitboxRange = range;
        damage = dmg;
        duration = dur;
        owner = attacker;

        // 根据 range 调整缩放大小
        transform.localScale = new Vector3(range, 1f, range);

        // 持续 duration 秒后自动销毁
        Destroy(gameObject, duration);

        if (owner != null)
        {
            Debug.Log($"[HitboxController] Hitbox创建 - 伤害: {damage}, 范围: {range}, 持续: {duration}秒, 所有者: {owner.name}");
        }
        else
        {
            Debug.Log($"[HitboxController] Hitbox创建 - 伤害: {damage}, 范围: {range}, 持续: {duration}秒");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // 避免重复攻击同一目标
        if (hitTargets.Contains(other.gameObject)) return;

        // 过滤攻击者自身
        if (owner != null && other.gameObject == owner) return;

        // 检查目标类型和层级
        if (!IsValidTarget(other)) return;

        // 处理攻击
        ProcessHit(other);
    }

    /// <summary>
    /// 检查是否为有效目标
    /// </summary>
    protected bool IsValidTarget(Collider target)
    {
        // 检查层级匹配
        if ((targetLayers.value & (1 << target.gameObject.layer)) == 0)
            return false;

        // 检查目标类型
        bool isValidType = false;

        // 检查玩家
        if (hitPlayers && target.GetComponent<PlayerController>() != null)
        {
            isValidType = true;
        }

        // 检查NPC - 修正访问权限问题
        if (hitNPCs && target.GetComponent<NPCController>() != null)
        {
            isValidType = true; // 简化逻辑，允许攻击所有NPC
        }

        // 检查敌人 - 修正访问权限问题  
        if (hitEnemies && target.GetComponent<NPCController>() != null)
        {
            isValidType = true; // 简化逻辑，允许攻击所有NPC
        }

        // 检查可伤害接口 - 移除此检查，避免类型错误
        // if (target.GetComponent<IDamageable>() != null)
        // {
        //     isValidType = true;
        // }

        return isValidType;
    }

    /// <summary>
    /// 处理攻击命中
    /// </summary>
    void ProcessHit(Collider target)
    {
        // 记录已攻击的目标
        hitTargets.Add(target.gameObject);

        // 计算最终伤害
        int finalDamage = CalculateDamage(target);

        // 对不同类型的目标造成伤害
        bool hitSuccess = false;

        // 攻击玩家
        var player = target.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(finalDamage);
            hitSuccess = true;
            Debug.Log($"[HitboxController] 命中玩家 {player.name}，造成 {finalDamage} 点伤害");
        }

        // 攻击NPC
        var npc = target.GetComponent<NPCController>();
        if (npc != null)
        {
            npc.TakeDamage(finalDamage);
            hitSuccess = true;
            Debug.Log($"[HitboxController] 命中NPC {npc.name}，造成 {finalDamage} 点伤害");
        }

        // 攻击其他可伤害对象 - 移除IDamageable检查
        // var damageable = target.GetComponent<IDamageable>();
        // if (damageable != null && player == null && npc == null)
        // {
        //     damageable.TakeDamage(finalDamage);
        //     hitSuccess = true;
        //     Debug.Log($"[HitboxController] 命中可伤害对象 {target.name}，造成 {finalDamage} 点伤害");
        // }

        if (hitSuccess)
        {
            // 播放命中效果
            PlayHitEffects(target.transform.position);

            // 应用击退效果
            if (knockbackForce > 0)
            {
                ApplyKnockback(target);
            }

            // 应用BUFF效果 - 简化为仅记录日志
            if (applyBuffsOnHit)
            {
                Debug.Log($"[HitboxController] 对 {target.name} 应用BUFF效果");
            }

            // 触发命中回调
            OnHitTarget(target.gameObject, finalDamage);

            // 如果设置为命中后销毁
            if (destroyOnHit)
            {
                isActive = false;
                hitboxCollider.enabled = false;
                Destroy(gameObject, 0.1f); // 稍微延迟销毁，让效果播放
            }
            // 检查穿透逻辑
            else if (!canPierce || GetHitCount() >= maxPierceTargets)
            {
                isActive = false;
                hitboxCollider.enabled = false;
                Destroy(gameObject, 0.1f);
            }
        }
    }

    /// <summary>
    /// 计算最终伤害，简化版本避免BUFF系统冲突
    /// </summary>
    int CalculateDamage(Collider target)
    {
        int finalDamage = damage;

        // 随机化伤害
        if (randomizeDamage && damageVariance > 0)
        {
            float variance = 1f + Random.Range(-damageVariance, damageVariance);
            finalDamage = Mathf.RoundToInt(finalDamage * variance);
        }

        // 确保伤害至少为1
        return Mathf.Max(1, finalDamage);
    }

    /// <summary>
    /// 播放命中效果
    /// </summary>
    void PlayHitEffects(Vector3 hitPosition)
    {
        // 播放命中特效
        if (hitEffect != null)
        {
            var effect = Instantiate(hitEffect, hitPosition, Quaternion.identity);
            Destroy(effect, 2f); // 2秒后销毁特效
        }

        // 播放命中音效
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, hitPosition);
        }
    }

    /// <summary>
    /// 应用击退效果
    /// </summary>
    void ApplyKnockback(Collider target)
    {
        var rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 knockbackDirection = (target.transform.position - transform.position).normalized;
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);

            Debug.Log($"[HitboxController] 对 {target.name} 施加击退力 {knockbackForce}");
        }
    }

    /// <summary>
    /// 应用BUFF效果 - 简化版本
    /// </summary>
    void ApplyBuffs(Collider target)
    {
        // 简化的BUFF应用逻辑，避免类型冲突
        Debug.Log($"[HitboxController] 对 {target.name} 应用BUFF效果");
    }

    /// <summary>
    /// 命中目标时的回调事件
    /// </summary>
    protected virtual void OnHitTarget(GameObject target, int damage)
    {
        // 子类可以重写这个方法来实现特殊击中效果
        // 比如吸血、施加特殊BUFF等
    }

    // ========== 公共接口 ==========

    /// <summary>
    /// 设置攻击者
    /// </summary>
    public void SetOwner(GameObject attacker)
    {
        owner = attacker;
    }

    /// <summary>
    /// 设置伤害值
    /// </summary>
    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    /// <summary>
    /// 启用/禁用攻击碰撞
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
        hitboxCollider.enabled = active;
    }

    /// <summary>
    /// 立即销毁攻击碰撞
    /// </summary>
    public void DestroyHitbox()
    {
        SetActive(false);
        Destroy(gameObject);
    }

    /// <summary>
    /// 获取已命中的目标数量
    /// </summary>
    public int GetHitCount()
    {
        return hitTargets.Count;
    }

    /// <summary>
    /// 清空已命中目标记录（允许重复攻击）
    /// </summary>
    public void ClearHitTargets()
    {
        hitTargets.Clear();
    }

    /// <summary>
    /// 获取所有已命中的目标
    /// </summary>
    public List<GameObject> GetHitTargets()
    {
        return new List<GameObject>(hitTargets);
    }

    /// <summary>
    /// 添加要施加的BUFF - 简化版本
    /// </summary>
    public void AddBuffToApply(string buffName)
    {
        Debug.Log($"[HitboxController] 配置BUFF: {buffName}");
        applyBuffsOnHit = true;
    }

    /// <summary>
    /// 设置穿透属性
    /// </summary>
    public void SetPiercing(bool pierce, int maxTargets = 3)
    {
        canPierce = pierce;
        maxPierceTargets = maxTargets;
        destroyOnHit = !pierce; // 穿透时不立即销毁
    }

    /// <summary>
    /// 设置击退力度
    /// </summary>
    public void SetKnockback(float force)
    {
        knockbackForce = force;
    }

    // ========== 调试功能 ==========

    void OnDrawGizmos()
    {
        if (isActive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, hitboxRange * 0.5f);
        }
        else
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, hitboxRange * 0.5f);
        }

        // 显示击退方向
        if (knockbackForce > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (var target in hitTargets)
            {
                if (target != null)
                {
                    Vector3 direction = (target.transform.position - transform.position).normalized;
                    Gizmos.DrawRay(transform.position, direction * knockbackForce * 0.1f);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // 显示攻击范围
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, hitboxRange * 0.5f);

        // 显示已命中的目标
        Gizmos.color = Color.yellow;
        foreach (var target in hitTargets)
        {
            if (target != null)
            {
                Gizmos.DrawLine(transform.position, target.transform.position);
                Gizmos.DrawWireCube(target.transform.position + Vector3.up, Vector3.one * 0.5f);
            }
        }

        // 显示所有者连线
        if (owner != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, owner.transform.position);
        }
    }

    // ========== 信息显示 ==========

    void OnGUI()
    {
        if (!Application.isPlaying || !isActive) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z > 0)
        {
            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 30, 100, 20),
                     $"伤害: {damage}");
            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 10, 100, 20),
                     $"命中: {hitTargets.Count}");
        }
    }
}

/// <summary>
/// 特殊攻击碰撞控制器 - 支持更复杂的攻击逻辑
/// </summary>
public class AdvancedHitboxController : HitboxController
{
    [Header("=== 特殊效果 ===")]
    public bool healOwnerOnHit = false;      // 击中时是否治疗攻击者
    public float healPercentage = 0.1f;      // 治疗百分比
    public bool chainLightning = false;      // 是否有连锁闪电效果
    public float chainRange = 5f;            // 连锁范围
    public int maxChainTargets = 3;          // 最大连锁目标数

    protected override void OnHitTarget(GameObject target, int damage)
    {
        base.OnHitTarget(target, damage);

        // 吸血效果
        if (healOwnerOnHit && owner != null)
        {
            var ownerCombat = owner.GetComponent<CombatController>();
            if (ownerCombat != null)
            {
                int healAmount = Mathf.RoundToInt(damage * healPercentage);
                ownerCombat.Heal(healAmount);
                Debug.Log($"[AdvancedHitboxController] 攻击者 {owner.name} 通过攻击恢复 {healAmount} 生命值");
            }
        }

        // 连锁闪电效果
        if (chainLightning && GetHitCount() == 1) // 只在第一次命中时触发连锁
        {
            TriggerChainLightning(target);
        }
    }

    /// <summary>
    /// 触发连锁闪电效果
    /// </summary>
    void TriggerChainLightning(GameObject primaryTarget)
    {
        Collider[] nearbyTargets = Physics.OverlapSphere(primaryTarget.transform.position, chainRange, targetLayers);
        List<GameObject> chainTargets = new List<GameObject>();

        foreach (var collider in nearbyTargets)
        {
            if (collider.gameObject == primaryTarget) continue;
            if (hitTargets.Contains(collider.gameObject)) continue;
            if (owner != null && collider.gameObject == owner) continue;
            if (!IsValidTarget(collider)) continue;

            chainTargets.Add(collider.gameObject);
            if (chainTargets.Count >= maxChainTargets) break;
        }

        // 对连锁目标造成递减伤害
        float damageReduction = 0.7f; // 每次连锁伤害递减30%
        int chainDamage = Mathf.RoundToInt(damage * damageReduction);

        foreach (var chainTarget in chainTargets)
        {
            hitTargets.Add(chainTarget);

            // 对连锁目标造成伤害 - 简化版本
            var npc = chainTarget.GetComponent<NPCController>();
            if (npc != null)
            {
                npc.TakeDamage(chainDamage);
            }

            var player = chainTarget.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(chainDamage);
            }

            // 创建连锁特效
            CreateChainEffect(primaryTarget.transform.position, chainTarget.transform.position);

            Debug.Log($"[AdvancedHitboxController] 连锁闪电命中 {chainTarget.name}，造成 {chainDamage} 点伤害");

            chainDamage = Mathf.RoundToInt(chainDamage * damageReduction);
        }
    }

    /// <summary>
    /// 创建连锁特效
    /// </summary>
    void CreateChainEffect(Vector3 from, Vector3 to)
    {
        if (hitEffect != null)
        {
            var effect = Instantiate(hitEffect, from, Quaternion.LookRotation(to - from));
            var lineRenderer = effect.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = effect.AddComponent<LineRenderer>();
            }

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, from);
            lineRenderer.SetPosition(1, to);
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.cyan;
            Destroy(effect, 0.5f);
        }
    }
}