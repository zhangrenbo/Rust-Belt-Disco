using UnityEngine;

/// <summary>
/// 独立的相机系统 - 不包含迷雾功能
/// </summary>
public class CameraSystem : MonoBehaviour
{
    [Header("=== 相机跟随设置 ===")]
    [Tooltip("跟随目标（如果为空会自动寻找Player标签的对象）")]
    public Transform followTarget;
    [Tooltip("跟随速度")]
    public float followSpeed = 10f;
    [Tooltip("相机与目标的偏移（相对位置）")]
    public Vector3 offset = new Vector3(0, 10, -15);

    [Header("=== 鼠标旋转设置 ===")]
    [Tooltip("是否启用鼠标旋转控制（按住右键旋转）")]
    public bool enableMouseRotation = true;
    [Tooltip("鼠标旋转敏感度")]
    public float mouseSensitivity = 3f;
    [Tooltip("鼠标滚轮缩放敏感度")]
    public float zoomSensitivity = 2f;

    [Header("=== 旋转速度（保持朝向） ===")]
    [Tooltip("保持朝向旋转速度限制")]
    public float rotationSpeed = 3f;

    [Header("=== 对齐设置 ===")]
    [Tooltip("是否启用对象对齐到相机前方")]
    public bool enableObjectAlignment = false;
    [Tooltip("需要对齐的对象列表")]
    public Transform[] alignObjects;
    [Tooltip("是否忽略Y轴的对齐")]
    public bool ignoreYAxis = false;
    [Tooltip("对齐方向允许偏差")]
    public float alignmentThreshold = 0.1f;

    [Header("=== 垂直角度限制 ===")]
    [Tooltip("垂直最小角度")]
    public float minVerticalAngle = -30f;
    [Tooltip("垂直最大角度")]
    public float maxVerticalAngle = 60f;

    [Header("=== 锁定设置 ===")]
    [Tooltip("是否锁定垂直角度（X轴旋转）")]
    public bool lockPitch = true;
    [Tooltip("锁定的垂直角度（单位：度）")]
    public float lockedPitch = 20f;

    [Header("=== 调试设置 ===")]
    [Tooltip("是否在控制台显示调试信息")]
    public bool showDebugInfo = false;

    // 私有变量
    private Camera mainCamera;
    private float currentYaw = 0f;
    private float currentPitch = 20f;
    private float currentDistance;
    private Vector3 idealPosition;

    void Start()
    {
        InitializeCamera();
        InitializeFollowTarget();
        // 初始化距离和旋转
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
    /// 初始化主相机组件
    /// </summary>
    void InitializeCamera()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                Debug.LogError("[CameraSystem] 未找到 Camera 组件！");
        }
    }

    /// <summary>
    /// 初始化跟随目标
    /// </summary>
    void InitializeFollowTarget()
    {
        if (followTarget == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                followTarget = playerObj.transform;
                Debug.Log("[CameraSystem] 自动找到跟随目标: " + playerObj.name);
            }
            else
                Debug.LogWarning("[CameraSystem] 未设置跟随目标，请在 Inspector 中指定！");
        }
    }

    /// <summary>
    /// 处理鼠标旋转，允许调整角度，垂直角度可选锁定
    /// </summary>
    void HandleMouseRotation()
    {
        if (!enableMouseRotation) return;
        if (Input.GetMouseButton(2)) // 按住中键旋转
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
    /// 处理鼠标滚轮缩放相机距离
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
    /// 跟随目标并更新相机位置
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
    /// 对齐列表中对象让它们朝向相机前方方向
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
    /// 获取相机前方向，是否忽略Y轴
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
    /// 将对象朝向相机前方
    /// </summary>
    void AlignObjectToCamera(Transform obj, Vector3 direction)
    {
        if (direction.magnitude < alignmentThreshold) return;
        obj.rotation = Quaternion.LookRotation(direction);
    }

    #region 公共接口
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