using UnityEngine;
using System.Collections;

/// <summary>
/// 简单的PNG序列播放器 - 用于草丛动画
/// </summary>
public class SimplePngSequencePlayer : MonoBehaviour
{
    [Header("=== 动画序列 ===")]
    [Tooltip("待机动画帧")]
    public Texture2D[] idleFrames;
    [Tooltip("摆动动画帧")]
    public Texture2D[] swayFrames;

    [Header("=== 播放设置 ===")]
    [Tooltip("帧率")]
    public float frameRate = 12f;

    private Renderer targetRenderer;
    private Material targetMaterial;
    private Coroutine currentAnimation;
    private bool isPlaying = false;

    void Start()
    {
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            targetMaterial = targetRenderer.material;
        }
    }

    public void PlayIdleAnimation()
    {
        if (idleFrames != null && idleFrames.Length > 0)
        {
            PlaySequence(idleFrames, true);
        }
    }

    public void PlaySwayAnimation()
    {
        if (swayFrames != null && swayFrames.Length > 0)
        {
            PlaySequence(swayFrames, false);
        }
    }

    private void PlaySequence(Texture2D[] frames, bool loop)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        currentAnimation = StartCoroutine(PlaySequenceCoroutine(frames, loop));
    }

    private IEnumerator PlaySequenceCoroutine(Texture2D[] frames, bool loop)
    {
        if (frames == null || frames.Length == 0 || targetMaterial == null) yield break;

        isPlaying = true;
        float frameTime = 1f / frameRate;

        do
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                {
                    targetMaterial.mainTexture = frames[i];
                }
                yield return new WaitForSeconds(frameTime);
            }
        } while (loop);

        isPlaying = false;
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }

    public void StopAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
            isPlaying = false;
        }
    }
}

/// <summary>
/// 修正版草丛动画控制器 - 使用SimplePngSequencePlayer
/// </summary>
public class GrassAnimationController : MonoBehaviour
{
    [Header("=== 草丛设置 ===")]
    [Tooltip("玩家触发范围")]
    public float triggerRadius = 1.5f;

    [Tooltip("摆动持续时间")]
    public float swayDuration = 2f;

    [Tooltip("冷却时间（防止频繁触发）")]
    public float cooldownTime = 1f;

    [Header("=== 调试设置 ===")]
    [Tooltip("显示触发范围")]
    public bool showTriggerRange = true;
    [Tooltip("显示调试信息")]
    public bool showDebugInfo = false;

    // 内部状态
    private SimplePngSequencePlayer sequencePlayer;
    private Transform player;
    private bool isSwaying = false;
    private float swayTimer = 0f;
    private float cooldownTimer = 0f;
    private bool playerWasInRange = false;

    void Start()
    {
        // 获取PNG序列播放器组件
        sequencePlayer = GetComponent<SimplePngSequencePlayer>();
        if (sequencePlayer == null)
        {
            Debug.LogError("[GrassAnimationController] 未找到 SimplePngSequencePlayer 组件！请添加该组件。");
            enabled = false;
            return;
        }

        // 查找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[GrassAnimationController] 未找到标签为 'Player' 的游戏对象");
        }

        // 播放待机动画
        PlayIdleAnimation();

