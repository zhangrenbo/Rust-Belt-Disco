using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CRTEffect : MonoBehaviour
{
    [Header("Shader & 材质")]
    public Shader shader;
    private Material mat;

    [Header("CRT 参数")]
    [Range(0, 1)] public float Distortion = 0.1f;
    [Range(0, 0.1f)] public float ChromAberration = 0.02f;
    public float ScanlineCount = 480f;
    [Range(0, 1)] public float ScanlineIntensity = 0.2f;
    [Range(0, 1)] public float NoiseIntensity = 0.1f;
    [Range(0, 1)] public float VignetteIntensity = 0.5f;
    [Range(0.1f, 1)] public float VignetteSoftness = 0.5f;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (shader == null)
        {
            Graphics.Blit(src, dest);
            return;
        }
        if (mat == null)
        {
            mat = new Material(shader);
            mat.hideFlags = HideFlags.HideAndDontSave;
        }

        // 设置参数
        mat.SetFloat("_Distortion", Distortion);
        mat.SetFloat("_ChromAber", ChromAberration);
        mat.SetFloat("_ScanlineCount", ScanlineCount);
        mat.SetFloat("_ScanlineIntensity", ScanlineIntensity);
        mat.SetFloat("_NoiseIntensity", NoiseIntensity);
        mat.SetFloat("_VignetteIntensity", VignetteIntensity);
        mat.SetFloat("_VignetteSoftness", VignetteSoftness);

        // 执行后期
        Graphics.Blit(src, dest, mat);
    }

    void OnDisable()
    {
        if (mat) DestroyImmediate(mat);
    }
}
