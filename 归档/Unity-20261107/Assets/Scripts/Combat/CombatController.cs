using UnityEngine;

/// <summary>
/// ��ɫս�������� - ������������ܡ������ȹ��ܣ���BuffController��ȫ����
/// </summary>
public class CombatController : MonoBehaviour
{
    [Header("=== ��ɫ���� ===")]
    public CharacterType characterType = CharacterType.Player;
    public bool enablePlayerInput = true; // �Ƿ���������������

    [Header("=== �������� ===")]
    public Weapon currentWeapon;
    public GameObject defaultHitboxPrefab;
    public float defaultHitboxRange = 3f;
    public int defaultHitboxDamage = 10;
    public float defaultHitboxDuration = 0.2f;
    public Vector3 defaultHitboxOffset = new Vector3(0, 0.5f, 1f);

    [Header("=== ���ܲ� (1-4) ===")]
    public Skill[] skillSlots = new Skill[4];

    [Header("=== ս������ ===")]
    public float baseAttackCooldown = 0.5f;
    public bool autoEnterCombatOnAttack = true;

    [Header("=== �������� ===")]
    public bool showDebugInfo = false;

    // �������
    private StateController stateController;
    private AttributeController attributeController;
    private BuffController buffController;
    private Animator animator;

    // ս��״̬
    private float lastAttackTime = 0f;
    private bool isAttacking = false;

    // �¼�
    public System.Action OnAttackPerformed;
    public System.Action<int> OnSkillCast; // ����ʩ��
    public System.Action<Weapon> OnWeaponChanged;
    public System.Action OnCombatStart;
    public System.Action OnCombatEnd;

    // ���Է�����
    public float CurrentAttackCooldown => baseAttackCooldown / Mathf.Max(AttackSpeed, 0.1f);
    public bool CanAttack => Time.time - lastAttackTime >= CurrentAttackCooldown &&
                            stateController != null &&
                            stateController.CanAttack();

    public bool IsAttacking => isAttacking;
    public Weapon CurrentWeapon => currentWeapon;

