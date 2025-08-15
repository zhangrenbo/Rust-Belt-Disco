using UnityEngine;
using System.Collections;

/// <summary>
/// �򵥵�PNG���в����� - ���ڲݴԶ���
/// </summary>
public class SimplePngSequencePlayer : MonoBehaviour
{
    [Header("=== �������� ===")]
    [Tooltip("��������֡")]
    public Texture2D[] idleFrames;
    [Tooltip("�ڶ�����֡")]
    public Texture2D[] swayFrames;

    [Header("=== �������� ===")]
    [Tooltip("֡��")]
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
/// ������ݴԶ��������� - ʹ��SimplePngSequencePlayer
/// </summary>
public class GrassAnimationController : MonoBehaviour
{
    [Header("=== �ݴ����� ===")]
    [Tooltip("��Ҵ�����Χ")]
    public float triggerRadius = 1.5f;

    [Tooltip("�ڶ�����ʱ��")]
    public float swayDuration = 2f;

    [Tooltip("��ȴʱ�䣨��ֹƵ��������")]
    public float cooldownTime = 1f;

    [Header("=== �������� ===")]
    [Tooltip("��ʾ������Χ")]
    public bool showTriggerRange = true;
    [Tooltip("��ʾ������Ϣ")]
    public bool showDebugInfo = false;

    // �ڲ�״̬
    private SimplePngSequencePlayer sequencePlayer;
    private Transform player;
    private bool isSwaying = false;
    private float swayTimer = 0f;
    private float cooldownTimer = 0f;
    private bool playerWasInRange = false;

    void Start()
    {
        // ��ȡPNG���в��������
        sequencePlayer = GetComponent<SimplePngSequencePlayer>();
        if (sequencePlayer == null)
        {
            Debug.LogError("[GrassAnimationController] δ�ҵ� SimplePngSequencePlayer ���������Ӹ������");
            enabled = false;
            return;
        }

        // �������
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[GrassAnimationController] δ�ҵ���ǩΪ 'Player' ����Ϸ����");
        }

        // ���Ŵ�������
        PlayIdleAnimation();

        if (showDebugInfo)
        {
            Debug.Log("[GrassAnimationController] �ݴԿ�������ʼ�����");
        }
    }

