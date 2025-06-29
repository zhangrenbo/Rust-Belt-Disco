using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// NPC类型枚举
/// </summary>
public enum NPCType
{
    Enemy,      // 敌对
    Neutral,    // 中立
    Friendly    // 友好
}

/// <summary>
/// NPC状态枚举
/// </summary>
public enum NPCState
{
    Idle,       // 空闲
    Patrol,     // 巡逻
    Approach,   // 接近
    Attack,     // 攻击
    Wait,       // 等待
    Dead        // 死亡
}

/// <summary>
/// NPCコントローラー - 通用版本，处理NPC的AI行为和状态管理
/// </summary>
public class NPCController : MonoBehaviour
{
    [Header("=== 基本设置 ===")]
    public NPCType npcType = NPCType.Enemy;
    public float detectionRange = 10f;
    public float approachRange = 8f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    private BuffController buffController;
        buffController = GetComponent<BuffController>();
    [Header("=== 巡逻路径 ===")]
    public Transform[] patrolPoints;
    public bool loopPatrol = true;
    private int currentPatrolIndex = 0;

    [Header("=== 移动设置 ===")]
    public float moveSpeed = 3.5f;

    [Header("=== 生命条UI ===")]
    public GameObject healthBarPrefab;
    private HealthUIController healthUIController;

    [Header("=== 掉落设置 ===")]
    public GameObject[] lootPrefabs;
    public float lootDropChance = 0.5f;

    // ========== 组件引用 ==========
    private NavMeshAgent agent;
    private Transform player;
    private Animator animator;
    private CharacterStateManager stateManager;

    // ========== AI状态 ==========
    private NPCState currentState = NPCState.Idle;
    private float stateTimer = 0f;
    private float nextAttackTime = 0f;

    // ========== 属性访问 ==========
    public int maxHealth
    {
        get { return stateManager?.maxHealth ?? 100; }
    }

    public int currentHealth
    {
        get { return stateManager?.currentHealth ?? 100; }
    }

    public bool isDead
    {
        get { return currentState == NPCState.Dead; }
    }

    // ========== 事件定义 ==========
    public System.Action<NPCState, NPCState> OnStateChanged;
    public System.Action<Transform> OnPlayerDetected;
    public System.Action OnPlayerLost;
    public System.Action<NPCController> OnNPCDeath;

    void Start()
    {
        InitializeNPC();
    }

    void InitializeNPC()
    {
        // 获取组件引用
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        stateManager = GetComponent<CharacterStateManager>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 设置导航代理
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        // 初始化状态管理器
        if (stateManager != null)
        {
            stateManager.characterType = CharacterType.NPC;
            stateManager.characterName = gameObject.name;
            stateManager.baseMovementSpeed = moveSpeed;
        }

        // 创建生命条UI
        CreateHealthBar();

        EnterState(NPCState.Idle);

        Debug.Log($"[NPCController] {gameObject.name} NPC初始化完成");
    }