    // ������ս������ - ����BUFF�ȼӳ�
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
        UpdateAttackState();
    }

    void UpdateAttackState()
    {
        // ��鹥�������Ƿ����
        if (isAttacking && animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName("Attack") && stateInfo.normalizedTime > 0.9f)
            {
                isAttacking = false;
            }
        }
    }

    // ========== �������� ==========

    /// <summary>
    /// ִ�й���
    /// </summary>
    public void PerformAttack()
    {
        if (!CanAttack) return;

        lastAttackTime = Time.time;
        isAttacking = true;

        // �Զ�����ս��״̬
        if (autoEnterCombatOnAttack && stateController != null)
        {
            stateController.EnterCombatState();
        }

        // ���Ź�������
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // ���ɹ�����ײ��
        SpawnAttackHitbox();

        OnAttackPerformed?.Invoke();

        if (showDebugInfo)
        {
            Debug.Log($"[CombatController] ִ�й��� - ������: {AttackPower}");
        }
    }

    /// <summary>
    /// ���ɹ�����ײ��
    /// </summary>
    void SpawnAttackHitbox()
    {
        GameObject prefab = defaultHitboxPrefab;
        float range = defaultHitboxRange;
        int damage = defaultHitboxDamage + AttackPower;
        float duration = defaultHitboxDuration;
        Vector3 offset = defaultHitboxOffset;

        // �����������ʹ����������ײ������
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
                Debug.Log($"[CombatController] ������ײ�� - �˺�: {damage}, ��Χ: {range}, ����: {duration}");
            }
        }
    }

    // ========== ���ܹ��� ==========

    /// <summary>
    /// ʩ�ż���
    /// </summary>
    public void CastSkill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skillSlots.Length)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[CombatController] ����ʩ�ų�����Χ: {skillIndex}");
            }
            return;
        }

        var skill = skillSlots[skillIndex];
        if (skill == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[CombatController] ���ܲ� {skillIndex + 1} Ϊ��");
            }
            return;
        }

        if (!skill.CanUse())
        {
            if (showDebugInfo)
            {
                float remainingCooldown = skill.GetRemainingCooldown();
                Debug.Log($"[CombatController] ���� {skill.skillName} ��ȴ�У�ʣ��: {remainingCooldown:F1}��");
            }
            return;
        }

        // ���ż��ܶ���
        if (animator != null)
        {
            animator.SetTrigger($"Skill{skillIndex + 1}");
        }

        // ʹ�ü���
        skill.Use();

        OnSkillCast?.Invoke(skillIndex);

        if (showDebugInfo)
        {
            Debug.Log($"[CombatController] ʩ�ż���: {skill.skillName} (��λ {skillIndex + 1})");
        }
    }

    /// <summary>
    /// ���ü��ܵ�ָ����λ
    /// </summary>
    public void SetSkill(int slotIndex, Skill skill)
    {
        if (slotIndex >= 0 && slotIndex < skillSlots.Length)
        {
            skillSlots[slotIndex] = skill;

            if (showDebugInfo)
            {
                string skillName = skill?.skillName ?? "��";
                Debug.Log($"[CombatController] ���ü��ܲ� {slotIndex + 1}: {skillName}");
            }
        }
    }

    /// <summary>
    /// ��ȡ������Ϣ
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
    /// ��鼼���Ƿ����
    /// </summary>
    public bool IsSkillAvailable(int slotIndex)
    {
        var skill = GetSkill(slotIndex);
        return skill != null && skill.CanUse();
    }

    /// <summary>
    /// ��ȡ����ʣ����ȴʱ��
    /// </summary>
    public float GetSkillCooldown(int slotIndex)
    {
        var skill = GetSkill(slotIndex);
        return skill?.GetRemainingCooldown() ?? 0f;
    }

    // ========== �������� ==========

    /// <summary>
    /// װ������
    /// </summary>
    public void EquipWeapon(Weapon weapon)
    {
        Weapon oldWeapon = currentWeapon;
        currentWeapon = weapon;

        OnWeaponChanged?.Invoke(currentWeapon);

        if (showDebugInfo)
        {
            string oldName = oldWeapon?.weaponName ?? "��";
            string newName = currentWeapon?.weaponName ?? "��";
            Debug.Log($"[CombatController] ��������: {oldName} -> {newName}");
        }
    }

    /// <summary>
    /// ж������
    /// </summary>
    public void UnequipWeapon()
    {
        EquipWeapon(null);
    }

    // ========== ս��״̬���� ==========

    /// <summary>
    /// ����ս��״̬
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
            Debug.Log("[CombatController] ����ս��״̬");
        }
    }

    /// <summary>
    /// �˳�ս��״̬
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
            Debug.Log("[CombatController] �˳�ս��״̬");
        }
    }

    // ========== ���˴��� ==========

    /// <summary>
    /// �ܵ��˺�
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (stateController != null && stateController.IsInDialogue) return;

        // ͨ��״̬�����������˺�
        var stateManager = GetComponent<CharacterStateManager>();
        if (stateManager != null)
        {
            // Ӧ������BUFF���˺�����
            if (buffController != null)
            {
                float vulnerableMultiplier = buffController.GetBuffMultiplier(BuffType.Vulnerable);
                damage = Mathf.RoundToInt(damage * vulnerableMultiplier);
            }

            stateManager.TakeDamage(damage);
        }

        // �������˶���
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }

        // �Զ�����ս��״̬
        if (autoEnterCombatOnAttack && stateController != null)
        {
            stateController.EnterCombatState();
        }

        // ����Ƿ�����
        if (attributeController != null && attributeController.Health <= 0)
        {
            HandleDeath();
        }

        if (showDebugInfo)
        {
            int currentHealth = attributeController?.Health ?? 0;
            int maxHealth = attributeController?.MaxHealth ?? 0;
            Debug.Log($"[CombatController] �ܵ��˺�: {damage}, ʣ������ֵ: {currentHealth}/{maxHealth}");
        }
    }

    /// <summary>
    /// ��������
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
            Debug.Log("[CombatController] �������");
        }
    }

    // ========== �����ӿ� ==========

    /// <summary>
    /// ���ù�����ȴʱ��
    /// </summary>
    public void SetAttackCooldown(float cooldown)
    {
        baseAttackCooldown = Mathf.Max(0f, cooldown);
    }

    /// <summary>
    /// ǿ�����ù�����ȴ
    /// </summary>
    public void ResetAttackCooldown()
    {
        lastAttackTime = 0f;
    }

    /// <summary>
    /// ��ȡ������ȴʣ��ʱ��
    /// </summary>
    public float GetAttackCooldownRemaining()
    {
        return Mathf.Max(0f, CurrentAttackCooldown - (Time.time - lastAttackTime));
    }

    /// <summary>
    /// ������м��ܲ�
    /// </summary>
    public void ClearAllSkills()
    {
        for (int i = 0; i < skillSlots.Length; i++)
        {
            skillSlots[i] = null;
        }

        if (showDebugInfo)
        {
            Debug.Log("[CombatController] ������м��ܲ�");
        }
    }

    /// <summary>
    /// Ӧ��BUFFЧ��������
    /// </summary>
    public void ApplyBuffsToAttack(ref int damage, ref float speed)
    {
        if (buffController != null)
        {
            // Ӧ�ù�����BUFF
            damage += Mathf.RoundToInt(buffController.GetBuffedValue(BuffType.Attack));

            // Ӧ�ù����ٶ�BUFF
            speed += buffController.GetBuffedValue(BuffType.AttackSpeed);

            // Ӧ���ٶȱ���
            speed *= buffController.GetBuffMultiplier(BuffType.AttackSpeed);
        }
    }

    /// <summary>
    /// ����
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
            Debug.Log($"[CombatController] ����: {amount}������ֵ");
        }
    }

    // ========== ���Թ��� ==========

    [ContextMenu("���Թ���")]
    void DebugAttack()
    {
        PerformAttack();
    }

    [ContextMenu("���Լ���1")]
    void DebugSkill1()
    {
        CastSkill(0);
    }

    [ContextMenu("��������")]
    void DebugTakeDamage()
    {
        TakeDamage(20);
    }

    [ContextMenu("��������")]
    void DebugHeal()
    {
        Heal(50);
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        // ��ʾ������Χ
        float range = currentWeapon?.hitboxRange ?? defaultHitboxRange;
        Vector3 offset = currentWeapon?.hitboxOffset ?? defaultHitboxOffset;
        Vector3 attackPos = transform.position + transform.forward * offset.z + Vector3.up * offset.y;

        Gizmos.color = isAttacking ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(attackPos, range);

        // ��ʾ��������
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        // ��ʾս��״̬
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

        GUILayout.Label("ս�����ܵ���", GUI.skin.label);
        GUILayout.Space(5);

        GUILayout.Label($"������: {AttackPower}");
        GUILayout.Label($"�����ٶ�: {AttackSpeed:F1}");
        GUILayout.Label($"����ǿ��: {SpellPower}");
        GUILayout.Space(5);

        GUILayout.Label($"������ȴ: {GetAttackCooldownRemaining():F1}s");
        GUILayout.Label($"���ڹ���: {isAttacking}");
        GUILayout.Space(5);

        string weaponName = currentWeapon?.weaponName ?? "������";
        GUILayout.Label($"��ǰ����: {weaponName}");
        GUILayout.Space(5);

        // ��ʾBUFF�ӳ�
        if (buffController != null)
        {
            GUILayout.Label("BUFF�ӳ�:");
            GUILayout.Label($"  ����: +{buffController.GetBuffedValue(BuffType.Attack):F0}");
            GUILayout.Label($"  ����: +{buffController.GetBuffedValue(BuffType.AttackSpeed):F0}");
            GUILayout.Label($"  ��ǿ: +{buffController.GetBuffedValue(BuffType.SpellPower):F0}");
            GUILayout.Label($"  ����: x{buffController.GetBuffMultiplier(BuffType.Vulnerable):F2}");
            GUILayout.Space(5);
        }

        GUILayout.Label("����״̬:");
        for (int i = 0; i < skillSlots.Length; i++)
        {
            var skill = skillSlots[i];
            if (skill != null)
            {
                float cooldown = skill.GetRemainingCooldown();
                string status = cooldown > 0 ? $"��ȴ {cooldown:F1}s" : "����";
                GUILayout.Label($"����{i + 1}: {skill.skillName} ({status})");
            }
            else
            {
                GUILayout.Label($"����{i + 1}: ��");
            }
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
