using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// NPC����ö��
/// </summary>
public enum NPCType
{
    Enemy,      // �ж�
    Neutral,    // ����
    Friendly    // �Ѻ�
}

/// <summary>
/// NPC״̬ö��
/// </summary>
public enum NPCState
{
    Idle,       // ����
    Patrol,     // Ѳ��
    Approach,   // �ӽ�
    Attack,     // ����
    Wait,       // �ȴ�
    Dead        // ����
}

/// <summary>
/// NPC����ȥ�`��` - ͨ�ð汾������NPC��AI��Ϊ��״̬����
/// </summary>
public class NPCController : MonoBehaviour, IDamageable
{
    [Header("=== �������� ===")]
    public NPCType npcType = NPCType.Enemy;
    public float detectionRange = 10f;
    public float approachRange = 8f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    [Header("=== Ѳ��·�� ===")]
    public Transform[] patrolPoints;
    public bool loopPatrol = true;
    private int currentPatrolIndex = 0;

    [Header("=== �ƶ����� ===")]
    public float moveSpeed = 3.5f;

    [Header("=== ������UI ===")]
    public GameObject healthBarPrefab;
    private HealthUIController healthUIController;

    [Header("=== �������� ===")]
    public GameObject[] lootPrefabs;
    public float lootDropChance = 0.5f;

    // ========== ������� ==========
    private NavMeshAgent agent;
    private Transform player;
    private Animator animator;
    private CharacterStateManager stateManager;

    // ========== AI״̬ ==========
    private NPCState currentState = NPCState.Idle;
    private float stateTimer = 0f;
    private float nextAttackTime = 0f;

    // ========== ���Է��� ==========
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

    // ========== �¼����� ==========
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
        // ��ȡ�������
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        stateManager = GetComponent<CharacterStateManager>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // ���õ�������
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }

        // ��ʼ��״̬������
        if (stateManager != null)
        {
            stateManager.characterType = CharacterType.NPC;
            stateManager.characterName = gameObject.name;
            stateManager.baseMovementSpeed = moveSpeed;
        }

        // ����������UI
        CreateHealthBar();

        EnterState(NPCState.Idle);

        Debug.Log($"[NPCController] {gameObject.name} NPC��ʼ�����");
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

    // ========== ״̬�л��߼� ==========

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

        // ����״̬�仯�¼�
        OnStateChanged?.Invoke(oldState, newState);
    }

    // ========== ״̬�����߼� ==========

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

    // ========== ս��ϵͳ - ֧��IDamageable�ӿ� ==========

    void PerformAttack()
    {
        // ͨ��IDamageable�ӿ�ͳһ�������������Ӳ����PlayerController
        var damageable = player?.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(10);
            Debug.Log($"[NPCController] {gameObject.name} ������Ŀ��");
        }
    }

    /// <summary>
    /// �ܵ��˺� - IDamageable�ӿ�ʵ��
    /// </summary>
    public void TakeDamage(int dmg)
    {
        if (currentState == NPCState.Dead) return;

        // ����NPC���������Ϊ�ж�
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

        Debug.Log($"[NPCController] {gameObject.name} �ܵ� {dmg} ���˺���ʣ������: {currentHealth}");
    }

    /// <summary>
    /// ��ȡ��ǰ����ֵ - IDamageable�ӿ�ʵ��
    /// </summary>
    public int GetCurrentHealth()
    {
        return stateManager?.currentHealth ?? 0;
    }

    /// <summary>
    /// ��ȡ�������ֵ - IDamageable�ӿ�ʵ��
    /// </summary>
    public int GetMaxHealth()
    {
        return stateManager?.maxHealth ?? 0;
    }

    /// <summary>
    /// ����Ƿ����� - IDamageable�ӿ�ʵ��
    /// </summary>
    public bool IsDead()
    {
        return currentState == NPCState.Dead;
    }

    void Die()
    {
        EnterState(NPCState.Dead);

        // ������Ʒ
        DropLoot();

        // ���������¼�
        OnNPCDeath?.Invoke(this);

        // �ӳ�����
        Destroy(gameObject, 3f);

        Debug.Log($"[NPCController] {gameObject.name} ����");
    }

    void DropLoot()
    {
        if (lootPrefabs == null || lootPrefabs.Length == 0) return;

        foreach (var loot in lootPrefabs)
        {
            if (loot != null && Random.value < lootDropChance)
            {
                Instantiate(loot, transform.position, Quaternion.identity);
            }
        }
    }

    // ========== BUFFϵͳ ==========

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

    // ========== �����ӿ� ==========

    /// <summary>
    /// ����Ѳ��·��
    /// </summary>
    public void SetPatrolPoints(Transform[] points)
    {
        patrolPoints = points;
        currentPatrolIndex = 0;
    }

    /// <summary>
    /// ǿ������NPC״̬
    /// </summary>
    public void ForceSetState(NPCState newState)
    {
        EnterState(newState);
    }

    /// <summary>
    /// ����NPC����
    /// </summary>
    public void SetNPCType(NPCType newType)
    {
        npcType = newType;
    }

    /// <summary>
    /// ��ȡ��ǰ״̬
    /// </summary>
    public NPCState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// ��ȡ����ҵľ���
    /// </summary>
    public float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, player.position);
    }

    /// <summary>
    /// ����Ƿ��ܿ������
    /// </summary>
    public bool CanSeePlayer()
    {
        if (player == null) return false;

        float distance = GetDistanceToPlayer();
        if (distance > detectionRange) return false;

        // �򵥵����߼��
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        RaycastHit hit;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToPlayer, out hit, detectionRange))
        {
            return hit.transform == player;
        }

        return true;
    }

    /// <summary>
    /// �����ƶ��ٶ�
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
    /// ����/����AI
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
    /// ����ֹͣ������Ϊ
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
    /// ǿ�ƹ���ָ��Ŀ��
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
    /// ���õ���ʼ״̬
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

    // ========== ���Է��� ==========

    void OnDrawGizmosSelected()
    {
        // ���Ƽ�ⷶΧ
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // ���ƹ�����Χ
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // ����Ѳ��·��
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

            // ������ǰĿ���
            if (currentPatrolIndex < patrolPoints.Length && patrolPoints[currentPatrolIndex] != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(patrolPoints[currentPatrolIndex].position, 0.3f);
            }
        }

        // ���Ƶ���ҵ�����
        if (player != null && GetDistanceToPlayer() <= detectionRange)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // �������߼��
        if (Application.isPlaying && CanSeePlayer())
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f,
                          (player.position - transform.position).normalized * detectionRange);
        }
    }

    void OnDrawGizmos()
    {
        // ��ʾ��ǰ״̬
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

            // ��ʾNPC����
            Gizmos.color = npcType == NPCType.Enemy ? Color.red : Color.blue;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 3f, Vector3.one * 0.3f);
        }

        // ��ʾ������ⷶΧ����͸����
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, detectionRange);
    }

    // ========== ����������� ==========

    void OnDestroy()
    {
        // �����¼�����
        OnStateChanged = null;
        OnPlayerDetected = null;
        OnPlayerLost = null;
        OnNPCDeath = null;
    }

    void OnDisable()
    {
        // ֹͣ������Ϊ
        if (agent != null)
        {
            agent.isStopped = true;
        }
    }

    void OnEnable()
    {
        // �ָ���Ϊ�����֮ǰ�Ǽ���״̬��
        if (Application.isPlaying && currentState != NPCState.Dead)
        {
            if (agent != null && currentState != NPCState.Idle)
            {
                agent.isStopped = false;
            }
        }
    }
}