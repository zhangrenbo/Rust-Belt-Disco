using UnityEngine;

public class WallVisibilityController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;  // 摄像机对象
    public float rotationThreshold = 90f;  // 设置Y轴旋转差值的阈值

    private Renderer wallRenderer;  // 墙壁的渲染器

    void Start()
    {
        // 获取墙壁的渲染器
        wallRenderer = GetComponent<Renderer>();

        // 如果没有手动设置摄像机，自动获取主摄像机
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("未找到主摄像机！请在Inspector中手动设置摄像机引用。");
            }
        }
    }

    void Update()
    {
        if (mainCamera == null || wallRenderer == null) return;

        // 获取物体和摄像机的Y轴旋转角度
        float wallYRotation = transform.eulerAngles.y;
        float cameraYRotation = mainCamera.transform.eulerAngles.y;

        // 计算两者的角度差异
        float angleDifference = Mathf.Abs(wallYRotation - cameraYRotation);

        // 确保角度差值不超过180度
        if (angleDifference > 180f)
        {
            angleDifference = 360f - angleDifference;
        }

        // 打印角度差值，帮助调试
        Debug.Log($"Wall Y Rotation: {wallYRotation}, Camera Y Rotation: {cameraYRotation}");
        Debug.Log($"Angle Difference: {angleDifference}");

        // 判断角度差值是否在设置的阈值范围内
        if (angleDifference <= rotationThreshold)
        {
            // 在阈值范围内，隐藏墙壁
            wallRenderer.enabled = false;
        }
        else
        {
            // 超出阈值范围，显示墙壁
            wallRenderer.enabled = true;
        }
    }
}