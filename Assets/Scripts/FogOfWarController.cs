using UnityEngine;

/// <summary>
/// 战争迷雾揭示器类型枚举
/// </summary>
public enum FogRevealerType
{
    Permanent,  // 永久揭示 - 玩家角色等
    Temporary,  // 临时揭示 - 技能、道具等
    Static      // 静态揭示 - 建筑物、传送点等
}

/// <summary>
/// 战争迷雾控制器 - 不用依赖相机系统
/// </summary>
public class FogOfWarController : Singleton<FogOfWarController>
{
    [Header("=== 战争迷雾设置 ===")]
    public Vector2 mapSize = new Vector2(100, 100);
    public float cellSize = 1f;
    public bool enableFogOfWar = true;

    private int gridWidth;
    private int gridHeight;
    private bool[,] visibleCells;
    private bool[,] exploredCells;

    protected override void Awake()
    {
        base.Awake();
        InitializeFog();
    }

    void InitializeFog()
    {
        gridWidth = Mathf.CeilToInt(mapSize.x / cellSize);
        gridHeight = Mathf.CeilToInt(mapSize.y / cellSize);
        visibleCells = new bool[gridWidth, gridHeight];
        exploredCells = new bool[gridWidth, gridHeight];

        Debug.Log($"[FogOfWarController] 迷雾系统初始化完成 - 网格: {gridWidth}x{gridHeight}");
    }

    /// <summary>
    /// 揭示指定位置周围的区域
    /// </summary>
    public void RevealArea(Vector3 position, float radius)
    {
        if (!enableFogOfWar) return;

        Vector2Int gridPos = WorldToGrid(position);
        int cellRadius = Mathf.CeilToInt(radius / cellSize);

        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                int gridX = gridPos.x + x;
                int gridY = gridPos.y + y;

                if (IsValidPosition(gridX, gridY))
                {
                    float distance = Vector2.Distance(Vector2.zero, new Vector2(x, y));
                    if (distance <= cellRadius)
                    {
                        visibleCells[gridX, gridY] = true;
                        exploredCells[gridX, gridY] = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 检查指定位置是否可见
    /// </summary>
    public bool IsCellVisible(Vector3 position)
    {
        if (!enableFogOfWar) return true;

        Vector2Int gridPos = WorldToGrid(position);
        if (IsValidPosition(gridPos.x, gridPos.y))
        {
            return visibleCells[gridPos.x, gridPos.y];
        }
        return false;
    }

    /// <summary>
    /// 检查指定位置是否已探索
    /// </summary>
    public bool IsCellExplored(Vector3 position)
    {
        if (!enableFogOfWar) return true;

        Vector2Int gridPos = WorldToGrid(position);
        if (IsValidPosition(gridPos.x, gridPos.y))
        {
            return exploredCells[gridPos.x, gridPos.y];
        }
        return false;
    }

    /// <summary>
    /// 世界坐标转网格坐标
    /// </summary>
    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x + mapSize.x * 0.5f) / cellSize);
        int y = Mathf.FloorToInt((worldPos.z + mapSize.y * 0.5f) / cellSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 检查网格位置是否有效
    /// </summary>
    bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    /// <summary>
    /// 清除所有迷雾（调试用）
    /// </summary>
    public void ClearAllFog()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                visibleCells[x, y] = true;
                exploredCells[x, y] = true;
            }
        }
        Debug.Log("[FogOfWarController] 清除了所有迷雾");
    }

    /// <summary>
    /// 重置迷雾（调试用）
    /// </summary>
    public void ResetFog()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                visibleCells[x, y] = false;
                exploredCells[x, y] = false;
            }
        }
        Debug.Log("[FogOfWarController] 重置了所有迷雾");
    }
}

/// <summary>
/// 战争迷雾揭示器 - 处理不同类型的视野揭示
/// </summary>
public class FogRevealer : MonoBehaviour
{
    [Header("=== 基本设置 ===")]
    [Tooltip("揭示器类型")]
    public FogRevealerType revealerType = FogRevealerType.Permanent;

    [Tooltip("视野半径")]
    public float visionRadius = 6f;

    [Tooltip("是否在启动时立即激活")]
    public bool activateOnStart = true;

    [Header("=== 临时揭示器设置 ===")]
    [Tooltip("持续时间（秒），仅对临时类型有效，0为无限时间）")]
    public float duration = 10f;

    [Tooltip("是否在时间结束后销毁GameObject")]
    public bool destroyOnExpire = true;

    [Header("=== 视觉效果设置 ===")]
    [Tooltip("揭示器的视觉效果预制体")]
    public GameObject visualEffectPrefab;

