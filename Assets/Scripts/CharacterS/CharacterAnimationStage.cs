using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ��ɫ�����׶ζ���
/// </summary>
[System.Serializable]
public class CharacterAnimationStage
{
    [Header("�׶���Ϣ")]
    public string stageName = "�����׶�";
    public int minLevel = 1;
    public int maxLevel = 10;

    [Header("��������")]
    public string[] requiredStoryKeys;  // ��Ҫ�ľ������
    public int[] requiredStoryValues;   // ��Ӧ�ľ���ֵ

    [Header("����������")]
    public RuntimeAnimatorController animatorController;

    [Header("������������")]
    public AnimationParameterOverride[] parameterOverrides;

    [Header("��ЧЧ��")]
    public GameObject[] stageEffects;   // �׶���Ч
    public AudioClip stageTransitionSound; // �׶��л���Ч
}

/// <summary>
/// ����������������
/// </summary>
[System.Serializable]
public class AnimationParameterOverride
{
    public string parameterName;
    public AnimationClip newClip;
    public float speedMultiplier = 1f;
}
