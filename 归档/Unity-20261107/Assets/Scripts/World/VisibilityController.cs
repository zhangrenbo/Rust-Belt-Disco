using UnityEngine;

public class VisibilityController : MonoBehaviour
{
    [Tooltip("�Ƿ���̽����������ʾ�������Ұ���")]
    public bool showInExploredArea = true;

    [Tooltip("�����滻�ĻҰ����ʣ���Ϊ��")]
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
        // ��� FogOfWarController �Ƿ����
        if (FogOfWarController.Instance == null)
        {
            // ���û��ս������ϵͳ�����ֿɼ�
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
            SetRenderState(true, useOriginal: false); // ��͸������
        }
        else
        {
            SetRenderState(false);
        }
    }

    void SetRenderState(bool renderOn, bool useOriginal = true)
    {
        if (renderOn == isVisible && useOriginal) return; // ״̬δ�䣬���л�

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

        // ����չ������UI��Ѫ����
        // Example: healthBar.SetActive(renderOn);
    }
}