    [Tooltip("视觉效果缩放比例")]
    public float effectScale = 1f;

    [Tooltip("效果颜色")]
    public Color effectColor = Color.white;

    [Header("=== 性能优化 ===")]
    [Tooltip("更新频率（秒），0为每帧更新")]
    public float updateInterval = 0f;

    [Tooltip("是否仅在移动时更新")]
    public bool updateOnlyWhenMoving = false;

    [Tooltip("移动检测阈值")]
    public float movementThreshold = 0.1f;

    // 私有变量
    private float timer = 0f;
    private float lastUpdateTime = 0f;
    private Vector3 lastPosition;
    private bool isActive = false;
    private GameObject visualEffect;

    // 事件
    public System.Action OnRevealerExpired;
    public System.Action<float> OnDurationTick; // 参数：剩余时间

    void Start()
    {
        InitializeRevealer();

        if (activateOnStart)
        {
            ActivateRevealer();
        }

        lastPosition = transform.position;
    }

    void Update()
    {
        if (!isActive) return;

        HandleDurationTimer();
        HandleFogReveal();
    }

    void OnDestroy()
    {
        if (visualEffect != null)
        {
            Destroy(visualEffect);
        }
    }

    /// <summary>
    /// 初始化揭示器
    /// </summary>
    void InitializeRevealer()
    {
        // 检查 FogOfWarController 是否存在
        if (FogOfWarController.Instance == null)
        {
            Debug.LogError($"[FogRevealer] FogOfWarController.Instance 不存在。位置: {gameObject.name}");
            enabled = false;
            return;
        }

        // 创建视觉效果
        CreateVisualEffect();

        Debug.Log($"[FogRevealer] 初始化完成 - 类型: {revealerType}, 半径: {visionRadius}");
    }

