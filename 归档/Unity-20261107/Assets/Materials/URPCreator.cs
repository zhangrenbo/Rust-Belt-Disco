using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class URPCreator : MonoBehaviour
{
    [MenuItem("Tools/URP/🌀 Create URP Pipeline Asset")]
    public static void CreateURPAssets()
    {
        // 创建 URP Asset
        var urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        AssetDatabase.CreateAsset(urpAsset, "Assets/UniversalRenderPipelineAsset.asset");

        // 创建 Forward Renderer
        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(rendererData, "Assets/UniversalRenderPipelineAsset_Renderer.asset");

        // 将 rendererData 设置进 urpAsset（需要通过序列化对象设置）
        using (var so = new SerializedObject(urpAsset))
        {
            var prop = so.FindProperty("m_RendererDataList");
            if (prop != null && prop.isArray && prop.arraySize > 0)
            {
                prop.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
                so.ApplyModifiedProperties();
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ URP Pipeline Asset 和 Renderer 已创建！");
    }
}
