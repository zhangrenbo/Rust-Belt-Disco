using UnityEngine;

/// <summary>
/// ���������ϵͳ - �������������
/// </summary>
public class CameraSystem : MonoBehaviour
{
    [Header("=== ����������� ===")]
    [Tooltip("����Ŀ�꣨���Ϊ�ջ��Զ�Ѱ��Player��ǩ�Ķ���")]
    public Transform followTarget;
    [Tooltip("�����ٶ�")]
    public float followSpeed = 10f;
    [Tooltip("�����Ŀ���ƫ�ƣ����λ�ã�")]
    public Vector3 offset = new Vector3(0, 10, -15);

    [Header("=== �����ת���� ===")]
    [Tooltip("�Ƿ����������ת���ƣ���ס�Ҽ���ת��")]
    public bool enableMouseRotation = true;
    [Tooltip("�����ת��ж�")]
    public float mouseSensitivity = 3f;
    [Tooltip("������������ж�")]
    public float zoomSensitivity = 2f;

    [Header("=== ��ת�ٶȣ����ֳ��� ===")]
    [Tooltip("���ֳ�����ת�ٶ�����")]
    public float rotationSpeed = 3f;

    [Header("=== �������� ===")]
    [Tooltip("�Ƿ����ö�����뵽���ǰ��")]
    public bool enableObjectAlignment = false;
    [Tooltip("��Ҫ����Ķ����б�")]
    public Transform[] alignObjects;
    [Tooltip("�Ƿ����Y��Ķ���")]
    public bool ignoreYAxis = false;
    [Tooltip("���뷽������ƫ��")]
    public float alignmentThreshold = 0.1f;

    [Header("=== ��ֱ�Ƕ����� ===")]
    [Tooltip("��ֱ��С�Ƕ�")]
    public float minVerticalAngle = -30f;
    [Tooltip("��ֱ���Ƕ�")]
    public float maxVerticalAngle = 60f;

    [Header("=== �������� ===")]
    [Tooltip("�Ƿ�������ֱ�Ƕȣ�X����ת��")]
    public bool lockPitch = true;
    [Tooltip("�����Ĵ�ֱ�Ƕȣ���λ���ȣ�")]
    public float lockedPitch = 20f;

    [Header("=== �������� ===")]
    [Tooltip("�Ƿ��ڿ���̨��ʾ������Ϣ")]
    public bool showDebugInfo = false;

    // ˽�б���
    private Camera mainCamera;
    private float currentYaw = 0f;
    private float currentPitch = 20f;
    private float currentDistance;
    private Vector3 idealPosition;

    void Start()
    {
        InitializeCamera();
        InitializeFollowTarget();
        // ��ʼ���������ת
        currentDistance = offset.magnitude;
        Vector3 euler = transform.eulerAngles;
        currentYaw = euler.y;
        currentPitch = lockPitch ? lockedPitch : euler.x;
    }

    void LateUpdate()
    {
        if (followTarget == null) return;
        HandleMouseRotation();
        HandleZoom();
        HandleCameraFollow();
        HandleObjectAlignment();
    }

