using UnityEngine;

public class VisibilityController : MonoBehaviour
{
    [Tooltip("是否在探索过区域显示轮廓（灰暗）")]
    public bool showInExploredArea = true;

    [Tooltip("用于替换的灰暗材质，可为空")]
    public Material exploredMaterial;

    private Material[] originalMaterials;
    private Renderer[] renderers;

    private bool isVisible = true;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
        }
    }

    void LateUpdate()
    {
        // 检查 FogOfWarController 是否存在
        if (FogOfWarController.Instance == null)
        {
            // 如果没有战争迷雾系统，保持可见
            SetRenderState(true, useOriginal: true);
            return;
        }

        Vector3 worldPos = transform.position;
        bool currentlyVisible = FogOfWarController.Instance.IsCellVisible(worldPos);
        bool explored = FogOfWarController.Instance.IsCellExplored(worldPos);

        if (currentlyVisible)
        {
            SetRenderState(true, useOriginal: true);
        }
        else if (explored && showInExploredArea)
        {
            SetRenderState(true, useOriginal: false); // 半透明材质
        }
        else
        {
            SetRenderState(false);
        }
    }

    void SetRenderState(bool renderOn, bool useOriginal = true)
    {
        if (renderOn == isVisible && useOriginal) return; // 状态未变，不切换

        isVisible = renderOn;

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = renderOn;

            if (renderOn)
            {
                if (useOriginal && originalMaterials[i] != null)
                    renderers[i].material = originalMaterials[i];
                else if (exploredMaterial != null)
                    renderers[i].material = exploredMaterial;
            }
        }

        // 可扩展：控制UI、血条等
        // Example: healthBar.SetActive(renderOn);
    }
}