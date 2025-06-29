using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 角色类型枚举 - 添加缺失的枚举
/// </summary>
public enum CharacterType
{
    Player,   // 玩家
    NPC,      // NPC
    Enemy     // 敌人
}

/// <summary>
/// 玩家状态枚举 - 添加缺失的枚举
/// </summary>
public enum PlayerState
{
    Normal,   // 正常状态
    Combat,   // 战斗状态
    Dialogue, // 对话状态
    Dead      // 死亡状态
}

/// <summary>
/// 角色状态信息类 - 添加缺失的类
/// </summary>
[System.Serializable]
public class CharacterStatusInfo
{
    public string name;
    public CharacterState state;
    public int health;
    public int maxHealth;
    public Vector2 movementDirection;
    public float movementSpeed;
    public int activeBuffCount;
}

/// <summary>
/// 角色状态管理器 - 简化版本，管理角色的所有状态和属性
/// </summary>
public class CharacterStateManager : MonoBehaviour
{
    [Header("=== 角色基本信息 ===")]
    public string characterName = "角色";
    public CharacterType characterType = CharacterType.Player;

    private BuffController buffController;
    public event Action<Buff> OnBuffAdded;
    public event Action<Buff> OnBuffRemoved;
        buffController = GetComponent<BuffController>();
        if (buffController != null)
        {
            buffController.OnBuffAdded += (b) => OnBuffAdded?.Invoke(b);
            buffController.OnBuffRemoved += (b) => OnBuffRemoved?.Invoke(b);
        }
    public int currentHealth = 100;

    [Header("=== 移动系统 ===")]
    public float baseMovementSpeed = 5f;
    public bool canMove = true;

    // ========== 角色状态 ==========
    [Header("=== 角色状态 ===")]
    public CharacterState currentState = CharacterState.Idle;
    public Vector2 movementDirection = Vector2.zero;
    public float movementSpeed = 0f;
    public bool isGrounded = true;

    // ========== BUFF系统 ==========
    private List<GameBuff> activeBuffs = new List<GameBuff>();

    // ========== 事件系统 ==========
    public event Action<CharacterState, CharacterState> OnStateChanged;
    public event Action<Vector2> OnMovementChanged;
    public event Action<GameBuff> OnBuffAdded;
    public event Action<GameBuff> OnBuffRemoved;
    public event Action<int, int> OnHealthChanged;

    void Start()
    {
        InitializeCharacter();
    }

    void Update()
    {
        UpdateBuffs();
        UpdateMovementSpeed();
    }

    void InitializeCharacter()
    {
        currentHealth = maxHealth;
        currentState = CharacterState.Idle;
        Debug.Log($"[CharacterStateManager] {characterName} 初始化完成");
    }

    // ========== 状态管理 ==========

    public void SetState(CharacterState newState)
    {
        if (currentState != newState)
        {
            CharacterState oldState = currentState;
            currentState = newState;
            OnStateChanged?.Invoke(oldState, newState);
            Debug.Log($"[CharacterStateManager] {characterName} 状态改变: {oldState} -> {newState}");
        }
    }

    public void SetMovement(Vector2 direction, float speed = 0f)
    {
        Vector2 oldDirection = movementDirection;
        movementDirection = direction;

        if (speed > 0)
            movementSpeed = speed;
        else
            movementSpeed = direction.magnitude * baseMovementSpeed;

        if (direction.magnitude > 0.1f)
        {
            SetState(speed > baseMovementSpeed * 0.8f ? CharacterState.Running : CharacterState.Walking);
        }
        else if (currentState == CharacterState.Walking || currentState == CharacterState.Running)
        {
            SetState(CharacterState.Idle);
        }

        if (oldDirection != movementDirection)
        {
            OnMovementChanged?.Invoke(movementDirection);
        }
    }

    void UpdateMovementSpeed()
    {
        if (movementDirection.magnitude > 0.1f)
        {
            movementSpeed = movementDirection.magnitude * baseMovementSpeed;
        }
    }

        }
    }

    // ========== 战斗系统 ==========

    public void TakeDamage(int damage)
    {
        if (currentState == CharacterState.Dead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            SetState(CharacterState.Dead);
        }
        else
        {
            SetState(CharacterState.Hit);
        }

        Debug.Log($"[CharacterStateManager] {characterName} 受到 {damage} 点伤害，剩余生命: {currentHealth}");
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[CharacterStateManager] {characterName} 恢复 {amount} 点生命值，当前生命: {currentHealth}");
    }

            activeBuffCount = buffController != null ? buffController.ActiveBuffCount : 0

    public List<Buff> GetActiveBuffs()
    {
        return buffController != null ? new List<Buff>(buffController.ActiveBuffs) : new List<Buff>();
        buffController?.ClearAllBuffs();
        buffController?.AddBuff(buff);
        buffController?.RemoveBuff(buff);
        return new List<GameBuff>(activeBuffs);
    }

    public void ClearAllBuffs()
    {
        var buffsToRemove = new List<GameBuff>(activeBuffs);
        activeBuffs.Clear();
        foreach (var buff in buffsToRemove)
        {
            OnBuffRemoved?.Invoke(buff);
        }
        Debug.Log($"[CharacterStateManager] {characterName} 清除了所有BUFF");
    }

    [ContextMenu("重置角色状态")]
    public void ResetCharacter()
    {
        currentHealth = maxHealth;
        currentState = CharacterState.Idle;
        movementDirection = Vector2.zero;
        movementSpeed = 0f;
        ClearAllBuffs();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[CharacterStateManager] {characterName} 状态已重置");
    }

    // ========== 兼容接口 ==========

    /// <summary>
    /// 兼容接口 - 为了与 Buff 类兼容而添加的重载方法
    /// </summary>
    public void AddBuff(Buff buff)
    {
        if (buff == null) return;

        // 将 Buff 转换为 GameBuff
        GameBuff gameBuff = new GameBuff(buff.buffName, buff.type, buff.value, buff.duration, buff.isPositive);
        AddBuff(gameBuff);
    }

    public void RemoveBuff(Buff buff)
    {
        if (buff == null) return;

        // 查找对应的 GameBuff 并移除
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].buffName == buff.buffName && activeBuffs[i].type == buff.type)
            {
                var buffToRemove = activeBuffs[i];
                activeBuffs.RemoveAt(i);
                OnBuffRemoved?.Invoke(buffToRemove);
                Debug.Log($"[CharacterStateManager] {characterName} 失去BUFF: {buff.type}");
                break;
            }
        }
    }
}