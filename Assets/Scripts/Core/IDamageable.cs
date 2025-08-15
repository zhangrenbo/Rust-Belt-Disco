using UnityEngine;

/// <summary>
/// ���˺��ӿ� - �κο����ܵ��˺��Ķ���Ӧ��ʵ������ӿ�
/// ����ӿڶ������˺�ϵͳ�Ļ�����Լ
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// �ܵ��˺�
    /// </summary>
    /// <param name="damage">�˺���ֵ</param>
    void TakeDamage(int damage);

    /// <summary>
    /// ��ȡ��ǰ����ֵ
    /// </summary>
    /// <returns>��ǰ����ֵ</returns>
    int GetCurrentHealth();

    /// <summary>
    /// ��ȡ�������ֵ
    /// </summary>
    /// <returns>�������ֵ</returns>
    int GetMaxHealth();

    /// <summary>
    /// ����Ƿ�������
    /// </summary>
    /// <returns>�Ƿ�����</returns>
    bool IsDead();
}

/// <summary>
/// �����ƽӿ� - ���Իָ�����ֵ�Ķ���
/// </summary>
public interface IHealable
{
    /// <summary>
    /// ���ƻָ�����ֵ
    /// </summary>
    /// <param name="amount">������ֵ</param>
    void Heal(int amount);

    /// <summary>
    /// ����ֵ����
    /// </summary>
    int Health { get; set; }
}

/// <summary>
/// �ɻ����ӿ� - ��ҿ�����֮�����Ķ���
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// ִ�л���
    /// </summary>
    /// <param name="interactor">����������</param>
    void Interact(GameObject interactor);

    /// <summary>
    /// ����Ƿ���Ի���
    /// </summary>
    /// <param name="interactor">����������</param>
    /// <returns>�Ƿ���Ի���</returns>
    bool CanInteract(GameObject interactor);

    /// <summary>
    /// ��ȡ������ʾ�ı�
    /// </summary>
    /// <returns>��ʾ�ı�</returns>
    string GetInteractionPrompt();
}

/// <summary>
/// ���ռ��ӿ� - ��ҿ����ռ�����Ʒ
/// </summary>
public interface ICollectable
{
    /// <summary>
    /// ���ռ�ʱ����
    /// </summary>
    /// <param name="collector">�ռ���</param>
    void OnCollected(GameObject collector);

    /// <summary>
    /// ����Ƿ���Ա��ռ�
    /// </summary>
    /// <param name="collector">�ռ���</param>
    /// <returns>�Ƿ�����ռ�</returns>
    bool CanBeCollected(GameObject collector);

    /// <summary>
    /// ��ȡ��Ʒ��ֵ
    /// </summary>
    /// <returns>��Ʒ��ֵ</returns>
    int GetValue();
}

/// <summary>
/// ��ʹ�ýӿ� - ���Ա�ʹ�õ���Ʒ�����
/// </summary>
public interface IUsable
{
    /// <summary>
    /// ʹ����Ʒ
    /// </summary>
    /// <param name="user">ʹ����</param>
    void Use(GameObject user);

    /// <summary>
    /// ����Ƿ����ʹ��
    /// </summary>
    /// <param name="user">ʹ����</param>
    /// <returns>�Ƿ����ʹ��</returns>
    bool CanUse(GameObject user);

    /// <summary>
    /// ��ȡʹ��˵��
    /// </summary>
    /// <returns>ʹ��˵��</returns>
    string GetUsageDescription();
}

/// <summary>
/// �ɼ���ӿ� - ���Ա�����/���õĶ���
/// </summary>
public interface IActivatable
{
    /// <summary>
    /// �������
    /// </summary>
    void Activate();

    /// <summary>
    /// ���ö���
    /// </summary>
    void Deactivate();

    /// <summary>
    /// ����Ƿ��Ѽ���
    /// </summary>
    /// <returns>�Ƿ��Ѽ���</returns>
    bool IsActive();

    /// <summary>
    /// �л�����״̬
    /// </summary>
    void Toggle();
}