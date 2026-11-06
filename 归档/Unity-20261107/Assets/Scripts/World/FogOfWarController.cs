using UnityEngine;

/// <summary>
/// ս�������ʾ������ö��
/// </summary>
public enum FogRevealerType
{
    Permanent,  // ���ý�ʾ - ��ҽ�ɫ��
    Temporary,  // ��ʱ��ʾ - ���ܡ����ߵ�
    Static      // ��̬��ʾ - ��������͵��
}

/// <summary>
/// ս����������� - �����������ϵͳ
/// </summary>
public class FogOfWarController : Singleton<FogOfWarController>
{
    [Header("=== ս���������� ===")]
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

        Debug.Log($"[FogOfWarController] ����ϵͳ��ʼ����� - ����: {gridWidth}x{gridHeight}");
    }

    /// <summary>
    /// ��ʾָ��λ����Χ������
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
    /// ���ָ��λ���Ƿ�ɼ�
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
    /// ���ָ��λ���Ƿ���̽��
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
    /// ��������ת��������
    /// </summary>
    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x + mapSize.x * 0.5f) / cellSize);
        int y = Mathf.FloorToInt((worldPos.z + mapSize.y * 0.5f) / cellSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// �������λ���Ƿ���Ч
    /// </summary>
    bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    /// <summary>
    /// �����������������ã�
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
        Debug.Log("[FogOfWarController] �������������");
    }

    /// <summary>
    /// ��������������ã�
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
        Debug.Log("[FogOfWarController] ��������������");
    }
}

/// <summary>
/// ս�������ʾ�� - �����ͬ���͵���Ұ��ʾ
/// </summary>
public class FogRevealer : MonoBehaviour
{
    [Header("=== �������� ===")]
    [Tooltip("��ʾ������")]
    public FogRevealerType revealerType = FogRevealerType.Permanent;

    [Tooltip("��Ұ�뾶")]
    public float visionRadius = 6f;

    [Tooltip("�Ƿ������ʱ��������")]
    public bool activateOnStart = true;

    [Header("=== ��ʱ��ʾ������ ===")]
    [Tooltip("����ʱ�䣨�룩��������ʱ������Ч��0Ϊ����ʱ�䣩")]
    public float duration = 10f;

    [Tooltip("�Ƿ���ʱ�����������GameObject")]
    public bool destroyOnExpire = true;

    [Header("=== �Ӿ�Ч������ ===")]
    [Tooltip("��ʾ�����Ӿ�Ч��Ԥ����")]
    public GameObject visualEffectPrefab;

    [Tooltip("�Ӿ�Ч�����ű���")]
    public float effectScale = 1f;

    [Tooltip("Ч����ɫ")]
    public Color effectColor = Color.white;

    [Header("=== �����Ż� ===")]
    [Tooltip("����Ƶ�ʣ��룩��0Ϊÿ֡����")]
    public float updateInterval = 0f;

    [Tooltip("�Ƿ�����ƶ�ʱ����")]
    public bool updateOnlyWhenMoving = false;

    [Tooltip("�ƶ������ֵ")]
    public float movementThreshold = 0.1f;

    // ˽�б���
    private float timer = 0f;
    private float lastUpdateTime = 0f;
    private Vector3 lastPosition;
    private bool isActive = false;
    private GameObject visualEffect;

    // �¼�
    public System.Action OnRevealerExpired;
    public System.Action<float> OnDurationTick; // ������ʣ��ʱ��

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
    /// ��ʼ����ʾ��
    /// </summary>
    void InitializeRevealer()
    {
        // ��� FogOfWarController �Ƿ����
        if (FogOfWarController.Instance == null)
        {
            Debug.LogError($"[FogRevealer] FogOfWarController.Instance �����ڡ�λ��: {gameObject.name}");
            enabled = false;
            return;
        }

        // �����Ӿ�Ч��
        CreateVisualEffect();

        Debug.Log($"[FogRevealer] ��ʼ����� - ����: {revealerType}, �뾶: {visionRadius}");
    }

