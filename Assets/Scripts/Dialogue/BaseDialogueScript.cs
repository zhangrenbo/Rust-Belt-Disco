using UnityEngine;

/// <summary>
/// �Ի��ű������� - ���жԻ��ű�����Ҫ�̳������
/// </summary>
public abstract class BaseDialogueScript : MonoBehaviour
{
    [Header("=== �Ի��������� ===")]
    public string dialogueId = "";
    public bool canRepeat = true;
    public int maxTriggerCount = -1; // -1��ʾ������
    public float cooldownTime = 0f;

    protected int triggerCount = 0;
    protected float lastTriggerTime = 0f;

    /// <summary>
    /// ����Ƿ���Դ����Ի�
    /// </summary>
    public virtual bool CanTrigger()
    {
        if (!canRepeat && triggerCount > 0)
            return false;

        if (maxTriggerCount > 0 && triggerCount >= maxTriggerCount)
            return false;

        if (Time.time - lastTriggerTime < cooldownTime)
            return false;

        return true;
    }

    /// <summary>
    /// ��ʼ�Ի� - �������ʵ��
    /// </summary>
    public abstract void StartDialogue(InteractiveDialogueTrigger trigger);

    /// <summary>
    /// ���Ϊ�Ѵ���
    /// </summary>
    protected void MarkAsTriggered()
    {
        triggerCount++;
        lastTriggerTime = Time.time;
    }

    /// <summary>
    /// ���öԻ�״̬
    /// </summary>
    public virtual void ResetDialogueState()
    {
        triggerCount = 0;
        lastTriggerTime = 0f;
    }
}