    void Update()
    {
        if (player == null) return;

        // ������ȴ��ʱ��
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // �����Ҿ���
        float distance = Vector3.Distance(transform.position, player.position);
        bool playerInRange = distance <= triggerRadius;

        // ��ҽ��뷶Χʱ�����ڶ�
        if (playerInRange && !playerWasInRange && !isSwaying && cooldownTimer <= 0)
        {
            TriggerSway();
        }

        playerWasInRange = playerInRange;

        // ���°ڶ�״̬
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
    /// ���Ŵ�������
    /// </summary>
    void PlayIdleAnimation()
    {
        if (sequencePlayer != null)
        {
            sequencePlayer.PlayIdleAnimation();
        }
    }

    /// <summary>
    /// �����ڶ�����
    /// </summary>
    void TriggerSway()
    {
        isSwaying = true;
        swayTimer = swayDuration;
        cooldownTimer = cooldownTime;

        // ���Űڶ�����
        if (sequencePlayer != null)
        {
            sequencePlayer.PlaySwayAnimation();
        }

        if (showDebugInfo)
        {
            Debug.Log("[GrassAnimationController] �ݴԿ�ʼ�ڶ�");
        }
    }

    /// <summary>
    /// ֹͣ�ڶ����ص�����״̬
    /// </summary>
    void StopSway()
    {
        isSwaying = false;
        swayTimer = 0f;

        // �ص���������
        PlayIdleAnimation();

        if (showDebugInfo)
        {
            Debug.Log("[GrassAnimationController] �ݴԻص�����״̬");
        }
    }

    /// <summary>
    /// �ֶ������ڶ������ڲ��ԣ�
    /// </summary>
    [ContextMenu("�ֶ������ڶ�")]
    public void ManualTriggerSway()
    {
        if (!isSwaying && cooldownTimer <= 0)
        {
            TriggerSway();
        }
        else if (showDebugInfo)
        {
            Debug.Log("[GrassAnimationController] �޷����������ڰڶ�������ȴ��");
        }
    }

    /// <summary>
    /// ���õ�����״̬
    /// </summary>
    [ContextMenu("���õ�����")]
    public void ResetToIdle()
    {
        StopSway();
    }

    /// <summary>
    /// �������òݴԶ���
    /// </summary>
    [ContextMenu("�������òݴԶ���")]
    public void QuickSetupGrassAnimation()
    {
        if (sequencePlayer == null)
        {
            sequencePlayer = GetComponent<SimplePngSequencePlayer>();
            if (sequencePlayer == null)
            {
                sequencePlayer = gameObject.AddComponent<SimplePngSequencePlayer>();
                Debug.Log("[GrassAnimationController] ���Զ���� SimplePngSequencePlayer ���");
            }
        }

        // ���òݴԿ���������
        triggerRadius = 1.5f;
        swayDuration = 2f;
        cooldownTime = 1f;
        showTriggerRange = true;

        Debug.Log("[GrassAnimationController] �ѿ������òݴԶ���");
    }

    /// <summary>
    /// ��ȡ��ǰ״̬��Ϣ
    /// </summary>
    public string GetStateInfo()
    {
        string state = isSwaying ? "�ڶ���" : "������";
        string playerDistance = player != null ?
            $"��Ҿ���: {Vector3.Distance(transform.position, player.position):F1}m" :
            "δ�ҵ����";

        return $"״̬: {state}\n{playerDistance}\n��ȴʣ��: {Mathf.Max(0, cooldownTimer):F1}s";
    }

    // ========== �����ӿ� ==========

    /// <summary>
    /// ���ô�����Χ
    /// </summary>
    public void SetTriggerRadius(float radius)
    {
        triggerRadius = Mathf.Max(0.1f, radius);
    }

    /// <summary>
    /// ���ðڶ�����ʱ��
    /// </summary>
    public void SetSwayDuration(float duration)
    {
        swayDuration = Mathf.Max(0.1f, duration);
    }

    /// <summary>
    /// ������ȴʱ��
    /// </summary>
    public void SetCooldownTime(float cooldown)
    {
        cooldownTime = Mathf.Max(0f, cooldown);
    }

    /// <summary>
    /// ����Ƿ����ڰڶ�
    /// </summary>
    public bool IsSwaying()
    {
        return isSwaying;
    }

    /// <summary>
    /// ����Ƿ�����ȴ��
    /// </summary>
    public bool IsOnCooldown()
    {
        return cooldownTimer > 0;
    }

    /// <summary>
    /// ��ȡ��Ҿ���
    /// </summary>
    public float GetPlayerDistance()
    {
        if (player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, player.position);
    }

    // ========== ���ԺͿ��ӻ� ==========

    void OnDrawGizmosSelected()
    {
        if (!showTriggerRange) return;

        // ���ƴ�����Χ
        Gizmos.color = isSwaying ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        // �������ң����Ƶ���ҵ�����
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            Gizmos.color = distance <= triggerRadius ? Color.yellow : Color.gray;
            Gizmos.DrawLine(transform.position, player.position);

            // �����λ�û���һ��С��
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(player.position, 0.2f);
        }

        // ����״ָ̬ʾ��
        Vector3 statusPos = transform.position + Vector3.up * 2f;
        Color orangeColor = new Color(1f, 0.5f, 0f); // ��ɫ (R=1, G=0.5, B=0)
        Gizmos.color = isSwaying ? Color.red : (IsOnCooldown() ? orangeColor : Color.green);
        Gizmos.DrawWireSphere(statusPos, 0.1f);
    }

    void OnDrawGizmos()
    {
        // ���ƻ���������Χ����͸����
        if (showTriggerRange)
        {
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawSphere(transform.position, triggerRadius);
        }
    }

    // ========== ����GUI ==========