    /// <summary>
    /// 创建视觉效果
    /// </summary>
    void CreateVisualEffect()
    {
        if (visualEffectPrefab != null)
        {
            visualEffect = Instantiate(visualEffectPrefab, transform);
            visualEffect.transform.localPosition = Vector3.zero;
            visualEffect.transform.localScale = Vector3.one * effectScale;

            // 设置效果颜色
            var renderer = visualEffect.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = effectColor;
            }

            visualEffect.SetActive(false);
        }
    }

    /// <summary>
    /// 激活揭示器
    /// </summary>
    public void ActivateRevealer()
    {
        if (FogOfWarController.Instance == null)
        {
            Debug.LogWarning("[FogRevealer] 无法激活，FogOfWarController 不存在");
            return;
        }

        isActive = true;
        timer = 0f;

        if (visualEffect != null)
        {
            visualEffect.SetActive(true);
        }

        // 立即揭示一次
        RevealFog();

        Debug.Log($"[FogRevealer] 已激活 - {gameObject.name}");
    }

    /// <summary>
    /// 停用揭示器
    /// </summary>
    public void DeactivateRevealer()
    {
        isActive = false;

        if (visualEffect != null)
        {
            visualEffect.SetActive(false);
        }

        Debug.Log($"[FogRevealer] 已停用 - {gameObject.name}");
    }

    /// <summary>
    /// 处理持续时间计时
    /// </summary>
    void HandleDurationTimer()
    {
        if (revealerType != FogRevealerType.Temporary || duration <= 0) return;

        timer += Time.deltaTime;

        // 触发剩余时间事件
        float remainingTime = duration - timer;
        OnDurationTick?.Invoke(remainingTime);

        if (timer >= duration)
        {
            OnRevealerExpired?.Invoke();

            if (destroyOnExpire)
            {
                Destroy(gameObject);
            }
            else
            {
                DeactivateRevealer();
            }
        }
    }

    /// <summary>
    /// 处理迷雾揭示
    /// </summary>
    void HandleFogReveal()
    {
        // 检查更新频率限制
        if (updateInterval > 0 && Time.time - lastUpdateTime < updateInterval)
        {
            return;
        }

        // 检查移动检测
        if (updateOnlyWhenMoving)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            if (distanceMoved < movementThreshold)
            {
                return;
            }
            lastPosition = transform.position;
        }

        RevealFog();
        lastUpdateTime = Time.time;
    }

    /// <summary>
    /// 执行迷雾揭示
    /// </summary>
    void RevealFog()
    {
        if (FogOfWarController.Instance != null)
        {
            FogOfWarController.Instance.RevealArea(transform.position, visionRadius);
        }
    }

    // ========== 公共接口 ==========

    /// <summary>
    /// 设置视野半径
    /// </summary>
    public void SetVisionRadius(float radius)
    {
        visionRadius = Mathf.Max(0, radius);

        // 更新视觉效果缩放
        if (visualEffect != null)
        {
            float scale = (radius / 6f) * effectScale; // 基于默认半径6进行缩放
            visualEffect.transform.localScale = Vector3.one * scale;
        }
    }

    /// <summary>
    /// 设置持续时间
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
        timer = 0f; // 重置计时器
    }

    /// <summary>
    /// 获取剩余时间
    /// </summary>
    public float GetRemainingTime()
    {
        if (revealerType != FogRevealerType.Temporary || duration <= 0)
            return float.MaxValue;

        return Mathf.Max(0, duration - timer);
    }

    /// <summary>
    /// 获取剩余时间百分比
    /// </summary>
    public float GetRemainingTimePercent()
    {
        if (revealerType != FogRevealerType.Temporary || duration <= 0)
            return 1f;

        return Mathf.Clamp01((duration - timer) / duration);
    }

    /// <summary>
    /// 延长持续时间
    /// </summary>
    public void ExtendDuration(float extraTime)
    {
        if (revealerType == FogRevealerType.Temporary)
        {
            duration += extraTime;
        }
    }

    /// <summary>
    /// 立即销毁揭示器
    /// </summary>
    public void DestroyRevealer()
    {
        OnRevealerExpired?.Invoke();
        Destroy(gameObject);
    }

    /// <summary>
    /// 检查揭示器是否激活
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }

    /// <summary>
    /// 设置效果颜色
    /// </summary>
    public void SetEffectColor(Color color)
    {
        effectColor = color;

        if (visualEffect != null)
        {
            var renderer = visualEffect.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = color;
            }
        }
    }

    // ========== 静态工厂方法 ==========

    /// <summary>
    /// 创建永久性揭示器（角色使用）
    /// </summary>
    public static FogRevealer CreatePermanentRevealer(GameObject target, float radius)
    {
        var revealer = target.GetComponent<FogRevealer>();
        if (revealer == null)
        {
            revealer = target.AddComponent<FogRevealer>();
        }

        revealer.revealerType = FogRevealerType.Permanent;
        revealer.visionRadius = radius;
        revealer.activateOnStart = true;

        return revealer;
    }

    /// <summary>
    /// 创建临时揭示器（迷雾之眼）
    /// </summary>
    public static GameObject CreateTemporaryRevealer(Vector3 position, float radius, float duration)
    {
        GameObject eyeObject = new GameObject("FogEye");
        eyeObject.transform.position = position;

        var revealer = eyeObject.AddComponent<FogRevealer>();
        revealer.revealerType = FogRevealerType.Temporary;
        revealer.visionRadius = radius;
        revealer.duration = duration;
        revealer.destroyOnExpire = true;
        revealer.activateOnStart = true;

        return eyeObject;
    }

    /// <summary>
    /// 创建静态揭示器（建筑物使用）
    /// </summary>
    public static FogRevealer CreateStaticRevealer(GameObject target, float radius)
    {
        var revealer = target.GetComponent<FogRevealer>();
        if (revealer == null)
        {
            revealer = target.AddComponent<FogRevealer>();
        }

        revealer.revealerType = FogRevealerType.Static;
        revealer.visionRadius = radius;
        revealer.updateOnlyWhenMoving = false; // 静态物体不需要移动检测
        revealer.updateInterval = 1f; // 较低更新频率
        revealer.activateOnStart = true;

        return revealer;
    }

    // ========== 调试功能 ==========

    void OnDrawGizmosSelected()
    {
        // 绘制视野范围
        Gizmos.color = isActive ? Color.green : Color.gray;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // 绘制类型标识
        Gizmos.color = GetTypeColor();
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);

        // 如果是临时类型，显示剩余时间信息
        if (revealerType == FogRevealerType.Temporary && Application.isPlaying)
        {
            float percent = GetRemainingTimePercent();
            Gizmos.color = Color.Lerp(Color.red, Color.green, percent);
            Gizmos.DrawSphere(transform.position + Vector3.up * 3f, 0.2f);
        }
    }

    Color GetTypeColor()
    {
        switch (revealerType)
        {
            case FogRevealerType.Permanent: return Color.blue;
            case FogRevealerType.Temporary: return Color.yellow;
            case FogRevealerType.Static: return Color.cyan;
            default: return Color.white;
        }
    }

    void OnDrawGizmos()
    {
        // 半透明显示基本范围
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, visionRadius);
    }
}