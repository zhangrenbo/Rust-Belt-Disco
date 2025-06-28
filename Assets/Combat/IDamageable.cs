using UnityEngine;

/// <summary>
/// 可伤害接口 - 任何可以受到伤害的对象都应该实现这个接口
/// 这个接口定义了伤害系统的基本契约
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害数值</param>
    void TakeDamage(int damage);

    /// <summary>
    /// 获取当前生命值
    /// </summary>
    /// <returns>当前生命值</returns>
    int GetCurrentHealth();

    /// <summary>
    /// 获取最大生命值
    /// </summary>
    /// <returns>最大生命值</returns>
    int GetMaxHealth();

    /// <summary>
    /// 检查是否已死亡
    /// </summary>
    /// <returns>是否死亡</returns>
    bool IsDead();
}

/// <summary>
/// 可治疗接口 - 可以恢复生命值的对象
/// </summary>
public interface IHealable
{
    /// <summary>
    /// 治疗恢复生命值
    /// </summary>
    /// <param name="amount">治疗数值</param>
    void Heal(int amount);

    /// <summary>
    /// 生命值属性
    /// </summary>
    int Health { get; set; }
}

/// <summary>
/// 可互动接口 - 玩家可以与之互动的对象
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// 执行互动
    /// </summary>
    /// <param name="interactor">互动发起者</param>
    void Interact(GameObject interactor);

    /// <summary>
    /// 检查是否可以互动
    /// </summary>
    /// <param name="interactor">互动发起者</param>
    /// <returns>是否可以互动</returns>
    bool CanInteract(GameObject interactor);

    /// <summary>
    /// 获取互动提示文本
    /// </summary>
    /// <returns>提示文本</returns>
    string GetInteractionPrompt();
}

/// <summary>
/// 可收集接口 - 玩家可以收集的物品
/// </summary>
public interface ICollectable
{
    /// <summary>
    /// 被收集时调用
    /// </summary>
    /// <param name="collector">收集者</param>
    void OnCollected(GameObject collector);

    /// <summary>
    /// 检查是否可以被收集
    /// </summary>
    /// <param name="collector">收集者</param>
    /// <returns>是否可以收集</returns>
    bool CanBeCollected(GameObject collector);

    /// <summary>
    /// 获取物品价值
    /// </summary>
    /// <returns>物品价值</returns>
    int GetValue();
}

/// <summary>
/// 可使用接口 - 可以被使用的物品或道具
/// </summary>
public interface IUsable
{
    /// <summary>
    /// 使用物品
    /// </summary>
    /// <param name="user">使用者</param>
    void Use(GameObject user);

    /// <summary>
    /// 检查是否可以使用
    /// </summary>
    /// <param name="user">使用者</param>
    /// <returns>是否可以使用</returns>
    bool CanUse(GameObject user);

    /// <summary>
    /// 获取使用说明
    /// </summary>
    /// <returns>使用说明</returns>
    string GetUsageDescription();
}

/// <summary>
/// 可激活接口 - 可以被激活/禁用的对象
/// </summary>
public interface IActivatable
{
    /// <summary>
    /// 激活对象
    /// </summary>
    void Activate();

    /// <summary>
    /// 禁用对象
    /// </summary>
    void Deactivate();

    /// <summary>
    /// 检查是否已激活
    /// </summary>
    /// <returns>是否已激活</returns>
    bool IsActive();

    /// <summary>
    /// 切换激活状态
    /// </summary>
    void Toggle();
}