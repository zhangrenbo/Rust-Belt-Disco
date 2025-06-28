using UnityEngine;

/// <summary>
/// 动画类型枚举
/// </summary>
public enum AnimationType
{
    Idle,       // 待机
    Walk,       // 行走
    Run,        // 奔跑
    Attack,     // 攻击
    Hit,        // 受击
    Die,        // 死亡
    Jump,       // 跳跃
    Cast        // 施法
}

/// <summary>
/// 动画方向枚举
/// </summary>
public enum AnimationDirection
{
    Down,       // 向下
    Up,         // 向上
    Left,       // 向左
    Right       // 向右
}

/// <summary>
/// 角色状态枚举
/// </summary>
public enum CharacterState
{
    Idle,       // 待机
    Walking,    // 行走
    Running,    // 奔跑
    Attacking,  // 攻击
    Hit,        // 受击
    Dead,       // 死亡
    Dialogue,   // 对话
    Jumping,    // 跳跃
    Casting     // 施法
}