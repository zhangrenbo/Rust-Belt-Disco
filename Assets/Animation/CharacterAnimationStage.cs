using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 角色动画阶段定义
/// </summary>
[System.Serializable]
public class CharacterAnimationStage
{
    [Header("阶段信息")]
    public string stageName = "初级阶段";
    public int minLevel = 1;
    public int maxLevel = 10;

    [Header("触发条件")]
    public string[] requiredStoryKeys;  // 需要的剧情进度
    public int[] requiredStoryValues;   // 对应的剧情值

    [Header("动画控制器")]
    public RuntimeAnimatorController animatorController;

    [Header("动画参数覆盖")]
    public AnimationParameterOverride[] parameterOverrides;

    [Header("特效效果")]
    public GameObject[] stageEffects;   // 阶段特效
    public AudioClip stageTransitionSound; // 阶段切换音效
}

/// <summary>
/// 动画参数覆盖设置
/// </summary>
[System.Serializable]
public class AnimationParameterOverride
{
    public string parameterName;
    public AnimationClip newClip;
    public float speedMultiplier = 1f;
}
