using UnityEngine;

public class WallVisibilityController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;  // ���������
    public float rotationThreshold = 90f;  // ����Y����ת��ֵ����ֵ

    private Renderer wallRenderer;  // ǽ�ڵ���Ⱦ��

    void Start()
    {
        // ��ȡǽ�ڵ���Ⱦ��
        wallRenderer = GetComponent<Renderer>();

        // ���û���ֶ�������������Զ���ȡ�������
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("δ�ҵ��������������Inspector���ֶ�������������á�");
            }
        }
    }

    void Update()
    {
        if (mainCamera == null || wallRenderer == null) return;

        // ��ȡ������������Y����ת�Ƕ�
        float wallYRotation = transform.eulerAngles.y;
        float cameraYRotation = mainCamera.transform.eulerAngles.y;

        // �������ߵĽǶȲ���
        float angleDifference = Mathf.Abs(wallYRotation - cameraYRotation);

        // ȷ���ǶȲ�ֵ������180��
        if (angleDifference > 180f)
        {
            angleDifference = 360f - angleDifference;
        }

        // ��ӡ�ǶȲ�ֵ����������
        Debug.Log($"Wall Y Rotation: {wallYRotation}, Camera Y Rotation: {cameraYRotation}");
        Debug.Log($"Angle Difference: {angleDifference}");

        // �жϽǶȲ�ֵ�Ƿ������õ���ֵ��Χ��
        if (angleDifference <= rotationThreshold)
        {
            // ����ֵ��Χ�ڣ�����ǽ��
            wallRenderer.enabled = false;
        }
        else
        {
            // ������ֵ��Χ����ʾǽ��
            wallRenderer.enabled = true;
        }
    }
}