    void OnGUI()
    {
        if (!showDebugInfo) return;

        // ����GUIλ�ã�����������������Ϣ�ص�
        float yOffset = 300f; // ��PngSequencePlayer�ĵ�����Ϣ���
        GUILayout.BeginArea(new Rect(Screen.width - 300, yOffset, 290, 150));
        GUILayout.BeginVertical("box");

        GUILayout.Label("�ݴԶ���������", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

        // ��ʾ״̬��Ϣ
        string[] stateLines = GetStateInfo().Split('\n');
        foreach (string line in stateLines)
        {
            GUILayout.Label(line);
        }

        GUILayout.Space(5);

        // ���ư�ť
        if (GUILayout.Button("�ֶ������ڶ�"))
        {
            ManualTriggerSway();
        }

        if (GUILayout.Button("���õ�����"))
        {
            ResetToIdle();
        }

        if (GUILayout.Button("��������"))
        {
            QuickSetupGrassAnimation();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}

/// <summary>
/// �ݴ��������ù��� - ������
/// </summary>
public class GrassBatchSetup : MonoBehaviour
{
    [Header("=== �������ò��� ===")]
    [Tooltip("������Χ")]
    public float triggerRadius = 1.5f;

    [Tooltip("�ڶ�����ʱ��")]
    public float swayDuration = 2f;

    [Tooltip("��ȴʱ��")]
    public float cooldownTime = 1f;

    /// <summary>
    /// Ϊѡ�е�����GameObject���òݴԶ���
    /// </summary>
    [ContextMenu("Ϊѡ�ж������òݴԶ���")]
    public void SetupSelectedGrass()
    {
#if UNITY_EDITOR
        var selectedObjects = UnityEditor.Selection.gameObjects;
        
        if (selectedObjects.Length == 0)
        {
            UnityEditor.EditorUtility.DisplayDialog("��ʾ", "����ѡ��Ҫ���õĲݴԶ���", "ȷ��");
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

        UnityEditor.EditorUtility.DisplayDialog("�������", 
            $"�ɹ�Ϊ {successCount}/{selectedObjects.Length} �����������˲ݴԶ���", "ȷ��");
#endif
    }

    bool SetupSingleGrass(GameObject grassObj)
    {
        // ��ӻ��ȡ SimplePngSequencePlayer ���
        var sequencePlayer = grassObj.GetComponent<SimplePngSequencePlayer>();
        if (sequencePlayer == null)
        {
            sequencePlayer = grassObj.AddComponent<SimplePngSequencePlayer>();
        }

        // ��ӻ��ȡ GrassAnimationController ���
        var grassController = grassObj.GetComponent<GrassAnimationController>();
        if (grassController == null)
        {
            grassController = grassObj.AddComponent<GrassAnimationController>();
        }

        // ���ò���
        grassController.triggerRadius = triggerRadius;
        grassController.swayDuration = swayDuration;
        grassController.cooldownTime = cooldownTime;

        // ȷ����Renderer���
        if (grassObj.GetComponent<Renderer>() == null)
        {
            Debug.LogWarning($"[GrassBatchSetup] {grassObj.name} û��Renderer������޷����Ŷ���");
            return false;
        }

        Debug.Log($"[GrassBatchSetup] ��Ϊ {grassObj.name} ���òݴԶ���");
        return true;
    }

    /// <summary>
    /// ���������õĲݴ�Ԥ�Ƽ�ģ��
    /// </summary>
    [ContextMenu("�����ݴ�Ԥ�Ƽ�ģ��")]
    public void CreateGrassPrefabTemplate()
    {
        // ��������GameObject
        GameObject grassPrefab = new GameObject("Grass");

        // ��ӻ������
        var meshRenderer = grassPrefab.AddComponent<MeshRenderer>();
        var meshFilter = grassPrefab.AddComponent<MeshFilter>();

        // ʹ��Quad mesh
        meshFilter.mesh = CreateQuadMesh();

        // ��������
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        meshRenderer.material = material;

        // ��Ӷ������
        SetupSingleGrass(grassPrefab);

        Debug.Log("[GrassBatchSetup] ������ݴ�Ԥ�Ƽ�ģ���Ѵ���");
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