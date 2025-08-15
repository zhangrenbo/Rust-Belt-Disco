using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// ��ɫ����ö�� - ���ȱʧ��ö��
/// </summary>
public enum CharacterType
{
    Player,   // ���
    NPC,      // NPC
    Enemy     // ����
}

/// <summary>
/// ���״̬ö�� - ���ȱʧ��ö��
/// </summary>
public enum PlayerState
{
    Normal,   // ����״̬
    Combat,   // ս��״̬
    Dialogue, // �Ի�״̬
    Dead      // ����״̬
}

/// <summary>
/// ��ɫ״̬��Ϣ�� - ���ȱʧ����
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
/// ��ɫ״̬������ - �򻯰汾�������ɫ������״̬������
/// </summary>
public class CharacterStateManager : MonoBehaviour
{
    [Header("=== ��ɫ������Ϣ ===")]
    public string characterName = "��ɫ";
    public CharacterType characterType = CharacterType.Player;

    [Header("=== �������� ===")]
    public int strength = 10;
    public int agility = 10;
    public int intelligence = 10;
    public int stamina = 10;
    public int vitality = 10;

    [Header("=== ����ֵϵͳ ===")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("=== �ƶ�ϵͳ ===")]
    public float baseMovementSpeed = 5f;
    public bool canMove = true;

    // ========== ��ɫ״̬ ==========
    [Header("=== ��ɫ״̬ ===")]
    public CharacterState currentState = CharacterState.Idle;
    public Vector2 movementDirection = Vector2.zero;
    public float movementSpeed = 0f;
    public bool isGrounded = true;

    // ========== BUFFϵͳ ==========
    private List<GameBuff> activeBuffs = new List<GameBuff>();

    // ========== �¼�ϵͳ ==========
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
        Debug.Log($"[CharacterStateManager] {characterName} ��ʼ�����");
    }

    // ========== ״̬���� ==========

    public void SetState(CharacterState newState)
    {
        if (currentState != newState)
        {
            CharacterState oldState = currentState;
            currentState = newState;
            OnStateChanged?.Invoke(oldState, newState);
            Debug.Log($"[CharacterStateManager] {characterName} ״̬�ı�: {oldState} -> {newState}");
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

    // ========== BUFFϵͳ ==========

    public void AddBuff(GameBuff buff)
    {
        if (buff == null) return;
        activeBuffs.Add(buff);
        OnBuffAdded?.Invoke(buff);
        Debug.Log($"[CharacterStateManager] {characterName} ���BUFF: {buff.type}");
    }

    public void RemoveBuff(GameBuff buff)
    {
        if (buff == null) return;
        if (activeBuffs.Remove(buff))
        {
            OnBuffRemoved?.Invoke(buff);
            Debug.Log($"[CharacterStateManager] {characterName} ʧȥBUFF: {buff.type}");
        }
    }

    public bool HasBuff(BuffType type)
    {
        foreach (var buff in activeBuffs)
        {
            if (buff.type == type) return true;
        }
        return false;
    }

    void UpdateBuffs()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].duration -= Time.deltaTime;
            if (activeBuffs[i].duration <= 0)
            {
                GameBuff expiredBuff = activeBuffs[i];
                activeBuffs.RemoveAt(i);
                OnBuffRemoved?.Invoke(expiredBuff);
                Debug.Log($"[CharacterStateManager] {characterName} BUFF����: {expiredBuff.type}");
            }
        }
    }

    // ========== ս��ϵͳ ==========

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

        Debug.Log($"[CharacterStateManager] {characterName} �ܵ� {damage} ���˺���ʣ������: {currentHealth}");
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[CharacterStateManager] {characterName} �ָ� {amount} ������ֵ����ǰ����: {currentHealth}");
    }

    // ========== �����ӿ� ==========

    public bool CanMove()
    {
        return canMove && currentState != CharacterState.Dead &&
               currentState != CharacterState.Attacking &&
               currentState != CharacterState.Hit;
    }

    public bool CanAttack()
    {
        return currentState != CharacterState.Dead &&
               currentState != CharacterState.Hit;
    }

    public CharacterStatusInfo GetStatusInfo()
    {
        return new CharacterStatusInfo
        {
            name = characterName,
            state = currentState,
            health = currentHealth,
            maxHealth = maxHealth,
            movementDirection = movementDirection,
            movementSpeed = movementSpeed,
            activeBuffCount = activeBuffs.Count
        };
    }

    public List<GameBuff> GetActiveBuffs()
    {
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
        Debug.Log($"[CharacterStateManager] {characterName} ���������BUFF");
    }

    [ContextMenu("���ý�ɫ״̬")]
    public void ResetCharacter()
    {
        currentHealth = maxHealth;
        currentState = CharacterState.Idle;
        movementDirection = Vector2.zero;
        movementSpeed = 0f;
        ClearAllBuffs();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[CharacterStateManager] {characterName} ״̬������");
    }

    // ========== ���ݽӿ� ==========

    /// <summary>
    /// ���ݽӿ� - Ϊ���� Buff ����ݶ���ӵ����ط���
    /// </summary>
    public void AddBuff(Buff buff)
    {
        if (buff == null) return;

        // �� Buff ת��Ϊ GameBuff
        GameBuff gameBuff = new GameBuff(buff.buffName, buff.type, buff.value, buff.duration, buff.isPositive);
        AddBuff(gameBuff);
    }

    public void RemoveBuff(Buff buff)
    {
        if (buff == null) return;

        // ���Ҷ�Ӧ�� GameBuff ���Ƴ�
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].buffName == buff.buffName && activeBuffs[i].type == buff.type)
            {
                var buffToRemove = activeBuffs[i];
                activeBuffs.RemoveAt(i);
                OnBuffRemoved?.Invoke(buffToRemove);
                Debug.Log($"[CharacterStateManager] {characterName} ʧȥBUFF: {buff.type}");
                break;
            }
        }
    }
}