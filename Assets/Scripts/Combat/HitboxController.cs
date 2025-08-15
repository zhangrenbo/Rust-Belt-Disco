using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ������ײ������ - ���������Χ�����˺�����BUFFϵͳ�������˼ӳ�
/// </summary>
[RequireComponent(typeof(Collider))]
public class HitboxController : MonoBehaviour
{
    [Header("=== �������� ===")]
    public float hitboxRange = 3f;       // Hitbox�ķ�Χ����Ҫ���ڿ��ӻ���
    public int damage = 10;              // �����˺�
    public float duration = 0.2f;        // Hitbox����ʱ��

    [Header("=== Ч������ ===")]
    public GameObject hitEffect;         // ����Ч��
    public AudioClip hitSound;           // ������Ч
    public bool destroyOnHit = true;     // ���к��Ƿ�����

    [Header("=== Ŀ����� ===")]
    public LayerMask targetLayers = -1;  // �ɹ����Ĳ㼶
    public bool hitPlayers = true;       // �Ƿ񹥻����
    public bool hitNPCs = true;          // �Ƿ񹥻�NPC
    public bool hitEnemies = true;       // �Ƿ񹥻�����

    [Header("=== BUFFЧ�� ===")]
    public bool applyBuffsOnHit = false; // �Ƿ�������ʱӦ��BUFF
    // �����buffsToApply���飬���İ��ݲ����

    [Header("=== �߼����� ===")]
    public bool canPierce = false;       // �Ƿ���Դ�͸
    public int maxPierceTargets = 3;     // ��ഩ͸Ŀ����
    public float knockbackForce = 0f;    // ��������
    public bool randomizeDamage = true;  // �Ƿ�������˺�
    public float damageVariance = 0.2f;  // �˺��仯��Χ

    // �ܱ����ı��������̳������
    protected Collider hitboxCollider;
    protected HashSet<GameObject> hitTargets = new HashSet<GameObject>();
    protected GameObject owner;            // ����������
    protected bool isActive = true;

    void Awake()
    {
        // ��ȡ������Ϊ������
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
    }