    void CreateHealthBar()
    {
        if (healthBarPrefab != null)
        {
            var hb = Instantiate(healthBarPrefab, transform);
            hb.transform.localPosition = Vector3.up * 2f;
            healthUIController = hb.GetComponent<HealthUIController>();
            if (healthUIController != null)
            {
                healthUIController.SetMaxHealth(maxHealth);
                healthUIController.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case NPCState.Idle:
                UpdateIdle();
                break;
            case NPCState.Patrol:
                UpdatePatrol();
                break;
            case NPCState.Approach:
                UpdateApproach();
                break;
            case NPCState.Attack:
                UpdateAttack();
                break;
            case NPCState.Wait:
                UpdateWait();
                break;
            case NPCState.Dead:
                break;
        }
    }

    // ========== 状态切换逻辑 ==========

    void EnterState(NPCState newState)
    {
        NPCState oldState = currentState;
        currentState = newState;
        stateTimer = 0f;

        switch (newState)
        {
            case NPCState.Idle:
                if (agent != null) agent.isStopped = true;
                if (animator != null) animator.SetTrigger("Idle");
                if (stateManager != null) stateManager.SetState(CharacterState.Idle);
                break;

            case NPCState.Patrol:
                if (patrolPoints.Length > 0 && agent != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                    if (animator != null) animator.SetTrigger("Walk");
                    if (stateManager != null) stateManager.SetState(CharacterState.Walking);
                }
                break;

            case NPCState.Approach:
                if (agent != null) agent.isStopped = false;
                if (animator != null) animator.SetTrigger("Run");
                if (stateManager != null) stateManager.SetState(CharacterState.Running);
                if (healthUIController != null) healthUIController.gameObject.SetActive(true);
                break;

            case NPCState.Attack:
                if (agent != null) agent.isStopped = true;
                if (animator != null) animator.SetTrigger("Attack");
                if (stateManager != null) stateManager.SetState(CharacterState.Attacking);
                break;

            case NPCState.Wait:
                if (agent != null) agent.isStopped = true;
                if (animator != null) animator.SetTrigger("Idle");
                if (stateManager != null) stateManager.SetState(CharacterState.Idle);
                break;

            case NPCState.Dead:
                if (agent != null) agent.isStopped = true;
                if (animator != null) animator.SetTrigger("Die");
                if (stateManager != null) stateManager.SetState(CharacterState.Dead);
                break;
        }

        // 触发状态变化事件
        OnStateChanged?.Invoke(oldState, newState);
    }

    // ========== 状态更新逻辑 ==========

    void UpdateIdle()
    {
        stateTimer += Time.deltaTime;

        if (player && npcType == NPCType.Enemy && Vector3.Distance(transform.position, player.position) <= detectionRange)
        {
            OnPlayerDetected?.Invoke(player);
            EnterState(NPCState.Approach);
            return;
        }

        if (stateTimer > 2f && patrolPoints.Length > 0)
            EnterState(NPCState.Patrol);
    }

    void UpdatePatrol()
    {
        if (player && npcType == NPCType.Enemy && Vector3.Distance(transform.position, player.position) <= detectionRange)
        {
            OnPlayerDetected?.Invoke(player);
            EnterState(NPCState.Approach);
            return;
        }

        if (agent != null && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            if (loopPatrol)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
            else
            {
                currentPatrolIndex++;
                if (currentPatrolIndex >= patrolPoints.Length)
                {
                    currentPatrolIndex = patrolPoints.Length - 1;
                    EnterState(NPCState.Idle);
                    return;
                }
            }
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    void UpdateApproach()
    {
        if (!player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attackRange)
        {
            EnterState(NPCState.Attack);
            nextAttackTime = Time.time;
        }
        else if (dist > detectionRange)
        {
            OnPlayerLost?.Invoke();
            EnterState(NPCState.Idle);
        }
        else
        {
            if (agent != null) agent.SetDestination(player.position);
        }
    }

    void UpdateAttack()
    {
        if (Time.time >= nextAttackTime)
        {
            PerformAttack();
            nextAttackTime = Time.time + attackCooldown;
        }

        EnterState(NPCState.Wait);
    }

    void UpdateWait()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer >= 1f)
        {
            if (player == null)
            {
                EnterState(NPCState.Idle);
                return;
            }

            float dist = Vector3.Distance(transform.position, player.position);

            if (dist <= attackRange)
                EnterState(NPCState.Attack);
            else if (dist <= detectionRange)
                EnterState(NPCState.Approach);
            else
            {
                OnPlayerLost?.Invoke();
                EnterState(NPCState.Idle);
            }
        }
    }

    // ========== 战斗系统 ==========

    void PerformAttack()
    {
        var pc = player?.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.TakeDamage(10);
            Debug.Log($"[NPCController] {gameObject.name} 攻击了玩家");
        }
    }

    public void TakeDamage(int dmg)
    {
        if (currentState == NPCState.Dead) return;

        // 中立NPC被攻击后变为敌对
        if (npcType == NPCType.Neutral)
            npcType = NPCType.Enemy;

        if (stateManager != null)
        {
            stateManager.TakeDamage(dmg);
        }

        if (healthUIController != null)
        {
            healthUIController.SetCurrentHealth(currentHealth);
            healthUIController.gameObject.SetActive(true);
        }

        if (currentHealth <= 0)
        {
            Die();
        }

        Debug.Log($"[NPCController] {gameObject.name} 受到 {dmg} 点伤害，剩余生命: {currentHealth}");
    }

    void Die()
    {
        EnterState(NPCState.Dead);

        // 掉落物品
        DropLoot();

        // 触发死亡事件
        OnNPCDeath?.Invoke(this);

        buffController?.AddBuff(buff);
    }