    /// <summary>
    /// �����Ӿ�Ч��
    /// </summary>
    void CreateVisualEffect()
    {
        if (visualEffectPrefab != null)
        {
            visualEffect = Instantiate(visualEffectPrefab, transform);
            visualEffect.transform.localPosition = Vector3.zero;
            visualEffect.transform.localScale = Vector3.one * effectScale;

            // ����Ч����ɫ
            var renderer = visualEffect.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = effectColor;
            }

            visualEffect.SetActive(false);
        }
    }

    /// <summary>
    /// �����ʾ��
    /// </summary>
    public void ActivateRevealer()
    {
        if (FogOfWarController.Instance == null)
        {
            Debug.LogWarning("[FogRevealer] �޷����FogOfWarController ������");
            return;
        }

        isActive = true;
        timer = 0f;

        if (visualEffect != null)
        {
            visualEffect.SetActive(true);
        }

        // ������ʾһ��
        RevealFog();

        Debug.Log($"[FogRevealer] �Ѽ��� - {gameObject.name}");
    }

    /// <summary>
    /// ͣ�ý�ʾ��
    /// </summary>
    public void DeactivateRevealer()
    {
        isActive = false;

        if (visualEffect != null)
        {
            visualEffect.SetActive(false);
        }

        Debug.Log($"[FogRevealer] ��ͣ�� - {gameObject.name}");
    }

    /// <summary>
    /// �������ʱ���ʱ
    /// </summary>
    void HandleDurationTimer()
    {
        if (revealerType != FogRevealerType.Temporary || duration <= 0) return;

        timer += Time.deltaTime;

        // ����ʣ��ʱ���¼�
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
    /// ���������ʾ
    /// </summary>
    void HandleFogReveal()
    {
        // ������Ƶ������
        if (updateInterval > 0 && Time.time - lastUpdateTime < updateInterval)
        {
            return;
        }

        // ����ƶ����
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
    /// ִ�������ʾ
    /// </summary>
    void RevealFog()
    {
        if (FogOfWarController.Instance != null)
        {
            FogOfWarController.Instance.RevealArea(transform.position, visionRadius);
        }
    }

    // ========== �����ӿ� ==========

    /// <summary>
    /// ������Ұ�뾶
    /// </summary>
    public void SetVisionRadius(float radius)
    {
        visionRadius = Mathf.Max(0, radius);

        // �����Ӿ�Ч������
        if (visualEffect != null)
        {
            float scale = (radius / 6f) * effectScale; // ����Ĭ�ϰ뾶6��������
            visualEffect.transform.localScale = Vector3.one * scale;
        }
    }

    /// <summary>
    /// ���ó���ʱ��
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
        timer = 0f; // ���ü�ʱ��
    }

    /// <summary>
    /// ��ȡʣ��ʱ��
    /// </summary>
    public float GetRemainingTime()
    {
        if (revealerType != FogRevealerType.Temporary || duration <= 0)
            return float.MaxValue;

        return Mathf.Max(0, duration - timer);
    }

    /// <summary>
    /// ��ȡʣ��ʱ��ٷֱ�
    /// </summary>
    public float GetRemainingTimePercent()
    {
        if (revealerType != FogRevealerType.Temporary || duration <= 0)
            return 1f;

        return Mathf.Clamp01((duration - timer) / duration);
    }

    /// <summary>
    /// �ӳ�����ʱ��
    /// </summary>
    public void ExtendDuration(float extraTime)
    {
        if (revealerType == FogRevealerType.Temporary)
        {
            duration += extraTime;
        }
    }

    /// <summary>
    /// �������ٽ�ʾ��
    /// </summary>
    public void DestroyRevealer()
    {
        OnRevealerExpired?.Invoke();
        Destroy(gameObject);
    }

    /// <summary>
    /// ����ʾ���Ƿ񼤻�
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }

    /// <summary>
    /// ����Ч����ɫ
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

    // ========== ��̬�������� ==========

    /// <summary>
    /// ���������Խ�ʾ������ɫʹ�ã�
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
    /// ������ʱ��ʾ��������֮�ۣ�
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
    /// ������̬��ʾ����������ʹ�ã�
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
        revealer.updateOnlyWhenMoving = false; // ��̬���岻��Ҫ�ƶ����
        revealer.updateInterval = 1f; // �ϵ͸���Ƶ��
        revealer.activateOnStart = true;

        return revealer;
    }

    // ========== ���Թ��� ==========

    void OnDrawGizmosSelected()
    {
        // ������Ұ��Χ
        Gizmos.color = isActive ? Color.green : Color.gray;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // �������ͱ�ʶ
        Gizmos.color = GetTypeColor();
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);

        // �������ʱ���ͣ���ʾʣ��ʱ����Ϣ
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
        // ��͸����ʾ������Χ
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        Gizmos.DrawSphere(transform.position, visionRadius);
    }
}