using UnityEngine;

/// <summary>
/// ��������ö��
/// </summary>
public enum AnimationType
{
    Idle,       // ����
    Walk,       // ����
    Run,        // ����
    Attack,     // ����
    Hit,        // �ܻ�
    Die,        // ����
    Jump,       // ��Ծ
    Cast        // ʩ��
}

/// <summary>
/// ��������ö��
/// </summary>
public enum AnimationDirection
{
    Down,       // ����
    Up,         // ����
    Left,       // ����
    Right       // ����
}

/// <summary>
/// ��ɫ״̬ö��
/// </summary>
public enum CharacterState
{
    Idle,       // ����
    Walking,    // ����
    Running,    // ����
    Attacking,  // ����
    Hit,        // �ܻ�
    Dead,       // ����
    Dialogue,   // �Ի�
    Jumping,    // ��Ծ
    Casting     // ʩ��
}