        buffController?.RemoveBuff(buff);
        return buffController?.HasBuff(type) ?? false;
        {
            if (loot != null && Random.value < lootDropChance)
            {
                Instantiate(loot, transform.position, Quaternion.identity);
            }
        }
    }

    // ========== BUFF系统 ==========

    public void AddBuff(Buff buff)
    {
        if (stateManager != null)
        {
            stateManager.AddBuff(buff);
        }
    }

    public void RemoveBuff(Buff buff)
    {
        if (stateManager != null)
        {
            stateManager.RemoveBuff(buff);
        }
    }

    public bool HasBuff(BuffType type)
    {
        return stateManager?.HasBuff(type) ?? false;
    }

    // ========== 公共接口 ==========

    /// <summary>
    /// 设置巡逻路径
    /// </summary>
    public void SetPatrolPoints(Transform[] points)
    {
        patrolPoints = points;
        currentPatrolIndex = 0;
    }

    /// <summary>
    /// 强制设置NPC状态
    /// </summary>
    public void ForceSetState(NPCState newState)
    {
        EnterState(newState);
    }

    /// <summary>
    /// 设置NPC类型
    /// </summary>
    public void SetNPCType(NPCType newType)
    {
        npcType = newType;
    }

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public NPCState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// 获取到玩家的距离
    /// </summary>
    public float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, player.position);
    }

    /// <summary>
    /// 检查是否能看到玩家
    /// </summary>
    public bool CanSeePlayer()
    {
        if (player == null) return false;

        float distance = GetDistanceToPlayer();
        if (distance > detectionRange) return false;

        // 简单的视线检测
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        RaycastHit hit;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToPlayer, out hit, detectionRange))
        {
            return hit.transform == player;
        }

        return true;
    }

    /// <summary>
    /// 设置移动速度
    /// </summary>
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }
        if (stateManager != null)
        {
            stateManager.baseMovementSpeed = moveSpeed;
        }
    }

    /// <summary>
    /// 启用/禁用AI
    /// </summary>
    public void SetAIEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (agent != null)
        {
            agent.isStopped = !enabled;
        }
    }

    /// <summary>
    /// 立即停止所有行为
    /// </summary>
    public void StopAllBehavior()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        EnterState(NPCState.Idle);
    }

    /// <summary>
    /// 强制攻击指定目标
    /// </summary>
    public void ForceAttackTarget(Transform target)
    {
        if (target != null)
        {
            player = target;
            EnterState(NPCState.Approach);
        }
    }

    /// <summary>
    /// 重置到初始状态
    /// </summary>
    public void ResetToInitialState()
    {
        currentPatrolIndex = 0;
        if (stateManager != null)
        {
            stateManager.currentHealth = stateManager.maxHealth;
        }

        if (healthUIController != null)
        {
            healthUIController.SetCurrentHealth(currentHealth);
            healthUIController.gameObject.SetActive(false);
        }

        EnterState(NPCState.Idle);
    }

    // ========== 调试方法 ==========

    void OnDrawGizmosSelected()
    {
        // 绘制检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 绘制攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 绘制巡逻路径
        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 0.5f);

                    int nextIndex = loopPatrol ? (i + 1) % patrolPoints.Length : i + 1;
                    if (nextIndex < patrolPoints.Length && patrolPoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                    }
                }
            }

            // 高亮当前目标点
            if (currentPatrolIndex < patrolPoints.Length && patrolPoints[currentPatrolIndex] != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(patrolPoints[currentPatrolIndex].position, 0.3f);
            }
        }

        // 绘制到玩家的连线
        if (player != null && GetDistanceToPlayer() <= detectionRange)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // 绘制视线检测
        if (Application.isPlaying && CanSeePlayer())
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f,
                          (player.position - transform.position).normalized * detectionRange);
        }
    }

    void OnDrawGizmos()
    {
        // 显示当前状态
        if (Application.isPlaying)
        {
            Color stateColor = currentState switch
            {
                NPCState.Idle => Color.white,
                NPCState.Patrol => Color.cyan,
                NPCState.Approach => Color.yellow,
                NPCState.Attack => Color.red,
                NPCState.Wait => Color.magenta,
                NPCState.Dead => Color.black,
                _ => Color.gray
            };

            Gizmos.color = stateColor;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, Vector3.one * 0.5f);

            // 显示NPC类型
            Gizmos.color = npcType == NPCType.Enemy ? Color.red : Color.blue;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.3f);
        }

        // 显示基本检测范围（半透明）
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, detectionRange);
    }

    // ========== 组件生命周期 ==========

    void OnDestroy()
    {
        // 清理事件引用
        OnStateChanged = null;
        OnPlayerDetected = null;
        OnPlayerLost = null;
        OnNPCDeath = null;
    }

    void OnDisable()
    {
        // 停止所有行为
        if (agent != null)
        {
            agent.isStopped = true;
        }
    }

    void OnEnable()
    {
        // 恢复行为（如果之前是激活状态）
        if (Application.isPlaying && currentState != NPCState.Dead)
        {
            if (agent != null && currentState != NPCState.Idle)
            {
                agent.isStopped = false;
            }
        }
    }
}