    /// <summary>
    /// ��ʼ����������
    /// </summary>
    void InitializeCamera()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                Debug.LogError("[CameraSystem] δ�ҵ� Camera �����");
        }
    }

    /// <summary>
    /// ��ʼ������Ŀ��
    /// </summary>
    void InitializeFollowTarget()
    {
        if (followTarget == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                followTarget = playerObj.transform;
                Debug.Log("[CameraSystem] �Զ��ҵ�����Ŀ��: " + playerObj.name);
            }
            else
                Debug.LogWarning("[CameraSystem] δ���ø���Ŀ�꣬���� Inspector ��ָ����");
        }
    }

    /// <summary>
    /// ���������ת����������Ƕȣ���ֱ�Ƕȿ�ѡ����
    /// </summary>
    void HandleMouseRotation()
    {
        if (!enableMouseRotation) return;
        if (Input.GetMouseButton(2)) // ��ס�м���ת
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            currentYaw += mouseX;
            if (!lockPitch)
            {
                currentPitch -= mouseY;
                currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
            }
            if (showDebugInfo)
                Debug.Log($"[CameraSystem] Yaw={currentYaw:F1}, Pitch={currentPitch:F1}");
        }
    }

    /// <summary>
    /// ���������������������
    /// </summary>
    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance -= scroll * zoomSensitivity;
            currentDistance = Mathf.Clamp(currentDistance, 1f, 100f);
        }
    }

    /// <summary>
    /// ����Ŀ�겢�������λ��
    /// </summary>
    void HandleCameraFollow()
    {
        Quaternion rot = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 dir = rot * Vector3.back;
        idealPosition = followTarget.position + dir * currentDistance;
        transform.position = Vector3.Lerp(transform.position, idealPosition, followSpeed * Time.deltaTime);
        transform.LookAt(followTarget.position);
    }

    /// <summary>
    /// �����б��ж��������ǳ������ǰ������
    /// </summary>
    void HandleObjectAlignment()
    {
        if (!enableObjectAlignment || alignObjects == null) return;
        Vector3 forwardDir = GetAlignmentDirection();
        foreach (var obj in alignObjects)
        {
            if (obj != null)
                AlignObjectToCamera(obj, forwardDir);
        }
    }

    /// <summary>
    /// ��ȡ���ǰ�����Ƿ����Y��
    /// </summary>
    Vector3 GetAlignmentDirection()
    {
        Vector3 forward = transform.forward;
        if (ignoreYAxis) forward.y = 0;
        return forward.normalized;
    }

    /// <summary>
    /// Set camera yaw and pitch directly.
    /// </summary>
    public void SetYawPitch(float yaw, float pitch)
    {
        currentYaw = yaw;
        currentPitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }

    /// <summary>
    /// Smoothly rotate the camera towards a world position.
    /// </summary>
    public void RotateTowards(Vector3 worldPosition, float speed)
    {
        Vector3 dir = worldPosition - transform.position;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        float yaw = targetRot.eulerAngles.y;
        float pitch = targetRot.eulerAngles.x;
        float newYaw = Mathf.LerpAngle(currentYaw, yaw, speed * Time.deltaTime);
        float newPitch = Mathf.LerpAngle(currentPitch, pitch, speed * Time.deltaTime);
        SetYawPitch(newYaw, newPitch);
    }

    /// <summary>
    /// �����������ǰ��
    /// </summary>
    void AlignObjectToCamera(Transform obj, Vector3 direction)
    {
        if (direction.magnitude < alignmentThreshold) return;
        obj.rotation = Quaternion.LookRotation(direction);
    }

    #region �����ӿ�
    public void TeleportToTarget()
    {
        if (followTarget == null) return;
        transform.position = followTarget.position + (Quaternion.Euler(currentPitch, currentYaw, 0f) * Vector3.back) * currentDistance;
        transform.LookAt(followTarget);
    }

    public void ResetCameraRotation()
    {
        currentYaw = 0f;
        currentPitch = lockedPitch;
    }

    public float GetCameraYaw() { return currentYaw; }
    public float GetCameraPitch() { return currentPitch; }
    public void SetFollowTarget(Transform target) { followTarget = target; }

    public void AddAlignObject(Transform obj)
    {
        var list = new System.Collections.Generic.List<Transform>(alignObjects ?? new Transform[0]);
        if (!list.Contains(obj)) list.Add(obj);
        alignObjects = list.ToArray();
    }

    public void RemoveAlignObject(Transform obj)
    {
        if (alignObjects == null) return;
        var list = new System.Collections.Generic.List<Transform>(alignObjects);
        if (list.Remove(obj)) alignObjects = list.ToArray();
    }
    #endregion

    void OnDrawGizmosSelected()
    {
        if (followTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, followTarget.position);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(idealPosition, 0.5f);
        }
        if (enableObjectAlignment && alignObjects != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 dir = GetAlignmentDirection();
            foreach (var obj in alignObjects)
            {
                if (obj != null)
                    Gizmos.DrawRay(obj.position, dir * 2f);
            }
        }
    }
}