    /// <summary>
    /// ��ʼ�� Hitbox ������������Ի�
    /// </summary>
    public void Setup(float range, int dmg, float dur, GameObject attacker = null)
    {
        hitboxRange = range;
        damage = dmg;
        duration = dur;
        owner = attacker;

        // ���� range �������Ŵ�С
        transform.localScale = new Vector3(range, 1f, range);

        // ���� duration ����Զ�����
        Destroy(gameObject, duration);

        if (owner != null)
        {
            Debug.Log($"[HitboxController] Hitbox���� - �˺�: {damage}, ��Χ: {range}, ����: {duration}��, ������: {owner.name}");
        }
        else
        {
            Debug.Log($"[HitboxController] Hitbox���� - �˺�: {damage}, ��Χ: {range}, ����: {duration}��");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // �����ظ�����ͬһĿ��
        if (hitTargets.Contains(other.gameObject)) return;

        // ���˹���������
        if (owner != null && other.gameObject == owner) return;

        // ���Ŀ�����ͺͲ㼶
        if (!IsValidTarget(other)) return;

        // �������
        ProcessHit(other);
    }

    /// <summary>
    /// ����Ƿ�Ϊ��ЧĿ�� - ͳһʹ��IDamageable�ӿ�
    /// </summary>
    protected bool IsValidTarget(Collider target)
    {
        // 1) ����ˣ�����ԭ�е� targetLayers��
        if ((targetLayers.value & (1 << target.gameObject.layer)) == 0)
            return false;

        // 2) �ӿڹ��ˣ�֧��������ڸ��ڵ�/���ڵ㣩
        var damageable = target.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return false;

        // 3) �����Լ�
        if (owner != null && target.gameObject == owner)
            return false;

        return true;
    }

    /// <summary>
    /// ����������� - ͳһͨ��IDamageable�ӿ�
    /// </summary>
    void ProcessHit(Collider target)
    {
        // ��¼�ѹ�����Ŀ��
        hitTargets.Add(target.gameObject);

        // ���������˺�
        int finalDamage = CalculateDamage(target);

        // ͳһͨ�� IDamageable ����
        var damageable = target.GetComponentInParent<IDamageable>();
        bool hitSuccess = false;

        if (damageable != null)
        {
            damageable.TakeDamage(finalDamage);
            hitSuccess = true;
            Debug.Log($"[HitboxController] ���� {target.name}����� {finalDamage} ���˺�");
        }

        if (hitSuccess)
        {
            // ��������Ч��
            PlayHitEffects(target.transform.position);

            // Ӧ�û���Ч��
            if (knockbackForce > 0)
            {
                ApplyKnockback(target);
            }

            // Ӧ��BUFFЧ�� - �򻯰��ݲ�ʵ��ϸ��
            if (applyBuffsOnHit)
            {
                Debug.Log($"[HitboxController] �� {target.name} Ӧ��BUFFЧ��");
            }

            // �������лص�
            OnHitTarget(target.gameObject, finalDamage);

            // �������þ������к�����
            if (destroyOnHit)
            {
                isActive = false;
                hitboxCollider.enabled = false;
                Destroy(gameObject, 0.1f); // ��΢�ӳ����٣���Ч������
            }
            // ��鴩͸�߼�
            else if (!canPierce || GetHitCount() >= maxPierceTargets)
            {
                isActive = false;
                hitboxCollider.enabled = false;
                Destroy(gameObject, 0.1f);
            }
        }
    }

    /// <summary>
    /// ���������˺����򻯰汾����BUFFϵͳϸ��
    /// </summary>
    int CalculateDamage(Collider target)
    {
        int finalDamage = damage;

        // ������˺�
        if (randomizeDamage && damageVariance > 0)
        {
            float variance = 1f + Random.Range(-damageVariance, damageVariance);
            finalDamage = Mathf.RoundToInt(finalDamage * variance);
        }

        // ȷ���˺�����Ϊ1
        return Mathf.Max(1, finalDamage);
    }

    /// <summary>
    /// ��������Ч��
    /// </summary>
    void PlayHitEffects(Vector3 hitPosition)
    {
        // ����������Ч
        if (hitEffect != null)
        {
            var effect = Instantiate(hitEffect, hitPosition, Quaternion.identity);
            Destroy(effect, 2f); // 2���������Ч
        }

        // ����������Ч
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, hitPosition);
        }
    }

    /// <summary>
    /// Ӧ�û���Ч��
    /// </summary>
    void ApplyKnockback(Collider target)
    {
        var rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 knockbackDirection = (target.transform.position - transform.position).normalized;
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);

            Debug.Log($"[HitboxController] �� {target.name} ʩ�ӻ����� {knockbackForce}");
        }
    }

    /// <summary>
    /// Ӧ��BUFFЧ�� - �򻯰汾
    /// </summary>
    void ApplyBuffs(Collider target)
    {
        // �򻯵�BUFFӦ���߼�����������ϸ��
        Debug.Log($"[HitboxController] �� {target.name} Ӧ��BUFFЧ��");
    }

    /// <summary>
    /// ����Ŀ��ʱ�Ļص��¼�
    /// </summary>
    protected virtual void OnHitTarget(GameObject target, int damage)
    {
        // ���������д���������ʵ�ֶ������Ч����
        // ����������������ʩ������BUFF��
    }

    // ========== �����ӿ� ==========

    /// <summary>
    /// ���ù�����
    /// </summary>
    public void SetOwner(GameObject attacker)
    {
        owner = attacker;
    }

    /// <summary>
    /// �����˺�ֵ
    /// </summary>
    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    /// <summary>
    /// ����/���ù�����ײ
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
        hitboxCollider.enabled = active;
    }

    /// <summary>
    /// �������ٹ�����ײ
    /// </summary>
    public void DestroyHitbox()
    {
        SetActive(false);
        Destroy(gameObject);
    }

    /// <summary>
    /// ��ȡ�����е�Ŀ������
    /// </summary>
    public int GetHitCount()
    {
        return hitTargets.Count;
    }

    /// <summary>
    /// ���������Ŀ���¼�������ظ�������
    /// </summary>
    public void ClearHitTargets()
    {
        hitTargets.Clear();
    }

    /// <summary>
    /// ��ȡ���������е�Ŀ��
    /// </summary>
    public List<GameObject> GetHitTargets()
    {
        return new List<GameObject>(hitTargets);
    }

    /// <summary>
    /// ���Ҫʩ�ӵ�BUFF - �򻯰汾
    /// </summary>
    public void AddBuffToApply(string buffName)
    {
        Debug.Log($"[HitboxController] ����BUFF: {buffName}");
        applyBuffsOnHit = true;
    }

    /// <summary>
    /// ���ô�͸����
    /// </summary>
    public void SetPiercing(bool pierce, int maxTargets = 3)
    {
        canPierce = pierce;
        maxPierceTargets = maxTargets;
        destroyOnHit = !pierce; // ��͸ʱ����������
    }

    /// <summary>
    /// ���û�������
    /// </summary>
    public void SetKnockback(float force)
    {
        knockbackForce = force;
    }

    // ========== ���Թ��� ==========

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

        // ��ʾ���˷���
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
        // ��ʾ������Χ
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, hitboxRange * 0.5f);

        // ��ʾ�����е�Ŀ��
        Gizmos.color = Color.yellow;
        foreach (var target in hitTargets)
        {
            if (target != null)
            {
                Gizmos.DrawLine(transform.position, target.transform.position);
                Gizmos.DrawWireCube(target.transform.position + Vector3.up, Vector3.one * 0.5f);
            }
        }

        // ��ʾ����������
        if (owner != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, owner.transform.position);
        }
    }

    // ========== ������ʾ ==========

    void OnGUI()
    {
        if (!Application.isPlaying || !isActive) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z > 0)
        {
            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 30, 100, 20),
                     $"�˺�: {damage}");
            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 10, 100, 20),
                     $"����: {hitTargets.Count}");
        }
    }
}