        if (showDebugInfo)
        {
            Debug.Log("[GrassAnimationController] 草丛控制器初始化完成");
        }
    }

    void Update()
    {
        if (player == null) return;

        // 更新冷却计时器
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // 检查玩家距离
        float distance = Vector3.Distance(transform.position, player.position);
        bool playerInRange = distance <= triggerRadius;

        // 玩家进入范围时触发摆动
        if (playerInRange && !playerWasInRange && !isSwaying && cooldownTimer <= 0)
        {
            TriggerSway();
        }

        playerWasInRange = playerInRange;

        // 更新摆动状态
        if (isSwaying)
        {
            swayTimer -= Time.deltaTime;
            if (swayTimer <= 0)
            {
                StopSway();
            }
        }
    }

    /// <summary>
    /// 播放待机动画
    /// </summary>
    void PlayIdleAnimation()
    {
        if (sequencePlayer != null)
        {
            sequencePlayer.PlayIdleAnimation();
        }
    }

    /// <summary>
    /// 触发摆动动画
    /// </summary>
    void TriggerSway()
    {
        isSwaying = true;
        swayTimer = swayDuration;
        cooldownTimer = cooldownTime;

        // 播放摆动动画
        if (sequencePlayer != null)
        {
            sequencePlayer.PlaySwayAnimation();
        }

        if (showDebugInfo)
        {
            Debug.Log("[GrassAnimationController] 草丛开始摆动");
        }
    }

    /// <summary>
    /// 停止摆动，回到待机状态
    /// </summary>
    void StopSway()
    {
        isSwaying = false;
        swayTimer = 0f;

        // 回到待机动画
        PlayIdleAnimation();

        if (showDebugInfo)
        {
            Debug.Log("[GrassAnimationController] 草丛回到待机状态");
        }
    }

    /// <summary>
    /// 手动触发摆动（用于测试）
    /// </summary>
    [ContextMenu("手动触发摆动")]
    public void ManualTriggerSway()
    {
        if (!isSwaying && cooldownTimer <= 0)
        {
            TriggerSway();
        }
        else if (showDebugInfo)
        {
            Debug.Log("[GrassAnimationController] 无法触发：正在摆动或处于冷却中");
        }
    }

    /// <summary>
    /// 重置到待机状态
    /// </summary>
    [ContextMenu("重置到待机")]
    public void ResetToIdle()
    {
        StopSway();
    }

    /// <summary>
    /// 快速配置草丛动画
    /// </summary>
    [ContextMenu("快速配置草丛动画")]
    public void QuickSetupGrassAnimation()
    {
        if (sequencePlayer == null)
        {
            sequencePlayer = GetComponent<SimplePngSequencePlayer>();
            if (sequencePlayer == null)
            {
                sequencePlayer = gameObject.AddComponent<SimplePngSequencePlayer>();
                Debug.Log("[GrassAnimationController] 已自动添加 SimplePngSequencePlayer 组件");
            }
        }

        // 配置草丛控制器参数
        triggerRadius = 1.5f;
        swayDuration = 2f;
        cooldownTime = 1f;
        showTriggerRange = true;

        Debug.Log("[GrassAnimationController] 已快速配置草丛动画");
    }

    /// <summary>
    /// 获取当前状态信息
    /// </summary>
    public string GetStateInfo()
    {
        string state = isSwaying ? "摆动中" : "待机中";
        string playerDistance = player != null ?
            $"玩家距离: {Vector3.Distance(transform.position, player.position):F1}m" :
            "未找到玩家";

        return $"状态: {state}\n{playerDistance}\n冷却剩余: {Mathf.Max(0, cooldownTimer):F1}s";
    }

    // ========== 公共接口 ==========

    /// <summary>
    /// 设置触发范围
    /// </summary>
    public void SetTriggerRadius(float radius)
    {
        triggerRadius = Mathf.Max(0.1f, radius);
    }

    /// <summary>
    /// 设置摆动持续时间
    /// </summary>
    public void SetSwayDuration(float duration)
    {
        swayDuration = Mathf.Max(0.1f, duration);
    }

    /// <summary>
    /// 设置冷却时间
    /// </summary>
    public void SetCooldownTime(float cooldown)
    {
        cooldownTime = Mathf.Max(0f, cooldown);
    }

    /// <summary>
    /// 检查是否正在摆动
    /// </summary>
    public bool IsSwaying()
    {
        return isSwaying;
    }

    /// <summary>
    /// 检查是否在冷却中
    /// </summary>
    public bool IsOnCooldown()
    {
        return cooldownTimer > 0;
    }

    /// <summary>
    /// 获取玩家距离
    /// </summary>
    public float GetPlayerDistance()
    {
        if (player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, player.position);
    }

    // ========== 调试和可视化 ==========

    void OnDrawGizmosSelected()
    {
        if (!showTriggerRange) return;

        // 绘制触发范围
        Gizmos.color = isSwaying ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        // 如果有玩家，绘制到玩家的连线
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            Gizmos.color = distance <= triggerRadius ? Color.yellow : Color.gray;
            Gizmos.DrawLine(transform.position, player.position);

            // 在玩家位置绘制一个小球
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(player.position, 0.2f);
        }

        // 绘制状态指示器
        Vector3 statusPos = transform.position + Vector3.up * 2f;
        Color orangeColor = new Color(1f, 0.5f, 0f); // 橙色 (R=1, G=0.5, B=0)
        Gizmos.color = isSwaying ? Color.red : (IsOnCooldown() ? orangeColor : Color.green);
        Gizmos.DrawWireSphere(statusPos, 0.1f);
    }

    void OnDrawGizmos()
    {
        // 绘制基本触发范围（半透明）
        if (showTriggerRange)
        {
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawSphere(transform.position, triggerRadius);
        }
    }

    // ========== 调试GUI ==========

    void OnGUI()
    {
        if (!showDebugInfo) return;

        // 计算GUI位置，避免与其他调试信息重叠
        float yOffset = 300f; // 与PngSequencePlayer的调试信息错开
        GUILayout.BeginArea(new Rect(Screen.width - 300, yOffset, 290, 150));
        GUILayout.BeginVertical("box");

        GUILayout.Label("草丛动画控制器", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

        // 显示状态信息
        string[] stateLines = GetStateInfo().Split('\n');
        foreach (string line in stateLines)
        {
            GUILayout.Label(line);
        }

        GUILayout.Space(5);

        // 控制按钮
        if (GUILayout.Button("手动触发摆动"))
        {
            ManualTriggerSway();
        }

        if (GUILayout.Button("重置到待机"))
        {
            ResetToIdle();
        }

        if (GUILayout.Button("快速配置"))
        {
            QuickSetupGrassAnimation();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}

/// <summary>
/// 草丛批量设置工具 - 修正版
/// </summary>
public class GrassBatchSetup : MonoBehaviour
{
    [Header("=== 批量设置参数 ===")]
    [Tooltip("触发范围")]
    public float triggerRadius = 1.5f;

    [Tooltip("摆动持续时间")]
    public float swayDuration = 2f;

    [Tooltip("冷却时间")]
    public float cooldownTime = 1f;

    /// <summary>
    /// 为选中的所有GameObject设置草丛动画
    /// </summary>
    [ContextMenu("为选中对象设置草丛动画")]
    public void SetupSelectedGrass()
    {
#if UNITY_EDITOR
        var selectedObjects = UnityEditor.Selection.gameObjects;
        
        if (selectedObjects.Length == 0)
        {
            UnityEditor.EditorUtility.DisplayDialog("提示", "请先选择要设置的草丛对象", "确定");
            return;
        }

        int successCount = 0;
        
        foreach (var obj in selectedObjects)
        {
            if (SetupSingleGrass(obj))
            {
                successCount++;
            }
        }

        UnityEditor.EditorUtility.DisplayDialog("设置完成", 
            $"成功为 {successCount}/{selectedObjects.Length} 个对象设置了草丛动画", "确定");
#endif
    }

    bool SetupSingleGrass(GameObject grassObj)
    {
        // 添加或获取 SimplePngSequencePlayer 组件
        var sequencePlayer = grassObj.GetComponent<SimplePngSequencePlayer>();
        if (sequencePlayer == null)
        {
            sequencePlayer = grassObj.AddComponent<SimplePngSequencePlayer>();
        }

        // 添加或获取 GrassAnimationController 组件
        var grassController = grassObj.GetComponent<GrassAnimationController>();
        if (grassController == null)
        {
            grassController = grassObj.AddComponent<GrassAnimationController>();
        }

        // 配置参数
        grassController.triggerRadius = triggerRadius;
        grassController.swayDuration = swayDuration;
        grassController.cooldownTime = cooldownTime;

        // 确保有Renderer组件
        if (grassObj.GetComponent<Renderer>() == null)
        {
            Debug.LogWarning($"[GrassBatchSetup] {grassObj.name} 没有Renderer组件，无法播放动画");
            return false;
        }

        Debug.Log($"[GrassBatchSetup] 已为 {grassObj.name} 设置草丛动画");
        return true;
    }

    /// <summary>
    /// 创建测试用的草丛预制件模板
    /// </summary>
    [ContextMenu("创建草丛预制件模板")]
    public void CreateGrassPrefabTemplate()
    {
        // 创建基本GameObject
        GameObject grassPrefab = new GameObject("Grass");

        // 添加基本组件
        var meshRenderer = grassPrefab.AddComponent<MeshRenderer>();
        var meshFilter = grassPrefab.AddComponent<MeshFilter>();

        // 使用Quad mesh
        meshFilter.mesh = CreateQuadMesh();

        // 创建材质
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        meshRenderer.material = material;

        // 添加动画组件
        SetupSingleGrass(grassPrefab);

        Debug.Log("[GrassBatchSetup] 修正版草丛预制件模板已创建");
    }

    Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0, 0),
            new Vector3(0.5f, 0, 0),
            new Vector3(-0.5f, 1, 0),
            new Vector3(0.5f, 1, 0)
        };

        Vector2[] uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        int[] triangles = new int[]
        {
            0, 2, 1,
            2, 3, 1
        };

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}