/// <summary>
/// ���⹥����ײ������ - ֧�ָ߼���������Ĺ����߼�
/// </summary>
public class AdvancedHitboxController : HitboxController
{
    [Header("=== ����Ч�� ===")]
    public bool healOwnerOnHit = false;      // ����ʱ�Ƿ�����������
    public float healPercentage = 0.1f;      // �����ٷֱ�
    public bool chainLightning = false;      // �Ƿ�����������Ч��
    public float chainRange = 5f;            // ������Χ
    public int maxChainTargets = 3;          // �������Ŀ����

    protected override void OnHitTarget(GameObject target, int damage)
    {
        base.OnHitTarget(target, damage);

        // ��ѪЧ��
        if (healOwnerOnHit && owner != null)
        {
            var ownerCombat = owner.GetComponent<CombatController>();
            if (ownerCombat != null)
            {
                int healAmount = Mathf.RoundToInt(damage * healPercentage);
                ownerCombat.Heal(healAmount);
                Debug.Log($"[AdvancedHitboxController] ������ {owner.name} ͨ�������ָ� {healAmount} ����ֵ");
            }
        }

        // ��������Ч��
        if (chainLightning && GetHitCount() == 1) // ֻ�ڵ�һ������ʱ��������
        {
            TriggerChainLightning(target);
        }
    }

    /// <summary>
    /// ������������Ч��
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

        // ������Ŀ����ɵݼ��˺�
        float damageReduction = 0.7f; // ÿ�������˺��ݼ�30%
        int chainDamage = Mathf.RoundToInt(damage * damageReduction);

        foreach (var chainTarget in chainTargets)
        {
            hitTargets.Add(chainTarget);

            // ������Ŀ������˺� - ͨ��IDamageableͳһ����
            var damageable = chainTarget.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(chainDamage);
            }

            // ����������Ч
            CreateChainEffect(primaryTarget.transform.position, chainTarget.transform.position);

            Debug.Log($"[AdvancedHitboxController] ������������ {chainTarget.name}����� {chainDamage} ���˺�");

            chainDamage = Mathf.RoundToInt(chainDamage * damageReduction);
        }
    }

    /// <summary>
    /// ����������Ч
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